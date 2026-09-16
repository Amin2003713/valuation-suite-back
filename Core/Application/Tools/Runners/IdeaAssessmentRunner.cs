using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Idea Assessment (ایده‌سنج) — ported from app/idea-assessment/data.ts.
 * Sections carry maxScore weights; each question's options carry explicit
 * values (1..5). Section percent = sum/maxScore*100; total = Σ section
 * percents (sections already weight to ~100 via their maxScores).
 * ════════════════════════════════════════════════════════════════════ */

public sealed class IdeaAssessmentRunner : IToolRunner
{
    public const double TotalMaxScore = 135;

    public string ToolCode => "IDEA-ASSESS";

    public ToolRunOutcome Run(JsonElement input)
    {
        var i = ToolInput.Bind<IdeaInput>(input);
        var sections = new List<IdeaSectionScore>();

        foreach (var section in i.Sections ?? [])
        {
            var answered = (section.Answers ?? []).Where(kv => kv.Value > 0).ToList();
            var sum = answered.Sum(kv => kv.Value);
            var percent = section.MaxScore > 0 ? Math.Round(sum / section.MaxScore * 100, 1) : 0;
            sections.Add(new IdeaSectionScore(section.Key, section.Title, section.MaxScore, sum, percent, answered.Count));
        }

        var total = Math.Round(sections.Sum(s => s.Percent), 1);
        var (level, description) = total switch
        {
            >= 100 => ("بسیار قوی", "ایده بسیار قوی و با پتانسیل بالا"),
            >= 70 => ("خوب", "ایده با پتانسیل خوب"),
            >= 40 => ("متوسط", "ایده متوسط با نیاز به بهبود"),
            _ => ("ضعیف", "ایده ضعیف با ریسک بالا"),
        };

        var result = new IdeaResult(sections, total, TotalMaxScore, level, description);
        return new ToolRunOutcome(result, Math.Min(100, total / TotalMaxScore * 100), i.Name);
    }

    public sealed class IdeaInput
    {
        public string? Name { get; set; }
        public List<IdeaSection> Sections { get; set; } = [];
    }

    public sealed class IdeaSection
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public double MaxScore { get; set; }
        public Dictionary<string, double> Answers { get; set; } = [];
    }

    public sealed record IdeaSectionScore(string Key, string Title, double MaxScore, double Sum, double Percent, int Answered);

    public sealed record IdeaResult(List<IdeaSectionScore> Sections, double Total, double MaxTotal, string Level, string Description);
}
