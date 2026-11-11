using System.Text.Json;
using Common.Exceptions;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Innovation Readiness (KTH) — ported from the KTH Innovation Readiness
 * Levels dashboard: 9 readiness levels, 3 questions each, every option a
 * band of the 1–9 readiness scale (question metadata: app/innovation-
 * readiness/data.ts, seeded into the tool's question table).
 *
 *   optionLevel = 1 + (value − 1) × 8 / (options − 1)   → evenly spaced band
 *   levelScore  = round(mean of the answered optionLevels)  → 1..9
 *   average     = round(mean of the level scores, 1)        → 1..9
 *   percent     = average / 9 × 100                          → 0..100 (OverallScore)
 *
 * Status per level (≤3 weak · ≤6 medium · else strong), the overall level
 * text and the strongest/weakest dimensions are computed here; the client
 * only renders them. Strings follow the run language (input "lang").
 * ════════════════════════════════════════════════════════════════════ */

public sealed class InnovationReadinessRunner : IToolRunner
{
    public string ToolCode => "INNOV-READ";

    private sealed record QuestionDef(string Key, int Options);

    private sealed record LevelDef(string Key, QuestionDef[] Questions);

    /** Option counts mirror the client's data file (per-question option sets). */
    private static readonly LevelDef[] Levels =
    [
        new("crl", [new("crl-q1", 4), new("crl-q2", 3), new("crl-q3", 3)]),
        new("trl", [new("trl-q1", 4), new("trl-q2", 3), new("trl-q3", 3)]),
        new("brl", [new("brl-q1", 4), new("brl-q2", 3), new("brl-q3", 3)]),
        new("iprl", [new("iprl-q1", 4), new("iprl-q2", 3), new("iprl-q3", 3)]),
        new("tmrl", [new("tmrl-q1", 4), new("tmrl-q2", 3), new("tmrl-q3", 3)]),
        new("frl", [new("frl-q1", 4), new("frl-q2", 3), new("frl-q3", 3)]),
        new("lrl", [new("lrl-q1", 4), new("lrl-q2", 3), new("lrl-q3", 3)]),
        new("scrl", [new("scrl-q1", 4), new("scrl-q2", 3), new("scrl-q3", 3)]),
        new("srl", [new("srl-q1", 4), new("srl-q2", 3), new("srl-q3", 3)]),
    ];

    private static readonly int TotalQuestions = Levels.Sum(l => l.Questions.Length);

    public ToolRunOutcome Run(JsonElement input)
    {
        var i = ToolInput.Bind<InnoInput>(input);
        var loc = new ToolLocalization(i.Lang);
        var answers = i.Answers ?? [];

        var indicators = new List<InnoIndicatorScore>();
        var answeredQuestions = 0;

        foreach (var level in Levels)
        {
            var optionLevels = new List<double>();

            foreach (var q in level.Questions)
            {
                if (!answers.TryGetValue(q.Key, out var value)) continue;
                if (value < 1 || value > q.Options)
                    throw ValuationException.BadRequest(
                        $"پاسخ «{q.Key}» باید بین ۱ تا {q.Options} باشد. (INNOV-READ)");

                // Evenly spaced band midpoint on the 1–9 readiness scale.
                optionLevels.Add(1 + (value - 1) * 8.0 / (q.Options - 1));
                answeredQuestions++;
            }

            if (optionLevels.Count == 0) continue;

            var score = Round(optionLevels.Average());
            indicators.Add(new InnoIndicatorScore(level.Key, score, PercentOf(score), StatusOf(score, loc)));
        }

        var average = indicators.Count > 0 ? Math.Round(indicators.Average(x => x.Score), 1) : 0;
        var percent = Math.Round(average / 9 * 100, 1);

        var levelText = average switch
        {
            <= 3 => loc.Pick(
                "پروژه در مراحل اولیه آمادگی نوآوری قرار دارد.",
                "The project is at an early stage of innovation readiness."),
            <= 6 => loc.Pick(
                "پروژه در سطح متوسط آمادگی نوآوری قرار دارد و در برخی ابعاد نیاز به توسعه دارد.",
                "The project is at a medium level of innovation readiness and needs development in some dimensions."),
            _ => loc.Pick(
                "پروژه از آمادگی نوآوری بالایی برخوردار است.",
                "The project has a high level of innovation readiness."),
        };

        var strongest = indicators.OrderByDescending(x => x.Score).ThenBy(x => x.Key).FirstOrDefault()?.Key;
        var weakest = indicators.OrderBy(x => x.Score).ThenBy(x => x.Key).FirstOrDefault()?.Key;

        var result = new InnoResult(
            indicators, average, percent, levelText, strongest, weakest,
            answeredQuestions, TotalQuestions);

        return new ToolRunOutcome(result, percent, i.Name);
    }

    /** 1–9 level → 0–100 normalization used by the benchmark radar. */
    private static double PercentOf(double score) => Math.Round(score / 9 * 100, 1);

    private static string StatusOf(double score, ToolLocalization loc) => score switch
    {
        <= 3 => loc.Pick("ضعیف", "Weak"),
        <= 6 => loc.Pick("متوسط", "Medium"),
        _ => loc.Pick("قوی", "Strong"),
    };

    /** JS Math.round: half toward +∞. */
    private static double Round(double x) => Math.Floor(x + 0.5);

    public sealed class InnoInput
    {
        public string? Name { get; set; }

        /** question key → selected option value (1-based). */
        public Dictionary<string, double> Answers { get; set; } = [];

        public string? Lang { get; set; }
    }

    public sealed record InnoIndicatorScore(string Key, double Score, double Percent, string Status);

    public sealed record InnoResult(
        List<InnoIndicatorScore> Indicators, double Average, double Percent, string Level,
        string? Strongest, string? Weakest, int Answered, int Total);
}
