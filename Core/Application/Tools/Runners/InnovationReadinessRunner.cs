using System.Text.Json;
using Common.Exceptions;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Innovation Readiness — ported from the source dashboard
 * ("innovatonreadinesslevel propexity.html"): nine readiness indicators,
 * each scored directly on 0–9 by the user (10 described levels per
 * indicator). There is no questionnaire aggregation in the source.
 *
 *   average = round(mean of the nine scores, 1)        → 0..9  (display)
 *   percent = mean of the nine scores / 9 × 100        → 0..100 (OverallScore)
 *   band    = ≤3 low · ≤6 medium · else high           (indicator + overall)
 *   gaps    = number of indicators scored ≤ 3          ("gapCount")
 *
 * Analysis groups follow the source exactly: strong = score ≥ 7 (desc, max 3),
 * weak = score ≤ 3 (ascending, max 3), medium = 4..6 (desc, max 3).
 * Content (names, level texts, actions) lives in the client data file; this
 * runner only computes the numbers, bands and group membership.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class InnovationReadinessRunner : IToolRunner
{
    public string ToolCode => "INNOV-READ";

    private static readonly string[] Keys =
        ["crl", "trl", "brl", "iprl", "tmrl", "frl", "lrl", "scrl", "srl"];

    private static readonly int TotalIndicators = Keys.Length;

    public ToolRunOutcome Run(JsonElement input)
    {
        var i = ToolInput.Bind<InnoInput>(input);
        var loc = new ToolLocalization(i.Lang);
        var answers = i.Answers ?? [];

        var scores = Keys
            .Select(key =>
            {
                // The source's selects start at 0, so an omitted key is level 0.
                var value = answers.TryGetValue(key, out var raw) ? raw : 0;
                if (value < 0 || value > 9)
                    throw ValuationException.BadRequest(
                        $"امتیاز «{key}» باید بین ۰ تا ۹ باشد. (INNOV-READ)");
                return (Key: key, Score: value);
            })
            .ToList();

        var mean = scores.Average(x => x.Score);
        var average = Math.Round(mean, 1);
        var percent = Math.Round(mean / 9 * 100, 1);
        var gapCount = scores.Count(x => x.Score <= 3);

        var indicators = scores
            .Select(x => new InnoIndicatorScore(
                x.Key, x.Score, Math.Round(x.Score / 9 * 100, 1), StatusOf(x.Score, loc)))
            .ToList();

        // Stable ranking, ties by the source's indicator order.
        var sorted = scores
            .OrderByDescending(x => x.Score)
            .ThenBy(x => Array.IndexOf(Keys, x.Key))
            .ToList();

        var strongPoints = sorted.Where(x => x.Score >= 7).Take(3).Select(x => x.Key).ToList();
        var weakPoints = sorted.AsEnumerable().Reverse()
            .Where(x => x.Score <= 3).Take(3).Select(x => x.Key).ToList();
        var mediumPoints = sorted
            .Where(x => x.Score is >= 4 and <= 6).Take(3).Select(x => x.Key).ToList();

        var levelText = average switch
        {
            <= 3 => loc.Pick("آمادگی اولیه", "Initial Readiness"),
            <= 6 => loc.Pick("آمادگی متوسط", "Moderate Readiness"),
            _ => loc.Pick("آمادگی پیشرفته", "Advanced Readiness"),
        };

        var answered = Keys.Count(k => answers.ContainsKey(k));

        var result = new InnoResult(
            indicators, average, percent, levelText,
            sorted.FirstOrDefault().Key, sorted.LastOrDefault().Key,
            gapCount, strongPoints, weakPoints, mediumPoints,
            answered, TotalIndicators);

        return new ToolRunOutcome(result, percent, i.Name);
    }

    private static string StatusOf(double score, ToolLocalization loc) => score switch
    {
        <= 3 => loc.Pick("نیازمند اقدام فوری", "Requires Immediate Action"),
        <= 6 => loc.Pick("در حال توسعه", "In Development"),
        _ => loc.Pick("نقطه قوت", "Strength"),
    };

    public sealed class InnoInput
    {
        public string? Name { get; set; }

        /** indicator key → chosen readiness level (0..9). */
        public Dictionary<string, double> Answers { get; set; } = [];

        public string? Lang { get; set; }
    }

    public sealed record InnoIndicatorScore(string Key, double Score, double Percent, string Status);

    public sealed record InnoResult(
        List<InnoIndicatorScore> Indicators, double Average, double Percent, string Level,
        string? Strongest, string? Weakest, int GapCount,
        List<string> StrongPoints, List<string> WeakPoints, List<string> MediumPoints,
        int Answered, int Total);
}
