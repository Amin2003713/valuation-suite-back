using System.Text.Json;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Know-How Valuation — ported from app/knowhow-valuation/logic.ts.
 * Reproduction / Replacement cost, RFR income, Market comparables,
 * Scorecard coefficient, probability scenarios → weighted total.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class KnowhowValuationRunner : IToolRunner
{
    public string ToolCode => "KNOWHOW";

    public ToolRunOutcome Run(JsonElement input)
    {
        var i = ToolInput.Bind<KnowhowInput>(input);

        double ov = i.OverheadPct / 100, dp = i.DeveloperProfitPct / 100, ei = i.EntrepreneurIncentivePct / 100;
        double fo = i.FunctionalObsolescencePct / 100, eo = i.EconomicObsolescencePct / 100;

        // Reproduction
        var reproLabor = i.PersonYears * i.SpecialistRate;
        var reproOverhead = reproLabor * ov;
        var reproDirect = reproLabor + reproOverhead;
        var reproDevProfit = reproDirect * dp;
        var reproIncentive = reproDirect * ei;
        var reproGross = reproDirect + reproDevProfit + reproIncentive;
        var reproFunc = reproGross * fo;
        var reproEcon = reproGross * eo;
        var reproFinal = reproGross - reproFunc - reproEcon;

        // Replacement
        var modernPersonYears = i.PersonYears * (1 - i.ModernEfficiencyPct / 100);
        var replLabor = modernPersonYears * i.SpecialistRate;
        var replOverhead = replLabor * ov;
        var replDirect = replLabor + replOverhead;
        var replDevProfit = replDirect * dp;
        var replIncentive = replDirect * ei;
        var replGross = replDirect + replDevProfit + replIncentive;
        var replEcon = replGross * eo;
        var replFinal = replGross - replEcon;

        // RFR income
        var n = Math.Max(1, (int)Math.Round(i.EconomicLifeYears));
        double disc = i.DiscountRate / 100, gr = i.GrowthRate / 100, rr = i.RoyaltyRate / 100;
        double sales = i.BaseSales, rfrTotal = 0;
        var rfrRows = new List<RfrRow>();
        for (var y = 1; y <= n; y++)
        {
            if (y > 1) sales *= 1 + gr;
            var royalty = sales * rr;
            var discFactor = 1 / Math.Pow(1 + disc, y);
            var pv = royalty * discFactor;
            rfrTotal += pv;
            rfrRows.Add(new RfrRow(y, sales, royalty, discFactor, pv));
        }
        var rfrAlt = rr > 0 ? rfrTotal * (i.AltRoyaltyRate / 100.0) / rr : 0;

        // Market
        var marketV1 = i.BaseSales * (i.MarketMultiplierPct / 100.0);
        var marketV2 = i.ReferenceBaseValue * i.MarketAdjustment;

        // Scorecard
        double scWeightSum = 0, scCoef = 0;
        foreach (var r in i.Scorecard)
        {
            scWeightSum += r.Weight;
            scCoef += r.Weight / 100.0 * (r.Score / 100.0);
        }
        var scorecardFinal = i.ReferenceBaseValue * scCoef;

        // Scenarios
        double probSum = 0, expected = 0;
        var scenarioResults = new List<ScenarioResultRow>();
        foreach (var row in i.Scenarios)
        {
            var value = rfrTotal * (row.Mult / 100.0);
            var weighted = row.Prob / 100.0 * value;
            probSum += row.Prob;
            expected += weighted;
            scenarioResults.Add(new ScenarioResultRow(row.Label, value, weighted));
        }

        var weights = i.Weights;
        double W(string key) => (weights?.TryGetValue(key, out var w) ?? false ? w : 0) / 100.0;
        var weightedTotal =
            reproFinal * W("repro") + replFinal * W("repl") + rfrTotal * W("rfr") +
            marketV1 * W("market") + scorecardFinal * W("scorecard") + expected * W("prob");

        var allValues = new[] { reproFinal, replFinal, rfrTotal, marketV1, scorecardFinal, expected };
        var scenarioValues = scenarioResults.Select(r => r.Value).ToList();

        var result = new KnowhowResult(
            reproLabor, reproOverhead, reproDirect, reproDevProfit, reproIncentive, reproGross, reproFunc, reproEcon, reproFinal,
            modernPersonYears, replLabor, replOverhead, replDirect, replDevProfit, replIncentive, replGross, replEcon, replFinal,
            reproFinal - replFinal,
            rfrRows, rfrTotal, rfrAlt,
            marketV1, marketV2, scWeightSum, scCoef, scorecardFinal,
            probSum, expected, scenarioValues, scenarioResults, allValues, weightedTotal);
        return new ToolRunOutcome(result, null, i.Name);
    }

    // ── Input model ──
    public sealed class KnowhowInput
    {
        public string? Name { get; set; }
        public string? Date { get; set; }
        public string? Currency { get; set; }
        public double HistoricalCost { get; set; }
        public double DevYears { get; set; }
        public double PersonYears { get; set; }
        public double SpecialistRate { get; set; }
        public double OverheadPct { get; set; }
        public double DeveloperProfitPct { get; set; }
        public double EntrepreneurIncentivePct { get; set; }
        public double FunctionalObsolescencePct { get; set; }
        public double EconomicObsolescencePct { get; set; }
        public double EconomicLifeYears { get; set; }
        public double DiscountRate { get; set; }
        public double GrowthRate { get; set; }
        public double RoyaltyRate { get; set; }
        public double BaseSales { get; set; }
        public double TechContributionPct { get; set; }
        public double ReferenceBaseValue { get; set; }
        public double MarketMultiplierPct { get; set; }
        public double ModernEfficiencyPct { get; set; }
        public double AltRoyaltyRate { get; set; }
        public double MarketAdjustment { get; set; }
        public List<ScorecardRow> Scorecard { get; set; } = [];
        public List<ScenarioRow> Scenarios { get; set; } = [];
        public Dictionary<string, double>? Weights { get; set; }
    }

    public sealed class ScorecardRow { public string Label { get; set; } = string.Empty; public double Weight { get; set; } public double Score { get; set; } }
    public sealed class ScenarioRow { public string Label { get; set; } = string.Empty; public double Prob { get; set; } public double Mult { get; set; } }

    // ── Result model ──
    public sealed record RfrRow(int Year, double Sales, double Royalty, double Disc, double Pv);
    public sealed record ScenarioResultRow(string Label, double Value, double Weighted);
    public sealed record KnowhowResult(
        double ReproLabor, double ReproOverhead, double ReproDirect, double ReproDevProfit, double ReproIncentive, double ReproGross, double ReproFunc, double ReproEcon, double ReproFinal,
        double ModernPersonYears, double ReplLabor, double ReplOverhead, double ReplDirect, double ReplDevProfit, double ReplIncentive, double ReplGross, double ReplEcon, double ReplFinal,
        double CostDiff,
        List<RfrRow> RfrRows, double RfrTotal, double RfrAlt,
        double MarketV1, double MarketV2, double ScWeightSum, double ScCoef, double ScorecardFinal,
        double ProbSum, double Expected,
        [property: JsonPropertyName("scenarioValues")] List<double> ScenarioValues,
        List<ScenarioResultRow> ScenarioResults,
        [property: JsonPropertyName("allValues")] double[] AllValues,
        double WeightedTotal);
}
