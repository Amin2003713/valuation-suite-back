using System.Text.Json;
using Application.Tools.Options;
using Common.Exceptions;
using Microsoft.Extensions.Options;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Patent Value Index — ported from app/patent-value-index/logic.ts.
 *   leaf points  = param.weight × leaf.f × (score / 10)   (out of 100)
 *   group points = Σ its parameters' leaf points
 *   total        = Σ the three groups' points
 *   param avg    = arithmetic mean of its leaf scores (/10)
 *   group avg    = Σ(paramAvg × param.weight) / Σ(param.weight)
 * Bands and the group/param/leaf tables come from PviOptions (generated from
 * the client's data.ts, so the numbers are the source's own).
 * ════════════════════════════════════════════════════════════════════ */

public sealed class PatentValueIndexRunner(IOptions<PviOptions> options) : IToolRunner
{
    public string ToolCode => "PVI";

    public ToolRunOutcome Run(JsonElement input)
    {
        var i = ToolInput.Bind<PviInput>(input);
        var o = options.Value;

        double Score(string leafId) =>
            i.Scores is not null && i.Scores.TryGetValue(leafId, out var v) ? v : 0;

        foreach (var (id, v) in i.Scores ?? new Dictionary<string, double>())
        {
            if (!o.Groups.SelectMany(g => g.Params).SelectMany(p => p.Leaves).Any(l => l.Id == id))
                throw ValuationException.BadRequest($"شناسهٔ ناشناختهٔ شاخص: {id}");
            if (v < 0 || v > 10)
                throw ValuationException.BadRequest($"امتیاز «{id}» باید بین ۰ تا ۱۰ باشد.");
        }

        var groupAchieved = o.Groups.Select(g =>
            g.Params.Sum(p => p.Leaves.Sum(l => p.Weight * l.F * (Score(l.Id) / 10)))
        ).ToArray();
        var total = groupAchieved.Sum();

        string band = "band5";
        foreach (var (key, min) in o.Bands)
        {
            if (total >= min) { band = key; break; }
        }

        double ParamAvg(PviParamOption p) => p.Leaves.Average(l => Score(l.Id));

        var groupAvgs = o.Groups.Select(g =>
        {
            double sumScore = 0, sumWeight = 0;
            foreach (var p in g.Params)
            {
                sumScore += ParamAvg(p) * p.Weight;
                sumWeight += p.Weight;
            }
            return sumScore / sumWeight;
        }).ToArray();

        var paramAvgs = o.Groups.SelectMany(g => g.Params).Select(ParamAvg).ToArray();

        var result = new PviResult(total, band, groupAchieved, groupAvgs, paramAvgs);
        return new ToolRunOutcome(result, total, i.Name);
    }

    public sealed class PviInput
    {
        public string? Name { get; set; }
        /** leaf id → 0–10 score. */
        public Dictionary<string, double>? Scores { get; set; }
    }

    public sealed record PviResult(
        double Total,
        string Band,
        double[] GroupAchieved,
        double[] GroupAvgs,
        double[] ParamAvgs);
}
