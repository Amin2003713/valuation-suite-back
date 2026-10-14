using System.Text.Json;
using Application.Tools.Options;
using Common.Exceptions;
using Microsoft.Extensions.Options;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Export readiness — ported from app/export-readiness/logic.ts, operation for
 * operation (same accumulation order, same rounding points):
 *   domainScore    = round(Σ (v/5) × (1/answerCount) × 100)   — answered only
 *   completion     = answered / provided answers              (0–1)
 *   domainStatus   = c==0 → waiting | c<1 → incomplete | ≥75 strong | <50 weak | else needs work
 *   overall        = round(Σ roundedDomainScore × weight/100 × 10) / 10  — complete forms only
 *   level          = ≥80 ready · ≥60 conditional · ≥40 developing · else not ready
 * Domain table (ids, weights, question counts) and the status/level cutoffs
 * come from ExportReadinessOptions; the label strings are the source's own.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class ExportReadinessRunner(IOptions<ExportReadinessOptions> options) : IToolRunner
{
    public string ToolCode => "EXPORT-READ";

    /* Status / priority / level strings — verbatim from the source. */
    private const string WaitingLabel = "در انتظار تکمیل";
    private const string IncompleteLabel = "ارزیابی ناقص";
    private const string StrongLabel = "نقطه قوت";
    private const string WeakLabel = "نقطه ضعف";
    private const string NeedsWorkLabel = "نیازمند بهبود";
    private const string PriorityComplete = "تکمیل ارزیابی";
    private const string PriorityUrgent = "فوری";
    private const string PriorityMedium = "میان‌مدت";
    private const string PriorityMaintain = "حفظ و پایش";

    public ToolRunOutcome Run(JsonElement input)
    {
        var i = ToolInput.Bind<ExportReadinessInput>(input);
        var o = options.Value;
        var answers = i.Answers ?? new Dictionary<string, double?[]>();

        var domains = new List<ExportDomainResult>();
        double weightedSum = 0;
        int totalAnswered = 0;
        bool complete = true;

        foreach (var d in o.Domains)
        {
            if (!answers.TryGetValue(d.Id, out var arr) || arr is null)
                throw ValuationException.BadRequest($"پاسخ‌های حوزهٔ {d.Id} ارسال نشده است.");
            if (arr.Length != d.QuestionCount)
                throw ValuationException.BadRequest($"حوزهٔ {d.Id} باید دقیقاً {d.QuestionCount} پاسخ داشته باشد.");
            if (arr.Any(v => v is not null && (v < 0 || v > o.QuestionMax)))
                throw ValuationException.BadRequest($"پاسخ‌های حوزهٔ {d.Id} باید بین ۰ تا {o.QuestionMax} باشند.");

            var answered = arr.Count(v => v is not null);

            // domainScore — round() per domain, exactly where the client rounds.
            double? score = null;
            if (answered > 0)
            {
                var weightPerQ = 1 / (double)arr.Length;
                double sum = 0;
                foreach (var v in arr)
                {
                    if (v is not null) sum += (v.Value / o.QuestionMax) * weightPerQ * 100;
                }
                score = Round(sum);
            }

            var completion = answered / (double)arr.Length;

            var status = completion == 0 ? new DomainStatus(WaitingLabel, "warn")
                : completion < 1 ? new DomainStatus(IncompleteLabel, "warn")
                : score >= o.GoodMin ? new DomainStatus(StrongLabel, "good")
                : score < o.WeakMax ? new DomainStatus(WeakLabel, "bad")
                : new DomainStatus(NeedsWorkLabel, "warn");

            var priority = completion < 1 ? PriorityComplete
                : score < o.WeakMax ? PriorityUrgent
                : score < o.GoodMin ? PriorityMedium
                : PriorityMaintain;

            domains.Add(new ExportDomainResult(d.Id, score, completion, status, priority));
            totalAnswered += answered;
            weightedSum += (score ?? 0) * (d.Weight / 100.0);
            if (answered != d.QuestionCount) complete = false;
        }

        double? overall = complete ? Round(weightedSum * 10) / 10 : null;

        var level = overall is null ? null : ReadinessLevel(overall.Value, o);
        var recommendation = overall is null ? null : Recommendation(overall.Value);

        var result = new ExportReadinessResult(
            domains.ToArray(), complete, overall, level, recommendation, totalAnswered);

        return new ToolRunOutcome(result, overall, i.Name);
    }

    /** JS Math.round: half toward +∞. */
    private static double Round(double x) => Math.Floor(x + 0.5);

    private static DomainStatus ReadinessLevel(double score, ExportReadinessOptions o) =>
        score >= o.ReadyMin ? new DomainStatus("آماده صادرات", "good")
        : score >= o.ConditionalMin ? new DomainStatus("آماده مشروط", "warn")
        : score >= o.DevelopingMin ? new DomainStatus("نیازمند توسعه", "warn")
        : new DomainStatus("فاقد آمادگی فعلی", "bad");

    private static string Recommendation(double score) =>
        score >= 80
            ? "زیرساخت آمادگی در سطح مطلوبی است. ورود مرحله‌ای به بازار هدف، همراه با کنترل مستمر ریسک‌های پرداخت و لجستیک، پیشنهاد می‌شود."
            : score >= 60
            ? "پیش از توسعه جدی بازار، ابتدا حوزه‌های با امتیاز پایین‌تر (نقاط ضعف اولویت‌دار جدول زیر) را اصلاح کنید تا ریسک ورود کاهش یابد."
            : score >= 40
            ? "یک برنامه توسعه صادرات ۳ تا ۶ ماهه با تمرکز بر حوزه‌های ضعیف تدوین کنید؛ ورود به بازار در شرایط فعلی پرریسک است."
            : "ابتدا زیرساخت‌های پایه صادرات (راهبرد، محصول، توان مالی) باید ایجاد شود؛ ورود زودهنگام به بازار توصیه نمی‌شود.";

    public sealed class ExportReadinessInput
    {
        public string? Name { get; set; }
        /** domain id → array of 0–5 points (null = unanswered), one per question. */
        public Dictionary<string, double?[]>? Answers { get; set; }
    }

    public sealed record DomainStatus(string Label, string Cls);

    public sealed record ExportDomainResult(
        string Id, double? Score, double Completion, DomainStatus Status, string Priority);

    public sealed record ExportReadinessResult(
        ExportDomainResult[] Domains, bool Complete, double? Overall,
        DomainStatus? Level, string? Recommendation, int TotalAnswered);
}
