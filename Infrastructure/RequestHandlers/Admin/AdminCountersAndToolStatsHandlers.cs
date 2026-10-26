using System.Text.Json;
using Application.Admin;
using Application.Interfaces;
using Application.Interfaces.Base;
using Common.Exceptions;
using Domain.Admin;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace RequestHandlers.Admin;

/// <summary>
///     Computes sidebar/news counter badges from each section's per-admin
///     "seen" timestamp: everything created AFTER the last acknowledgement
///     counts. A section never seen reports everything currently new.
/// </summary>
public sealed class GetAdminCountersQueryHandler(
    IQueryRepository<AdminSectionSeen> seen,
    IToolSubmissionQueries submissions,
    IQueryRepository<Domain.Users.ApplicationUser> users,
    IQueryRepository<Domain.Messaging.ContactMessage> contacts)
    : IRequestHandler<GetAdminCountersQuery, AdminCountersResponse>
{
    public async Task<AdminCountersResponse> Handle(GetAdminCountersQuery request, CancellationToken ct)
    {
        var marks = await seen.TableNoTracking.ToListAsync(ct);
        DateTime Seen(string key) =>
            marks.FirstOrDefault(m => m.SectionKey == key)?.LastSeenAt ?? DateTime.MinValue;

        var seenSubs = Seen("submissions");
        var seenCustomers = Seen("customers");
        var seenMessages = Seen("messaging");
        var seenAdviser = Seen("adviser-queue");

        var newSubmissions = await submissions.All
            .Where(s => s.IsActive && s.CreatedAt > seenSubs)
            .CountAsync(ct);

        var newCustomers = await users.TableNoTracking
            .Where(u => u.IsActive && u.CreatedAt > seenCustomers)
            .CountAsync(ct);

        var newMessages = await contacts.TableNoTracking
            .Where(c => c.IsActive && (c.CreatedAt > seenMessages || c.Status == Domain.Messaging.ContactMessageStatus.New))
            .CountAsync(ct);

        // Unseen adviser replies: customer-authored notes newer than the ack —
        // cheap proxy: submissions whose latest change (ModifiedAt) is a new
        // reply would need a join over notes; the queue endpoint already
        // computes waiting counts, so mirror only the "since last seen" total.
        var adviserUnseen = await submissions.All
            .Where(s => s.IsActive && s.ModifiedAt != null && s.ModifiedAt > seenAdviser)
            .CountAsync(ct);

        return new AdminCountersResponse(newSubmissions, newCustomers, newMessages, adviserUnseen);
    }
}

