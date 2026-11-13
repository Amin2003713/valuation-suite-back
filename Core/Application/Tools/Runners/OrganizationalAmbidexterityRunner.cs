using System.Text.Json;
using Common.Exceptions;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Organizational ambidexterity diagnostic — ported from the standalone
 * «ابزار تشخیصی دوسویی سازمانی» workbook, operation for operation:
 *
 *   sectionScore = (a1 + a2 + a3) / 3            only when all three answered
 *   status       = score ≥ 3.5 · strong | ≥ 2.5 · fair | else weak
 *   overall      = mean of the five section scores (all five required)
 *   order        = scored sections sorted ascending by score
 *   rank         = position in that order (used for action priority)
 *   priority     = none · score ≥ 3.5 → keep · rank < 2 → high · else medium
 *   percent      = sectionScore / 5 × 100        (radar axis, 0–100)
 *   tips         = question indexes whose answer ≤ 3 (the source's filter)
 *   roadmap      = order split 2 / 2 / 1 into the 90-day, 6- and 12-month groups
 *
 * Scores are returned unrounded, exactly as the source displays them with one
 * decimal. Answer 0 is treated as unanswered, mirroring the source's arrays.
 * Question/option text and solution copy stay in the client's data file (as
 * with the other assessment ports); their ids are the section keys below.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class OrganizationalAmbidexterityRunner : IToolRunner
{
    public string ToolCode => "AMBIDEXTERITY";

    /** Section keys + question count, in display order (the client's tabs). */
    private static readonly (string Key, int Questions)[] Sections =
    [
        ("strategy", 3),
        ("structure", 3),
        ("culture", 3),
        ("processes", 3),
        ("portfolio", 3),
    ];

    private const double StrongMin = 3.5;
    private const double FairMin = 2.5;

    public ToolRunOutcome Run(JsonElement input)
    {
        var i = ToolInput.Bind<AmbidexterityInput>(input);
        var answers = i.Answers ?? new Dictionary<string, double?[]>();

        var scores = new Dictionary<string, double?>();
        var tipIndexes = new Dictionary<string, int[]>();

        foreach (var (key, count) in Sections)
        {
            var arr = answers.TryGetValue(key, out var provided) ? provided : null;

            if (arr is not null && arr.Length != count)
                throw ValuationException.BadRequest(
                    $"بخش «{key}» باید دقیقاً {count} پاسخ داشته باشد. (AMBIDEXTERITY)");

            var values = arr ?? new double?[count];
            for (var j = 0; j < values.Length; j++)
            {
                var v = values[j];
                if (v is null or 0) continue;
                if (v < 1 || v > 5)
                    throw ValuationException.BadRequest(
                        $"پاسخ {j + 1} بخش «{key}» باید بین ۱ تا ۵ باشد. (AMBIDEXTERITY)");
            }

            var answered = values.Where(v => v is not null and not 0).ToArray();
            double? score = answered.Length == count ? answered.Sum(v => v!.Value) / count : null;
            scores[key] = score;

            // Mirrors the source: an unanswered slot inside a submitted array counts
            // as weak (null ≤ 3 in JS); an entirely missing section yields no tips.
            tipIndexes[key] = arr is null
                ? []
                : Enumerable.Range(0, count).Where(j => (values[j] ?? 0) <= 3).ToArray();
        }

        // Sections from weakest to strongest; stable for equal scores (source order).
        var order = Sections.Select(s => s.Key)
            .Where(k => scores[k] is not null)
            .OrderBy(k => scores[k])
            .ToArray();
        var rankOf = order.Select((k, idx) => (k, idx)).ToDictionary(x => x.k, x => x.idx);

        var rows = Sections.Select(s =>
        {
            var score = scores[s.Key];
            var status = score is null ? "none" : score >= StrongMin ? "strong" : score >= FairMin ? "fair" : "weak";
            var priority = score is null ? "todo"
                : score >= StrongMin ? "keep"
                : rankOf.TryGetValue(s.Key, out var rank) && rank < 2 ? "high"
                : "medium";

            return new AmbidexteritySectionResult(
                s.Key,
                score,
                score is null ? null : Math.Round(score.Value / 5 * 100, 1),
                status,
                priority,
                rankOf.TryGetValue(s.Key, out var r) ? r : null,
                tipIndexes[s.Key]);
        }).ToArray();

        var complete = rows.All(r => r.Score is not null);
        double? overall = complete ? rows.Average(r => r.Score!.Value) : null;
        var overallStatus = overall is null ? null
            : overall >= StrongMin ? "strong"
            : overall >= FairMin ? "fair"
            : "weak";

        // 90 days / 6 months / 12 months — empty groups are dropped, as in the source.
        var roadmap = new[] { order.Take(2).ToArray(), order.Skip(2).Take(2).ToArray(), order.Skip(4).ToArray() }
            .Where(g => g.Length > 0)
            .ToArray();

        var result = new AmbidexterityResult(
            rows,
            overall,
            overall is null ? null : Math.Round(overall.Value / 5 * 100, 1),
            overallStatus,
            complete,
            order.Length,
            order,
            roadmap);

        return new ToolRunOutcome(result, result.OverallPercent, i.Name);
    }

    public sealed class AmbidexterityInput
    {
        public string? Name { get; set; }

        /** section key → three answers (1–5, null/0 = unanswered). */
        public Dictionary<string, double?[]>? Answers { get; set; }

        public string? Lang { get; set; }
    }

    public sealed record AmbidexteritySectionResult(
        string Key,
        double? Score,
        double? Percent,
        string Status,
        string Priority,
        int? Rank,
        int[] TipIndexes);

    public sealed record AmbidexterityResult(
        AmbidexteritySectionResult[] Sections,
        double? Overall,
        double? OverallPercent,
        string? OverallStatus,
        bool Complete,
        int AnsweredSections,
        string[] Order,
        string[][] Roadmap);
}
