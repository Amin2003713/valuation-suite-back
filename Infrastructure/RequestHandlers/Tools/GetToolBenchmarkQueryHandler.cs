using System.Text.Json;
using Application.Interfaces;
using Application.Interfaces.Base;
using Application.Tools;
using Domain.Tools;
using MediatR;

namespace RequestHandlers.Tools;

/// <summary>
///     Builds the benchmark (spider-web) data for one tool:
///       • per-metric Min / Max / Avg across OTHER users' stored submissions of
///         the caller's company industry ("Area" bucket), and
///       • the same stats across every OTHER submission of the tool regardless
///         of industry ("Overall" bucket).
///     The caller's own submissions are EXCLUDED from both buckets: the chart
///     plots the user's result separately and the comparison must not include it.
///     Metrics are the tool's normalized section/indicator scores found in the
///     stored result JSON. Submissions without an industry snapshot still count
///     toward Overall but never toward an Area bucket.
/// </summary>
public sealed class GetToolBenchmarkQueryHandler(
    IToolSubmissionQueries submissions,
    IQueryRepository<Domain.Users.ApplicationUser> usersQueries)
    : IRequestHandler<GetToolBenchmarkQuery, ToolBenchmarkResponse>
{
    public async Task<ToolBenchmarkResponse> Handle(GetToolBenchmarkQuery request, CancellationToken ct)
    {
        var user = await usersQueries.TableNoTracking
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        // Own submissions are excluded from the aggregate (the chart shows the
        // user's result as its own series — averaging it in would bias both).
        var rows = await submissions.ByTool(request.ToolCode)
            .Where(s => s.OverallScore != null
                        && s.UserId != request.UserId)
            .ToListAsync(ct);

        // Tool-metric extraction is agnostic of the specific tool: every tool's
        // result JSON carries either a "sections" array (key + score/percent) or
        // an "indicators" array, or a flat dict of numeric metrics.
        var perSubmission = rows
            .Select(s => (submission: s, metrics: ExtractMetrics(s.ResultJson)))
            .Where(x => x.metrics.Count > 0)
            .ToList();

        var overallAgg = Aggregate(perSubmission.Select(x => x.metrics));

        var myIndustry = user?.Company?.Industry;
        Dictionary<string, BenchmarkStat> areaAgg = new();
        if (!string.IsNullOrWhiteSpace(myIndustry))
        {
            areaAgg = Aggregate(
                perSubmission
                    .Where(x => string.Equals(x.submission.Industry, myIndustry, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.metrics));
        }

        var areaCount = string.IsNullOrWhiteSpace(myIndustry)
            ? 0
            : perSubmission.Count(x => string.Equals(x.submission.Industry, myIndustry, StringComparison.OrdinalIgnoreCase));

        return new ToolBenchmarkResponse(
            request.ToolCode,
            myIndustry,
            areaCount,
            perSubmission.Count,
            areaAgg,
            overallAgg);
    }

    /// <summary>Computes min/max/avg per metric key over many submissions.</summary>
    private static Dictionary<string, BenchmarkStat> Aggregate(IEnumerable<Dictionary<string, double>> buckets)
    {
        var acc = new Dictionary<string, List<double>>();
        foreach (var metrics in buckets)
        {
            foreach (var (key, value) in metrics)
            {
                if (!acc.TryGetValue(key, out var list))
                    acc[key] = list = [];
                list.Add(value);
            }
        }

        var result = new Dictionary<string, BenchmarkStat>();
        foreach (var (key, values) in acc)
        {
            if (values.Count == 0) continue;
            result[key] = new BenchmarkStat(
                values.Min(),
                values.Max(),
                values.Average(),
                values.Count,
                Median(values),
                TopQuartile(values));
        }
        return result;
    }

    /// <summary>Median of a sample (linear interpolation on the sorted list).</summary>
    private static double Median(List<double> values)
    {
        if (values.Count == 0) return 0;
        var sorted = values.OrderBy(v => v).ToList();
        var mid = (sorted.Count - 1) / 2.0;
        var lo = (int)Math.Floor(mid);
        var hi = (int)Math.Ceiling(mid);
        return sorted[lo] + (sorted[hi] - sorted[lo]) * (mid - lo);
    }

    /// <summary>75th percentile (top quartile) of a sample.</summary>
    private static double TopQuartile(List<double> values)
    {
        if (values.Count == 0) return 0;
        var sorted = values.OrderBy(v => v).ToList();
        var pos = 0.75 * (sorted.Count - 1);
        var lo = (int)Math.Floor(pos);
        var hi = (int)Math.Ceiling(pos);
        return sorted[lo] + (sorted[hi] - sorted[lo]) * (pos - lo);
    }

    /// <summary>
    ///     Pulls 0-100-normalizable metric scores out of a stored result JSON:
    ///     sections[] { key, score|percent }, indicators[] { key, score },
    ///     or a top-level numeric map (stepScores style).
    /// </summary>
    private static Dictionary<string, double> ExtractMetrics(string resultJson)
    {
        var metrics = new Dictionary<string, double>();
        try
        {
            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("rows", out var rows) && rows.ValueKind == JsonValueKind.Array)
                {
                    // STARTUP-ASSESS: one row per axis — the axis score "C" is 0–100.
                    var i = 0;
                    foreach (var r in rows.EnumerateArray())
                    {
                        if (r.ValueKind == JsonValueKind.Object)
                        {
                            // STARTUP-ASSESS: one row per axis — the axis score "C" is 0–100.
                            var axisScore = ReadScore(r, prop: "C");
                            if (axisScore is { } v) metrics[$"axis{i}"] = v;
                            // STARTUP-VAL: one row per valuation method — "h" is the
                            // method's share of the blended value (0–1); the chart
                            // scale is 0–100, so report h×100. Keyed by method id so
                            // submissions aggregate per method.
                            else if (r.TryGetProperty("h", out var hEl) && hEl.ValueKind == JsonValueKind.Number && hEl.TryGetDouble(out var h)
                                     && h >= 0 && h <= 1
                                     && r.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
                                     && idEl.GetString() is { } id)
                                metrics[id] = h * 100;
                        }
                        i++;
                    }
                }
                else if (root.TryGetProperty("sections", out var sections) && sections.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in sections.EnumerateArray())
                    {
                        if (s.ValueKind != JsonValueKind.Object) continue;
                        var key = s.TryGetProperty("key", out var k) && k.ValueKind == JsonValueKind.String ? k.GetString() : null;
                        if (key is null) continue;
                        var score = ReadScore(s);
                        if (score is { } v) metrics[key] = v;
                    }
                }
                else if (root.TryGetProperty("indicators", out var indicators) && indicators.ValueKind == JsonValueKind.Array)
                {
                    // INNOV-READ stores indicator scores 0–10; the benchmark scale
                    // is 0–100 (same ×10 the chart page applies), so normalize
                    // when the whole set sits in the 0–10 range.
                    var raw = new Dictionary<string, double>();
                    foreach (var s in indicators.EnumerateArray())
                    {
                        if (s.ValueKind != JsonValueKind.Object) continue;
                        var key = s.TryGetProperty("key", out var k) && k.ValueKind == JsonValueKind.String ? k.GetString() : null;
                        if (key is null) continue;
                        var score = ReadScore(s, max: 10);
                        if (score is { } v) raw[key] = v;
                    }
                    if (raw.Count > 0 && raw.Values.Max() <= 10)
                        foreach (var (key, v) in raw)
                            metrics[key] = v * 10;
                }
                else if (root.TryGetProperty("patents", out var patents) && patents.ValueKind == JsonValueKind.Array)
                {
                    // IPscore: each patent carries { scores: { legal, tech, market,
                    // finance, strategy } } (0–100). Every evaluated patent of
                    // other users contributes to the same per-dimension aggregate.
                    foreach (var p in patents.EnumerateArray())
                    {
                        if (p.ValueKind != JsonValueKind.Object) continue;
                        if (!p.TryGetProperty("scores", out var sc) || sc.ValueKind != JsonValueKind.Object) continue;
                        foreach (var prop in sc.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetDouble(out var d) && d >= 0 && d <= 100)
                                metrics[prop.Name] = d;
                        }
                    }
                }
                else if (root.TryGetProperty("total", out var totEl) && totEl.ValueKind == JsonValueKind.Number && totEl.TryGetDouble(out var tot)
                         && tot > 0
                         && root.TryGetProperty("rd", out var rdEl) && rdEl.ValueKind == JsonValueKind.Object
                         && root.TryGetProperty("hc", out var hcEl) && hcEl.ValueKind == JsonValueKind.Object
                         && root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Object)
                {
                    // INTANGIBLE: the result carries three module values whose sum
                    // is "total"; the honest 0–100 per-dimension figure is each
                    // module's share of that total.
                    ModuleShare(rdEl, "value", tot, "rd", metrics);
                    ModuleShare(hcEl, "blended", tot, "hc", metrics);
                    ModuleShare(dataEl, "value", tot, "data", metrics);
                }
                else
                {
                    // Flat numeric map (calculator-style stepScores).
                    foreach (var p in root.EnumerateObject())
                    {
                        if (p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetDouble(out var d) && d >= 0 && d <= 100)
                            metrics[p.Name] = d;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Corrupt/legacy result row — contributes nothing.
        }
        return metrics;
    }

    /// <summary>Reads a numeric property from a module object and reports it as
    /// its share of the module total (0–100), used for the INTANGIBLE radar.</summary>
    private static void ModuleShare(JsonElement module, string prop, double total, string key, Dictionary<string, double> metrics)
    {
        if (!module.TryGetProperty(prop, out var v) || v.ValueKind != JsonValueKind.Number || !v.TryGetDouble(out var d)) return;
        if (d < 0) return;
        var share = d / total * 100;
        if (share > 100) return; // total drifted out of sync with the sum — skip the axis
        metrics[key] = share;
    }

    /// <summary>Reads score/percent/value from a section object, clamped to 0..<paramref name="max"/>.</summary>
    private static double? ReadScore(JsonElement el, double max = 100, string? prop = null)
    {
        foreach (var p in prop is null ? new[] { "percent", "score", "value" } : new[] { prop })
        {
            if (el.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d))
            {
                if (d < 0 || d > max) return null;
                return d;
            }
        }
        return null;
    }
}
