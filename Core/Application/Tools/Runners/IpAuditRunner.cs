using System.Text.Json;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * IP Audit — ported from app/ip-audit/data.ts.
 * Records of IP assets with weighted risk scoring:
 * risk = (ownership*0.30 + protection*0.25 + infringement*0.20 +
 *         compliance*0.15 + impact*0.10) * 20  → 0..100
 * ════════════════════════════════════════════════════════════════════ */

public sealed class IpAuditRunner : IToolRunner
{
    private static readonly (string Field, double Weight)[] RiskWeights =
    [
        ("r_ownership", 0.30),
        ("r_protection", 0.25),
        ("r_infringement", 0.20),
        ("r_compliance", 0.15),
        ("r_impact", 0.10),
    ];

    public string ToolCode => "IP-AUDIT";

    public ToolRunOutcome Run(JsonElement input)
    {
        var i = ToolInput.Bind<AuditInput>(input);

        var items = (i.Items ?? []).Select(item =>
        {
            var raw = RiskWeights.Sum(w => GetScore(item.Fields, w.Field) * w.Weight);
            var score = Math.Round(raw * 20, 0);
            var level = score >= 70 ? "high" : score >= 40 ? "medium" : "low";
            return new AuditItemResult(item.Category, item.Fields, score, level);
        }).ToList();

        var avg = items.Count > 0 ? Math.Round(items.Average(x => x.RiskScore), 1) : 0;
        var high = items.Count(x => x.RiskLevel == "high");
        var medium = items.Count(x => x.RiskLevel == "medium");
        var low = items.Count(x => x.RiskLevel == "low");

        var result = new AuditResult(items, avg, high, medium, low);
        return new ToolRunOutcome(result, 100 - avg, i.Name);
    }

    private static double GetScore(Dictionary<string, JsonElement>? fields, string key)
    {
        if (fields is null || !fields.TryGetValue(key, out var el)) return 0;
        if (el.ValueKind == JsonValueKind.Number)
            return el.GetDouble();
        if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(), out var v))
            return v;
        return 0;
    }

    public sealed class AuditInput
    {
        public string? Name { get; set; }
        public List<AuditItem>? Items { get; set; }
    }

    public sealed class AuditItem
    {
        public string Category { get; set; } = string.Empty;
        public Dictionary<string, JsonElement>? Fields { get; set; }
    }

    public sealed record AuditItemResult(string Category, Dictionary<string, JsonElement>? Fields, double RiskScore, string RiskLevel);
    public sealed record AuditResult(List<AuditItemResult> Items, double AverageRisk, int HighCount, int MediumCount, int LowCount);
}
