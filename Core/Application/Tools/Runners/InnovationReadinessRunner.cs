using System.Text.Json;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Innovation Readiness (KTH) — ported from app/innovation-readiness/data.ts.
 * 9 readiness indicators (CRL, TRL, BRL, IPRL, TmRL, FRL, LRL, SCRL, SRL);
 * each answered 1..10. Status per indicator + overall average + level.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class InnovationReadinessRunner : IToolRunner
{
    public string ToolCode => "INNOV-READ";

    public ToolRunOutcome Run(JsonElement input)
    {
        var i = ToolInput.Bind<InnoInput>(input);

        var indicators = (i.Scores ?? [])
            .Select(kv => new InnoIndicatorScore(kv.Key, kv.Value, StatusOf(kv.Value)))
            .ToList();

        var avg = indicators.Count > 0 ? Math.Round(indicators.Average(x => x.Score), 1) : 0;
        var level = avg switch
        {
            <= 3 => "پروژه در مراحل اولیه آمادگی نوآوری قرار دارد.",
            <= 6 => "پروژه در سطح متوسط آمادگی نوآوری قرار دارد و در برخی ابعاد نیاز به توسعه دارد.",
            _ => "پروژه از آمادگی نوآوری بالایی برخوردار است.",
        };

        var result = new InnoResult(indicators, avg, level);
        return new ToolRunOutcome(result, avg * 10, i.Name);
    }

    private static string StatusOf(double score) => score switch
    {
        <= 3 => "ضعیف",
        <= 6 => "متوسط",
        _ => "قوی",
    };

    public sealed class InnoInput
    {
        public string? Name { get; set; }
        public Dictionary<string, double> Scores { get; set; } = [];
    }

    public sealed record InnoIndicatorScore(string Key, double Score, string Status);
    public sealed record InnoResult(List<InnoIndicatorScore> Indicators, double Average, string Level);
}
