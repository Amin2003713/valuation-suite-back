using System.Text.Json;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * WIPO Diagnostics — ported from app/wipo-diagnostics/data.ts.
 * 6 IP sections scored by answer index: score = (index+1)*20 per question;
 * section score = avg over ALL questions (unanswered drag down);
 * overall = avg of answered section scores.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class WipoDiagnosticsRunner : IToolRunner
{
    public string ToolCode => "WIPO-DIAG";

    public ToolRunOutcome Run(JsonElement input)
    {
        var i = ToolInput.Bind<WipoInput>(input);

        var sections = new List<WipoSectionScore>();
        foreach (var s in i.Sections ?? [])
        {
            var answers = s.Answers ?? [];
            if (answers.Count == 0) continue;

            var sum = answers.Values.Sum(a => (a + 1) * 20);
            var score = (int)Math.Round(sum / (double)s.QuestionCount);
            sections.Add(new WipoSectionScore(s.Key, score, answers.Count, s.QuestionCount));
        }

        if (sections.Count == 0)
            return new ToolRunOutcome(new WipoResult([], 0, "ضعیف", "هنوز داده‌ای ثبت نشده"), 0, i.Name);

        var overall = (int)Math.Round(sections.Average(s => s.Score));
        var (level, description) = overall switch
        {
            >= 80 => ("عالی", "حفاظت مالکیت فکری در سطح بالا"),
            >= 60 => ("خوب", "حفاظت نسبتاً مناسب با جای پیشرفت"),
            >= 40 => ("متوسط", "نیاز به تقویت در برخی حوزه‌ها"),
            _ => ("ضعیف", "ریسک قابل توجه در حفاظت از دارایی‌های فکری"),
        };

        var result = new WipoResult(sections, overall, level, description);
        return new ToolRunOutcome(result, overall, i.Name);
    }

    public sealed class WipoInput
    {
        public string? Name { get; set; }
        public List<WipoSection>? Sections { get; set; }
    }

    public sealed class WipoSection
    {
        public string Key { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        /// <summary>questionId → chosen option index (0-based).</summary>
        public Dictionary<string, int>? Answers { get; set; }
    }

    public sealed record WipoSectionScore(string Key, int Score, int Answered, int QuestionCount);
    public sealed record WipoResult(List<WipoSectionScore> Sections, int OverallScore, string Level, string Description);
}
