using System.Text.Json;
using Application.Tools.Finance;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Startup Valuation — ported from app/startup-valuation/logic.ts.
 * 9 methods (DCF, VC, Multiples, Comparable Transactions, Cost-to-Duplicate,
 * Scorecard, Berkus, Risk Factor, First Chicago) combined by stage weights.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class StartupValuationRunner : IToolRunner
{
    private static readonly Dictionary<string, double[]> StageWeights = new()
    {
        ["Pre-Seed"] = [0.00, 0.10, 0.05, 0.00, 0.20, 0.25, 0.25, 0.10, 0.05],
        ["Seed"] = [0.05, 0.20, 0.10, 0.05, 0.10, 0.15, 0.10, 0.10, 0.15],
        ["Series A"] = [0.15, 0.20, 0.15, 0.15, 0.05, 0.05, 0.00, 0.10, 0.15],
        ["Series B+"] = [0.25, 0.20, 0.20, 0.20, 0.00, 0.00, 0.00, 0.05, 0.10],
    };

    private static readonly string[] MethodLabels =
    [
        "DCF", "Venture Capital", "Market Multiples", "Comparable Transactions",
        "Cost-to-Duplicate", "Scorecard", "Berkus", "Risk Factor", "First Chicago",
    ];

    public string ToolCode => "STARTUP-VAL";

    public ToolRunOutcome Run(JsonElement input)
    {
        var s = ToolInput.Bind<StartupInput>(input);

        var waccInfo = ComputeWacc(s);
        var effWacc = waccInfo.Effective;
        var dcf = ComputeDcf(s, effWacc);
        var vc = ComputeVc(s);
        var multiples = ComputeMultiples(s);
        var compTrans = ComputeCompTrans(s);
        var costDup = ComputeCostDup(s);
        var scorecard = ComputeScorecard(s);
        var berkus = ComputeBerkus(s);
        var riskFactor = ComputeRiskFactor(s);
        var firstChicago = ComputeFirstChicago(s, effWacc);

        var values = new[]
        {
            dcf.Value, vc.PreMoney, multiples.Value, compTrans.Value, costDup.Value,
            scorecard.Value, berkus.Value, riskFactor.Value, firstChicago.Value,
        };

        var weights = StageWeights.GetValueOrDefault(s.Stage ?? "Seed") ?? StageWeights["Seed"];
        var weightSum = weights.Sum();
        var final = weightSum > 0 ? values.Select((v, i) => v * weights[i]).Sum() / weightSum : 0;

        var result = new StartupResult(
            waccInfo, dcf, vc, multiples, compTrans, costDup, scorecard, berkus, riskFactor, firstChicago,
            MethodLabels, values, weights, weightSum, final, values.Min(), values.Max());

        return new ToolRunOutcome(result, null, s.Name);
    }

    private static WaccInfo ComputeWacc(StartupInput s)
    {
        var built = s.RiskFreeRate + s.Beta * s.EquityRiskPremium + s.SizePremium + s.SpecificRiskPremium;
        return new WaccInfo(built, s.UseBuiltWacc ? built : s.Wacc);
    }

    private static DcfResult ComputeDcf(StartupInput s, double wacc)
    {
        var rows = new List<DcfRow>();
        double sumPv = 0;
        for (var t = 1; t <= 5; t++)
        {
            var rev = s.Rev0 * Math.Pow(1 + s.Growth, t);
            var ebitda = rev * s.EbitdaMargin;
            var fcf = ebitda * s.FcfConv;
            var disc = 1 / Math.Pow(1 + wacc, t);
            var pv = fcf * disc;
            sumPv += pv;
            rows.Add(new DcfRow(t, rev, ebitda, fcf, disc, pv));
        }
        var last = rows[^1];
        var tv = last.Fcf * (1 + s.LtGrowth) / (wacc - s.LtGrowth);
        var pvTv = tv * last.Disc;
        return new DcfResult(rows, tv, pvTv, sumPv + pvTv);
    }

    private static CostDupResult ComputeCostDup(StartupInput s)
    {
        var c = s.CostDup;
        var totalCost = c.ProductDevCost + c.TeamBuildCost + c.IpBrandCost + c.MarketingToDateCost;
        return new CostDupResult(totalCost, totalCost * c.IntangiblePremium);
    }

    private static CompTransResult ComputeCompTrans(StartupInput s)
    {
        var baseValue = s.Rev0 * s.CompTrans.DealRevMultiple;
        return new CompTransResult(baseValue, baseValue * (1 + s.CompTrans.ControlPremium));
    }

    private static VcResult ComputeVc(StartupInput s)
    {
        var revAtExit = s.Rev0 * Math.Pow(1 + s.Growth, s.YearsToExit);
        var exitValue = revAtExit * s.ExitMultiple;
        var postMoney = exitValue / Math.Pow(1 + s.Irr, s.YearsToExit);
        return new VcResult(revAtExit, exitValue, postMoney, s.CapitalNeeded, postMoney - s.CapitalNeeded);
    }

    private static MultiplesResult ComputeMultiples(StartupInput s)
    {
        var revMultipleValue = s.Rev0 * s.RevMultipleIndustry;
        var ebitdaBase = s.Rev0 * s.EbitdaMargin;
        var ebitdaMultipleValue = ebitdaBase * s.EbitdaMultipleIndustry;
        return new MultiplesResult(revMultipleValue, ebitdaBase, ebitdaMultipleValue, (revMultipleValue + ebitdaMultipleValue) / 2);
    }

    private static ScorecardResult ComputeScorecard(StartupInput s)
    {
        var rows = s.Scorecard.Select(r => new ScorecardRowResult(r.Name, r.Weight, r.Score, r.Weight * r.Score / 5)).ToList();
        var score = rows.Sum(r => r.Weighted);
        return new ScorecardResult(rows, score, s.CompAvgValue, score * s.CompAvgValue);
    }

    private static BerkusResult ComputeBerkus(StartupInput s)
    {
        var rows = s.Berkus.Select(r => new BerkusRowResult(r.Name, r.Score, r.MaxValue, r.Score / 5.0 * r.MaxValue)).ToList();
        return new BerkusResult(rows, rows.Sum(r => r.Value));
    }

    private static RiskFactorResult ComputeRiskFactor(StartupInput s)
    {
        var rows = s.RiskFactor.Select(r => new RiskFactorRowResult(r.Name, r.Score, r.Score * 0.10)).ToList();
        var totalAdj = rows.Sum(r => r.Effect);
        return new RiskFactorResult(rows, totalAdj, s.CompAvgValue, s.CompAvgValue * (1 + totalAdj));
    }

    private static FirstChicagoResult ComputeFirstChicago(StartupInput s, double wacc)
    {
        var disc = 1 / Math.Pow(1 + wacc, s.YearsToExit);
        var rows = s.FirstChicago.Select(r => new FirstChicagoRowResult(r.Name, r.Prob, r.ExitValue, disc, r.Prob * r.ExitValue * disc)).ToList();
        return new FirstChicagoResult(rows, disc, rows.Sum(r => r.Pv));
    }

    // ── Input model ──
    public sealed class StartupInput
    {
        public string? Name { get; set; }
        public string? Stage { get; set; }
        public double Rev0 { get; set; }
        public double Growth { get; set; }
        public double EbitdaMargin { get; set; }
        public double FcfConv { get; set; }
        public double Wacc { get; set; }
        public double LtGrowth { get; set; }
        public double CapitalNeeded { get; set; }
        public double Irr { get; set; }
        public double YearsToExit { get; set; }
        public double ExitMultiple { get; set; }
        public double CompAvgValue { get; set; }
        public double RevMultipleIndustry { get; set; }
        public double EbitdaMultipleIndustry { get; set; }
        public bool UseBuiltWacc { get; set; }
        public double RiskFreeRate { get; set; }
        public double Beta { get; set; }
        public double EquityRiskPremium { get; set; }
        public double SizePremium { get; set; }
        public double SpecificRiskPremium { get; set; }
        public CostDupInput CostDup { get; set; } = new();
        public CompTransInput CompTrans { get; set; } = new();
        public List<NameWeightScore> Scorecard { get; set; } = [];
        public List<NameScoreMax> Berkus { get; set; } = [];
        public List<NameScore> RiskFactor { get; set; } = [];
        public List<NameProbValue> FirstChicago { get; set; } = [];
    }

    public sealed class CostDupInput
    {
        public double ProductDevCost { get; set; }
        public double TeamBuildCost { get; set; }
        public double IpBrandCost { get; set; }
        public double MarketingToDateCost { get; set; }
        public double IntangiblePremium { get; set; } = 1;
    }

    public sealed class CompTransInput
    {
        public double DealRevMultiple { get; set; }
        public double ControlPremium { get; set; }
        public int DealCount { get; set; }
    }

    public sealed record NameWeightScore(string Name, double Weight, double Score);
    public sealed record NameScoreMax(string Name, double Score, double MaxValue);
    public sealed record NameScore(string Name, double Score);
    public sealed record NameProbValue(string Name, double Prob, double ExitValue);

    // ── Result model ──
    public sealed record WaccInfo(double Built, double Effective);
    public sealed record DcfRow(int T, double Rev, double Ebitda, double Fcf, double Disc, double Pv);
    public sealed record DcfResult(List<DcfRow> Rows, double Tv, double PvTv, double Value);
    public sealed record VcResult(double RevAtExit, double ExitValue, double PostMoney, double Investment, double PreMoney);
    public sealed record MultiplesResult(double RevMultipleValue, double EbitdaBase, double EbitdaMultipleValue, double Value);
    public sealed record CompTransResult(double BaseValue, double Value);
    public sealed record CostDupResult(double TotalCost, double Value);
    public sealed record ScorecardRowResult(string Name, double Weight, double Score, double Weighted);
    public sealed record ScorecardResult(List<ScorecardRowResult> Rows, double Score, double Benchmark, double Value);
    public sealed record BerkusRowResult(string Name, double Score, double MaxValue, double Value);
    public sealed record BerkusResult(List<BerkusRowResult> Rows, double Value);
    public sealed record RiskFactorRowResult(string Name, double Score, double Effect);
    public sealed record RiskFactorResult(List<RiskFactorRowResult> Rows, double TotalAdj, double Base, double Value);
    public sealed record FirstChicagoRowResult(string Name, double Prob, double ExitValue, double Disc, double Pv);
    public sealed record FirstChicagoResult(List<FirstChicagoRowResult> Rows, double Disc, double Value);

    public sealed record StartupResult(
        WaccInfo WaccInfo, DcfResult Dcf, VcResult Vc, MultiplesResult Multiples, CompTransResult CompTrans,
        CostDupResult CostDup, ScorecardResult Scorecard, BerkusResult Berkus, RiskFactorResult RiskFactor,
        FirstChicagoResult FirstChicago, string[] MethodLabels, double[] Values, double[] Weights,
        double WeightSum, double Final, double Min, double Max);
}
