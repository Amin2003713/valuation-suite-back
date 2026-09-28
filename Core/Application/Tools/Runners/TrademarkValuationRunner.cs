using System.Text.Json;
using Application.Tools.Finance;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Trademark Valuation — ported from app/trademark-valuation/logic.ts.
 * Cost / Relief-from-Royalty / Premium Profit / Market / Scorecard /
 * Probability-weighted / Monte Carlo → reconciliation-weighted final.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class TrademarkValuationRunner : IToolRunner
{
    public string ToolCode => "TRADEMARK-VAL";

    public ToolRunOutcome Run(JsonElement input)
    {
        var s = ToolInput.Bind<TrademarkInput>(input);
        var loc = new ToolLocalization(s.Lang);

        var pastLife        = YearsBetween(s.RegDate, s.EvalDate);
        var remainingLife   = Math.Max(1,   Math.Min(10, s.TermYears));
        var legalRiskFactor = Math.Max(0.5, 1 - s.LegalRisk / 10.0);
        var territoryFactor = 1 + 0.15 * (s.IntlTerritory ? 1 : 0) + 0.05 * (s.Renewable ? 1 : 0);

        // Canonical status code (accepts legacy Persian values and English ones),
        // then localized label back into the result.
        var statusCode = ToolLocalization.NormalizeUseStatus(s.UseStatus);
        var useFactor  = statusCode switch { "active" => 1.0, "developing" => 0.8, _ => 0.5 };
        var useStatusLabel = loc.UseStatusLabel(statusCode);

        var sumFc          = s.Classes.Sum(c => c.Demand * c.Share);
        var sumF           = s.Classes.Sum(c => c.Share);
        var classFinalCoef = sumF > 0 ? sumFc / sumF : 1;

        // Cost approach
        var costRaw   = s.Costs.Sum(c => c.Amount * (1 - c.Depreciation));
        var costValue = costRaw * legalRiskFactor * classFinalCoef;

        // Relief from Royalty
        var    rfrYears = new List<YearRow>();
        double rfrRaw   = 0;

        for (var y = 1; y <= 10; y++)
        {
            var sales   = s.BaseSales * Math.Pow(1 + s.GrowthRate, y);
            var savings = sales * s.RoyaltyRate * (1 - s.TaxRate);
            var pv      = y > remainingLife ? 0 : savings * s.AttributionRate / Math.Pow(1 + s.DiscountRate, y);
            rfrRaw += pv;
            rfrYears.Add(new YearRow(y, sales, savings, pv));
        }

        var rfrValue = rfrRaw * classFinalCoef * legalRiskFactor * territoryFactor * useFactor;

        // Premium Profit
        var    premiumYears = new List<PremiumYearRow>();
        double premiumRaw   = 0;

        for (var y = 1; y <= 10; y++)
        {
            var sales   = s.BaseSales * Math.Pow(1 + s.GrowthRate, y);
            var benefit = sales * s.PremiumPct * s.PremiumMargin * s.AttributionRate;
            var pv      = y > remainingLife ? 0 : benefit / Math.Pow(1 + s.DiscountRate, y);
            premiumRaw += pv;
            premiumYears.Add(new PremiumYearRow(y, sales, benefit, pv));
        }

        var premiumValue = premiumRaw * classFinalCoef * legalRiskFactor;

        // Market
        var sumSim = s.MarketComps.Sum(m => m.Similarity);
        var weightedTxn = sumSim > 0
            ? s.MarketComps.Sum(m => m.Value * m.Similarity * m.Time * m.Geo * m.Similarity) / sumSim
            : 0;

        var weightedRoyalty = sumSim > 0
            ? s.MarketComps.Sum(m => m.Royalty * m.Similarity) / sumSim
            : s.RoyaltyRate;

        var comparableEstimate = s.BaseSales * weightedRoyalty * 10 * s.AttributionRate * classFinalCoef;
        var marketValue        = weightedTxn > 0 ? weightedTxn : comparableEstimate;

        // Scorecard
        var scoreTotal           = s.Scorecard.Sum(r => r.Weight * r.Score);
        var scoreIndex           = scoreTotal / 100;
        var scorecardValue       = Math.Max(costValue, Math.Max(rfrValue, marketValue)) * scoreIndex;
        var scorecardCredibility = Math.Min(1, Math.Max(0.4, scoreTotal / 100));

        // Probability-weighted scenarios
        double ScenarioValue(double growth, double royalty, double discount, double success)
        {
            double sum = 0;

            for (var y = 1; y <= remainingLife; y++)
            {
                var sales = s.BaseSales * Math.Pow(1 + growth, y);
                sum += sales * royalty * (1 - s.TaxRate) * success * s.AttributionRate / Math.Pow(1 + discount, y);
            }

            return sum * classFinalCoef * legalRiskFactor;
        }

        var probRows = s.ProbScenarios
            .Select(x => new ProbRow(x.Name, x.Prob, ScenarioValue(x.Growth, x.Royalty, x.Discount, x.Success)))
            .ToList();
        var probWeightedValue = probRows.Sum(r => r.Prob * r.Value);
        var probSuccess = s.ProbScenarios.Sum(x => x.Prob * x.Success);

        // Monte Carlo
        var n         = s.Mc.Iterations;
        var mcResults = new double[n];

        for (var i = 0; i < n; i++)
        {
            mcResults[i] = ScenarioValue(
                FinanceMath.Triangular(s.Mc.Growth.Min,   s.Mc.Growth.Likely,   s.Mc.Growth.Max),
                FinanceMath.Triangular(s.Mc.Royalty.Min,  s.Mc.Royalty.Likely,  s.Mc.Royalty.Max),
                FinanceMath.Triangular(s.Mc.Discount.Min, s.Mc.Discount.Likely, s.Mc.Discount.Max),
                FinanceMath.Triangular(s.Mc.Success.Min,  s.Mc.Success.Likely,  s.Mc.Success.Max));
        }

        Array.Sort(mcResults);

        double Pct(double p)
            => mcResults[(int)Math.Round(p * (n - 1))];

        var mcMean = mcResults.Average();

        // Reconciliation
        var valueMap = new Dictionary<string, double>
        {
            ["cost"] = costValue,
            ["rfr"] = rfrValue,
            ["premium"] = premiumValue,
            ["market"] = marketValue,
            ["scorecard"] = scorecardValue,
            ["prob"] = probWeightedValue,
            ["mc"] = mcMean,
        };

        var recRows = s.Reconciliation
            .Select(r => new RecRow(r.Key, r.Name, r.Weight, r.Reliability, valueMap.GetValueOrDefault(r.Key), r.Weight * r.Reliability))
            .ToList();

        var sumEff     = recRows.Sum(r => r.EffWeight);
        var finalValue = sumEff > 0 ? recRows.Sum(r => r.Value * r.EffWeight) / sumEff : 0;        var result = new TrademarkResult(
            pastLife, remainingLife, legalRiskFactor, territoryFactor, useFactor, classFinalCoef,
            costRaw, costValue, rfrRaw, rfrValue, rfrYears, premiumRaw, premiumValue, premiumYears,
            marketValue, weightedTxn, weightedRoyalty, comparableEstimate,
            scoreTotal, scoreIndex, scorecardValue, scorecardCredibility,
            probWeightedValue, probSuccess, mcMean, Pct(0.1), Pct(0.5), Pct(0.9), mcResults,
            finalValue, finalValue * 0.8, finalValue * 1.2, useStatusLabel);

        return new ToolRunOutcome(result, null, s.Name);
    }

    private static double YearsBetween(string? d1, string? d2)
    {
        if (!DateOnly.TryParse(d1, out var a) || !DateOnly.TryParse(d2, out var b)) return 0;

        return Math.Max(0, (b.DayNumber - a.DayNumber) / 365.25);
    }

    // ── Input model ──
    public sealed class TrademarkInput
    {
        public string? Name { get; set; }
        public string? Owner { get; set; }
        public string? Lang { get; set; }
        public string? UseStatus { get; set; }
        public string? RegDate { get; set; }
        public string? EvalDate { get; set; }
        public double TermYears { get; set; }
        public double BaseSales { get; set; }
        public double GrowthRate { get; set; }
        public double TaxRate { get; set; }
        public double DiscountRate { get; set; }
        public double RoyaltyRate { get; set; }
        public double AttributionRate { get; set; }
        public double LegalRisk { get; set; }
        public bool Renewable { get; set; }
        public bool IntlTerritory { get; set; }
        public double PremiumPct { get; set; }
        public double PremiumMargin { get; set; }
        public List<TmClass> Classes { get; set; } = [];
        public List<CostItem> Costs { get; set; } = [];
        public List<MarketComp> MarketComps { get; set; } = [];
        public List<ScoreRow> Scorecard { get; set; } = [];
        public List<ProbInput> ProbScenarios { get; set; } = [];
        public McParams Mc { get; set; } = new();
        public List<RecInput> Reconciliation { get; set; } = [];
    }

    public sealed class TmClass
    {
        public double Class { get; set; }
        public double Items { get; set; }
        public double Demand { get; set; }
        public double Competition { get; set; }
        public double Commercial { get; set; }
        public double Share { get; set; }
        public string? Note { get; set; }
    }

    public sealed class CostItem
    {
        public string Name { get; set; } = string.Empty;
        public double Amount { get; set; }
        public double Depreciation { get; set; }
    }

    public sealed class MarketComp
    {
        public double Value { get; set; }
        public double Royalty { get; set; }
        public double Similarity { get; set; }
        public double Time { get; set; }
        public double Geo { get; set; }
    }

    public sealed class ScoreRow
    {
        public string Factor { get; set; } = string.Empty;
        public double Weight { get; set; }
        public double Score { get; set; }
    }    public sealed class ProbInput { public string Name { get; set; } = string.Empty; public double Prob { get; set; } public double Growth { get; set; } public double Royalty { get; set; } public double Discount { get; set; } public double Success { get; set; } }

    public sealed class Range3
    {
        public double Min { get; set; }
        public double Likely { get; set; }
        public double Max { get; set; }
    }

    public sealed class McParams
    {
        public Range3 Growth { get; set; } = new();
        public Range3 Royalty { get; set; } = new();
        public Range3 Discount { get; set; } = new();
        public Range3 Success { get; set; } = new();
        public int Iterations { get; set; } = 5000;
    }

    public sealed class RecInput
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Weight { get; set; }
        public double Reliability { get; set; }
        public string? Note { get; set; }
    }

    // ── Result model ──
    public sealed record YearRow(
        int Year,
        double Sales,
        double Savings,
        double Pv
    );

    public sealed record PremiumYearRow(
        int Year,
        double Sales,
        double Benefit,
        double Pv
    );

    public sealed record ProbRow(
        string Name,
        double Prob,
        double Value
    );

    public sealed record RecRow(
        string Key,
        string Name,
        double Weight,
        double Reliability,
        double Value,
        double EffWeight
    );    public sealed record TrademarkResult(
        double PastLife, double RemainingLife, double LegalRiskFactor, double TerritoryFactor, double UseFactor, double ClassFinalCoef,
        double CostRaw, double CostValue, double RfrRaw, double RfrValue, List<YearRow> RfrYears,
        double PremiumRaw, double PremiumValue, List<PremiumYearRow> PremiumYears,
        double MarketValue, double WeightedTxn, double WeightedRoyalty, double ComparableEstimate,
        double ScoreTotal, double ScoreIndex, double ScorecardValue, double ScorecardCredibility,
        double ProbWeightedValue, double ProbSuccess, double McMean, double McP10, double McP50, double McP90,
        [property: JsonPropertyName("mcResults")] double[] McResults,
        double FinalValue, double Conservative, double Optimistic, string UseStatusLabel);
}