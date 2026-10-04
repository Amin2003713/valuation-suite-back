using System.Text.Json;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Patent Search (جستجوی پتنت) — the search itself runs client-side against
 * Lens.org (free API). This runner persists the search as a tool submission
 * so the run shows up in "previous results" and — most importantly — the
 * paid adviser chat becomes available for it, like every other tool.
 * ════════════════════════════════════════════════════════════════════ */
public sealed class PatentSearchRunner : IToolRunner
{
    public string ToolCode => "PATENT-SEARCH";

    public ToolRunOutcome Run(JsonElement input)
    {
        var i = ToolInput.Bind<PatentSearchInput>(input);

        var keywords = new[] { i.Title1, i.Title2, i.Abstract1, i.Abstract2, i.Claim1, i.Claim2 }
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .ToList();

        var result = new PatentSearchResult(
            Keywords: keywords,
            KeywordCount: keywords.Count,
            Query: string.Join(" OR ", keywords.Select(k => $"\"{k}\"")),
            SearchedAtUtc: DateTime.UtcNow);

        return new ToolRunOutcome(result, null, i.Name);
    }

    public sealed class PatentSearchInput
    {
        public string? Name { get; set; }
        public string? Lang { get; set; }
        public string? Title1 { get; set; }
        public string? Title2 { get; set; }
        public string? Abstract1 { get; set; }
        public string? Abstract2 { get; set; }
        public string? Claim1 { get; set; }
        public string? Claim2 { get; set; }
    }

    public sealed record PatentSearchResult(
        List<string> Keywords, int KeywordCount, string Query, DateTime SearchedAtUtc);
}