/// <summary>Upserts the caller's seen marker for a section.</summary>
public sealed class MarkAdminSectionSeenCommandHandler(
    ICommandRepository<AdminSectionSeen> seenCommands,
    IQueryRepository<AdminSectionSeen> seenQueries,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<MarkAdminSectionSeenCommand, Guid>
{
    public async Task<Guid> Handle(MarkAdminSectionSeenCommand request, CancellationToken ct)
    {
        if (currentUser.UserId == Guid.Empty)
            throw ValuationException.Forbidden("دسترسی غیرمجاز");
        if (string.IsNullOrWhiteSpace(request.SectionKey))
            throw ValuationException.BadRequest("بخش نامشخص است.");

        var now = DateTime.UtcNow;
        var row = await seenQueries.FirstOrDefaultAsync(
            m => m.UserId == currentUser.UserId && m.SectionKey == request.SectionKey, ct);
        if (row is null)
        {
            row = new AdminSectionSeen
            {
                UserId = currentUser.UserId,
                SectionKey = request.SectionKey,
                LastSeenAt = now,
            };
            await seenCommands.AddAsync(row, ct);
        }
        else
        {
            row.LastSeenAt = now;
            await seenCommands.UpdateAsync(row, ct);
        }
        return row.Id;
    }
}

/// <summary>
///     Server-side aggregates for one tool (admin reporting): overall score
///     min/avg/max, per-dimension min/avg/max, submission count, daily trend
///     and overall-score histogram, with date-range/industry/company filters.
/// </summary>
public sealed class GetAdminToolStatsQueryHandler(IToolSubmissionQueries submissions)
    : IRequestHandler<GetAdminToolStatsQuery, AdminToolStatsResponse>
{
    public async Task<AdminToolStatsResponse> Handle(GetAdminToolStatsQuery request, CancellationToken ct)
    {
        var from = request.From ?? DateTime.MinValue;
        var to = request.To ?? DateTime.MaxValue;
        var toExclusive = to == DateTime.MaxValue ? to : to.Date.AddDays(1);

        var query = submissions.ByTool(request.ToolCode)
            .Where(s => s.IsActive && s.CreatedAt >= from && s.CreatedAt < toExclusive);

        if (!string.IsNullOrWhiteSpace(request.Industry))
            query = query.Where(s => s.Industry == request.Industry);
        if (request.CompanyId is { } cid && cid != Guid.Empty)
            query = query.Where(s => s.CompanyId == cid);

        var rows = await query
            .OrderByDescending(s => s.CreatedAt)
            .Take(2000)
            .Select(s => new { s.CreatedAt, s.OverallScore, s.ResultJson })
            .ToListAsync(ct);

        var scored = rows;

        var overallRows = scored.Where(r => r.OverallScore is { }).ToList();

        AdminToolStatsStat? overall = null;
        if (overallRows.Count > 0)
        {
            var scores = overallRows.Select(r => r.OverallScore!.Value).ToList();
            overall = new AdminToolStatsStat(scores.Min(), scores.Average(), scores.Max(), scores.Count);
        }

        // Per-dimension aggregates over the same metric extraction the public
        // benchmark endpoint uses (sections[]/indicators[]/rows[]/flat map).
        var dims = new Dictionary<string, List<double>>();
        foreach (var r in scored)
        {
            foreach (var (key, value) in ExtractMetrics(r.ResultJson))
            {
                if (!dims.TryGetValue(key, out var list))
                    dims[key] = list = [];
                list.Add(value);
            }
        }

        var dimensions = dims
            .Select(kv => new AdminToolDimensionRow(kv.Key, kv.Value.Min(), kv.Value.Average(), kv.Value.Max(), kv.Value.Count))
            .OrderBy(d => d.Key)
            .ToList();

        var trend = scored
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new AdminToolPoint(g.Key.ToString("yyyy-MM-dd"), (double)g.Count()))
            .OrderBy(p => p.Label)
            .ToList();

        var histogram = scored
            .Where(r => r.OverallScore is { })
            .GroupBy(r => (int)Math.Clamp(Math.Floor(r.OverallScore!.Value / 10), 0, 9))
            .Select(g => new AdminToolPoint($"{g.Key * 10}-{g.Key * 10 + 9}", (double)g.Count()))
            .OrderBy(p => p.Label)
            .ToList();

        return new AdminToolStatsResponse(
            request.ToolCode,
            scored.Count,
            overall,
            dimensions,
            trend,
            histogram);
    }

    /// <summary>Extracts 0-100 metric scores (same heuristic as the benchmark handler).</summary>
    private static List<(string Key, double Score)> ExtractMetrics(string resultJson)
    {
        var list = new List<(string, double)>();
        try
        {
            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return list;

            if (root.TryGetProperty("rows", out var rows) && rows.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var r in rows.EnumerateArray())
                {
                    if (r.ValueKind == JsonValueKind.Object &&
                        r.TryGetProperty("C", out var c) && c.ValueKind == JsonValueKind.Number && c.TryGetDouble(out var cv) &&
                        cv >= 0 && cv <= 100)
                        list.Add(($"axis{i}", cv));
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
                    foreach (var p in new[] {"percent", "score", "value"})
                    {
                        if (s.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d) && d >= 0 && d <= 100)
                        {
                            list.Add((key, d));
                            break;
                        }
                    }
                }
            }
            else if (root.TryGetProperty("indicators", out var indicators) && indicators.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in indicators.EnumerateArray())
                {
                    if (s.ValueKind != JsonValueKind.Object) continue;
                    var key = s.TryGetProperty("key", out var k) && k.ValueKind == JsonValueKind.String ? k.GetString() : null;
                    if (key is null) continue;
                    if (s.TryGetProperty("score", out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d) && d >= 0 && d <= 10)
                        list.Add((key, d * 10));
                }
            }
            else if (root.TryGetProperty("patents", out var patents) && patents.ValueKind == JsonValueKind.Array)
            {
                // IPscore: patents[].scores { legal, tech, market, finance, strategy } (0–100).
                foreach (var p in patents.EnumerateArray())
                {
                    if (p.ValueKind != JsonValueKind.Object) continue;
                    if (!p.TryGetProperty("scores", out var sc) || sc.ValueKind != JsonValueKind.Object) continue;
                    foreach (var prop in sc.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetDouble(out var d) && d >= 0 && d <= 100)
                            list.Add((prop.Name, d));
                    }
                }
            }
            else
            {
                foreach (var p in root.EnumerateObject())
                {
                    if (p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetDouble(out var d) && d >= 0 && d <= 100)
                        list.Add((p.Name, d));
                }
            }
        }
        catch (JsonException)
        {
            // corrupt/legacy row — contributes nothing
        }
        return list;
    }
}
