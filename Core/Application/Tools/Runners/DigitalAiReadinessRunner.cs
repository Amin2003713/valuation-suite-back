using System.Text.Json;
using Application.Tools.Options;
using Common.Exceptions;
using Microsoft.Extensions.Options;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Digital & AI readiness — ported from app/digital-ai-readiness/logic.ts:
 *   per section (D first, then A):
 *     cov    = answered / QuestionsPerSection        (0–1)
 *     score  = mean of CODE_SCORE[code-1] over answered, or null
 *     ok     = cov >= CoverageOk && score != null
 *     lv     = ok ? (score < 50 ? 0 : score < 80 ? 1 : 2) : null
 *     unk    = critical questions left unanswered
 *     red    = red alerts raised by this section (question code 3, or an
 *              indicator beyond its red threshold)
 *   alerts are then sorted red-before-yellow, critical first (stable).
 * Two source quirks are reproduced: an unanswered "don't know" indicator never
 * raises an alert, and a blank critical indicator does not count as unassessed.
 * Questions, indicator thresholds and risk texts come from DigitalAiOptions.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class DigitalAiReadinessRunner(IOptions<DigitalAiOptions> options) : IToolRunner
{
    public string ToolCode => "DIGITAL-AI";

    public ToolRunOutcome Run(JsonElement input)
    {
        var i = ToolInput.Bind<DigitalAiInput>(input);
        var o = options.Value;

        var lang = i.Lang == "en" ? "en" : "fa";
        var codeScores = o.CodeScores;

        double? AOf(string id) => i.A is not null && i.A.TryGetValue(id, out var v) ? v : null;
        double? TOf(string id) => i.T is not null && i.T.TryGetValue(id, out var v) ? v : null;

        // Validate answer codes (1/2/3) once, up front.
        foreach (var (id, code) in i.A ?? new Dictionary<string, double>())
        {
            if (code < 1 || code > codeScores.Length || code != Math.Floor(code))
                throw ValuationException.BadRequest($"کد پاسخ «{id}» نامعتبر است (۱ تا ۳).");
        }

        var alerts = new List<Alert>();
        var sections = new List<SectionResult>();

        // Same section walk as the client: D, then A.
        foreach (var sec in new[] { "D", "A" })
        {
            var qs = o.Questions.Where(q => q.Section == sec).ToArray();

            var answered = qs.Where(q => AOf(q.Id) is not null).ToArray();
            var cov = answered.Length / (double)o.QuestionsPerSection;
            double? score = answered.Length == 0
                ? null
                : answered.Sum(q => codeScores[(int)AOf(q.Id)!.Value - 1]) / answered.Length;
            var ok = cov >= o.CoverageOk && score is not null;
            int? lv = ok ? (score! < o.Level1Max ? 0 : score! < o.Level2Max ? 1 : 2) : null;

            int unk = 0;
            foreach (var q in qs)
            {
                var a = AOf(q.Id);
                if (a is 2 or 3)
                    alerts.Add(new Alert(q.Id, lang == "fa" ? q.RiskFa : q.RiskEn, a == 3, q.Critical, sec));
                // Source quirk: only a blank CRITICAL QUESTION counts as unassessed.
                if (q.Critical && a is null) unk++;
            }

            foreach (var t in o.Indicators.Where(x => x.Section == sec))
            {
                var v = TOf(t.Id);
                if (v is null)
                    continue; // source quirk: blank / "don't know" never alerts

                var isRed = t.Dir == "l" ? v < t.Red : v > t.Red;
                var isYellow = t.Dir == "l" ? v < t.Yellow : v > t.Yellow;
                if (isRed || isYellow)
                {
                    var baseQ = o.Questions.FirstOrDefault(g => g.Id == t.Base);
                    var text = baseQ is null ? "" : lang == "fa" ? baseQ.RiskFa : baseQ.RiskEn;
                    alerts.Add(new Alert(t.Id, text, isRed, t.Critical, sec));
                }
            }

            var red = alerts.Count(al => al.Sec == sec && al.R);
            sections.Add(new SectionResult(sec, cov, score, ok, lv, red, unk));
        }

        // Stable sorts replicate Array.prototype.sort's stability.
        var sorted = alerts.OrderByDescending(al => al.R).ThenByDescending(al => al.C).ToArray();

        var result = new DigitalResult(sections.ToArray(), sorted);
        var nonNull = sections.Where(s => s.Score is not null).Select(s => s.Score!.Value).ToArray();
        var summary = nonNull.Length > 0 ? nonNull.Average() : (double?)null;
        return new ToolRunOutcome(result, summary, i.Name);
    }

    public sealed class DigitalAiInput
    {
        public string? Name { get; set; }
        public string? Lang { get; set; }
        /** question id → chosen code (1 good, 2 intermediate, 3 serious weakness). */
        public Dictionary<string, double>? A { get; set; }
        /** indicator id → numeric value, or null when blank. */
        public Dictionary<string, double?>? T { get; set; }
        /** indicator id → state index (unused by the model — kept for input fidelity). */
        public Dictionary<string, double>? Ts { get; set; }
    }

    public sealed record SectionResult(
        string Sec, double Cov, double? Score, bool Ok, int? Lv, int Red, int Unk);

    public sealed record Alert(string Id, string T, bool R, bool C, string Sec);

    public sealed record DigitalResult(SectionResult[] Sections, Alert[] Alerts);
}
