using System.Text.Json;
using Application.Tools.Options;
using Common.Exceptions;
using Microsoft.Extensions.Options;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Technology/commercial readiness maturity — ported from app/maturity-assessment/logic.ts:
 *   coeff          = evidence: valid & current → 1 · limited/old → 0.75 · else 0.5
 *   adjusted       = score × coeff
 *   domainScore    = round(Σ adj / answered / 5 × 100)        (null when none answered)
 *   completion     = answered / 60                            (0–1)
 *   total          = round((Σ adj × domainWeight) / 30 × 100 × 10) / 10 — complete forms only
 *   critical gate  = any critical indicator with score < 3 → failed (checked after the waiting state)
 *   level          = gate, then 80 / 60 / 40
 * Domain weights, question→domain map, critical flags, specials and cutoffs
 * come from MaturityOptions (generated from the client's data.ts).
 * ════════════════════════════════════════════════════════════════════ */

public sealed class MaturityAssessmentRunner(IOptions<MaturityOptions> options) : IToolRunner
{
    public string ToolCode => "MATURITY";

    /* Verbatim source strings. */
    private const string NoData = "بدون داده";
    private const string AnalysisGood = "نقطه قوت";
    private const string AnalysisWeak = "نقطه ضعف";
    private const string AnalysisNeedsWork = "نیازمند بهبود";
    private const string WaitingComplete = "در انتظار تکمیل";
    private const string Incomplete = "ارزیابی ناقص";
    private const string CriticalFail = "دارای نقص بحرانی";
    private const string CriticalPass = "همه شروط قبول";
    private const string CondFailed = "آمادگی مشروط؛ رفع نقص حیاتی";
    private const string LevelMature = "بالغ و آماده توسعه";
    private const string LevelStructured = "ساختاریافته";
    private const string LevelDeveloping = "در حال توسعه";
    private const string LevelInitial = "مقدماتی و فردمحور";
    private const string PriorityHigh = "بالا";
    private const string PrioritySuggested = "پیشنهادی";
    private const string PriorityNotNow = "فعلاً ضروری نیست";

    public ToolRunOutcome Run(JsonElement input)
    {
        var i = ToolInput.Bind<MaturityInput>(input);
        var o = options.Value;

        var answers = i.Answers ?? new Dictionary<string, MaturityAnswerInput>();
        MaturityAnswerInput? A(int idx) => answers.TryGetValue(idx.ToString(), out var v) ? v : null;
        bool GoalActive(string key) =>
            i.Goals is not null && i.Goals.TryGetValue(key, out var v) && v == o.YesValue;

        double Coeff(string? evidence) =>
            evidence is not null && o.EvidenceCoefficients.TryGetValue(evidence, out var c)
                ? c
                : o.EvidenceDefaultCoefficient;

        double? Adj(int idx)
        {
            var a = A(idx);
            return a?.Score is null ? null : a.Score * Coeff(a.Evidence);
        }

        var entries = o.Questions.OrderBy(q => q.Index).ToArray();

        double? DomainScore(string key)
        {
            var qs = entries.Where(q => q.Domain == key).ToArray();
            var answered = qs.Select(q => Adj(q.Index)).Where(a => a is not null).ToArray();
            if (answered.Length == 0) return null;
            return Round(answered.Sum(a => a!.Value) / answered.Length / 5 * 100);
        }

        static string DomainAnalysis(double? score) =>
            score is null ? NoData : score >= 75 ? AnalysisGood : score < 50 ? AnalysisWeak : AnalysisNeedsWork;

        // ── Per domain ───────────────────────────────────────────────────────
        var domains = o.Domains.Select(d =>
        {
            var s = DomainScore(d.Key);
            return new MaturityDomainResult(d.Key, s, DomainAnalysis(s));
        }).ToArray();

        // ── Whole assessment ─────────────────────────────────────────────────
        var adjs = entries.Select(q => Adj(q.Index)).ToArray();
        var answeredCount = adjs.Count(a => a is not null);
        var completion = answeredCount / (double)o.TotalQuestions;

        double? total = null;
        if (completion >= 1)
        {
            double sum = 0;
            for (var k = 0; k < entries.Length; k++)
                sum += (adjs[k] ?? 0) * WeightOf(entries[k].Domain);
            total = Round(sum / o.DomainMaxRaw * 100 * 10) / 10;
        }

        double WeightOf(string domainKey) =>
            o.Domains.First(d => d.Key == domainKey).Weight;

        // Critical gate: waiting beats failed, exactly as the source's ordering.
        var criticalResults = entries
            .Where(q => q.Critical)
            .Select(q => A(q.Index)?.Score is null ? "wait" : A(q.Index)!.Score < o.CriticalThreshold ? "fail" : "pass")
            .ToArray();
        var criticalStatus = criticalResults.Contains("wait") ? WaitingComplete
            : criticalResults.Contains("fail") ? CriticalFail
            : CriticalPass;

        string level;
        if (completion == 0) level = WaitingComplete;
        else if (completion < 1) level = Incomplete;
        else if (criticalStatus == CriticalFail) level = CondFailed;
        else if (total >= 80) level = LevelMature;
        else if (total >= 60) level = LevelStructured;
        else if (total >= 40) level = LevelDeveloping;
        else level = LevelInitial;

        // ── Per question ─────────────────────────────────────────────────────
        var perQuestion = entries.Select((q, k) => new MaturityQuestionResult(
            q.Index, adjs[k],
            !q.Critical ? "-"
            : A(q.Index)?.Score is null ? "در انتظار"
            : A(q.Index)!.Score < o.CriticalThreshold ? "رد"
            : "قبول")).ToArray();

        // ── Specialist assessments ───────────────────────────────────────────
        var specials = o.Specials.Select(s =>
        {
            var status = DomainAnalysis(DomainScore(s.DomainKey));
            var priority = GoalActive(s.GoalKey) ? PriorityHigh
                : status is AnalysisWeak or AnalysisNeedsWork ? PrioritySuggested
                : PriorityNotNow;
            return new MaturitySpecialResult(status, priority);
        }).ToArray();

        var result = new MaturityResult(domains, completion, total, criticalStatus, level, perQuestion, specials);
        return new ToolRunOutcome(result, total ?? 0, i.Name);
    }

    /** JS Math.round: half toward +∞. */
    private static double Round(double x) => Math.Floor(x + 0.5);

    public sealed class MaturityInput
    {
        public string? Name { get; set; }
        /** question idx (0-based, string key) → { score 0–5|null, evidence, notes }. */
        public Dictionary<string, MaturityAnswerInput>? Answers { get; set; }
        /** goal toggles: goal key → 'بله' / 'خیر'. */
        public Dictionary<string, string>? Goals { get; set; }
    }

    public sealed class MaturityAnswerInput
    {
        public double? Score { get; set; }
        public string? Evidence { get; set; }
        public string? Notes { get; set; }
    }

    public sealed record MaturityDomainResult(string Key, double? Score, string Analysis);

    public sealed record MaturityQuestionResult(int Id, double? Adj, string Verdict);

    public sealed record MaturitySpecialResult(string Status, string Priority);

    public sealed record MaturityResult(
        MaturityDomainResult[] Domains, double Completion, double? Total,
        string CriticalStatus, string Level, MaturityQuestionResult[] PerQuestion,
        MaturitySpecialResult[] Specials);
}
