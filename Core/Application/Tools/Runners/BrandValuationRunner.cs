using System.Text.Json;
using Application.Tools.Finance;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Brand Valuation — ported from app/brand-valuation/logic.ts.
 * Strength scores → royalty engine → RFR / Premium Profit / Incremental CF /
 * Scenarios / Monte Carlo / Tornado → weighted final.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class BrandValuationRunner : IToolRunner
{
    public string ToolCode => "BRAND-VAL";

    public ToolRunOutcome Run(JsonElement input)
    {
        var s = ToolInput.Bind<BrandInput>(input);

        var scorecard = ComputeStrength(s.Scorecard);
        var legal = ComputeStrength(s.Legal);
        var royalty = ComputeRoyaltyEngine(s, scorecard.Factor, legal.Factor);
        var forecast = ComputeForecast(s);
        var rfr = ComputeRfr(s, forecast, royalty.Adjusted);
        var pp = ComputePremiumProfit(s);
        var icf = ComputeIncrementalCf(s, forecast);
        var scen = ComputeScenarios(s, rfr.Value);
        var mc = ComputeMonteCarlo(s, rfr.Value, royalty);
        var tornado = ComputeTornado(s, rfr.Value);
        var final = ComputeFinal(s, rfr.Value, pp.Value, icf.Value, scen.ExpectedValue, mc.Mean);

        var result = new BrandResult(scorecard, legal, royalty, forecast, rfr, pp, icf, scen, mc, tornado, final);
        return new ToolRunOutcome(result, null, s.BrandName);
    }

    private static StrengthResult ComputeStrength(List<WeightedRow> rows)
    {
        var scoreSum = rows.Sum(r => r.Weighted);
        var score100 = scoreSum * 10;
        return new StrengthResult(rows, score100, score100 / 100);
    }

    private static RoyaltyResult ComputeRoyaltyEngine(BrandInput s, double brandStrength, double legalStrength)
    {
        var row = s.IndustryDb?.FirstOrDefault(r => r.Name == s.Industry) ?? s.IndustryDb?.FirstOrDefault()
                  ?? new IndustryRow("سایر", 0.01, 0.05, 0.12, null);
        var composite = brandStrength * s.BrandWeight + legalStrength * s.LegalWeight;
        var adjusted = FinanceMath.Clamp(row.Base * (0.5 + composite / 2), row.Low, row.High);
        return new RoyaltyResult(row, brandStrength, legalStrength, composite, adjusted,
            Math.Max(row.Low, adjusted * 0.7), adjusted, Math.Min(row.High, adjusted * 1.3));
    }

    private static ForecastResult ComputeForecast(BrandInput s)
    {
        var rows = s.Forecast.Revenue.Select((rev, i) =>
        {
            var ebit = rev * s.Forecast.Margin[i];
            var nopat = ebit * (1 - s.Forecast.TaxYear[i]);
            return new ForecastRow(i + 1, rev, s.Forecast.Growth[i], s.Forecast.Margin[i], ebit, s.Forecast.TaxYear[i], nopat);
        }).ToList();
        return new ForecastResult(rows);
    }

    private static RfrResult ComputeRfr(BrandInput s, ForecastResult forecast, double royaltyRate)
    {
        var rows = forecast.Rows.Select(r =>
        {
            var saving = r.Revenue * royaltyRate;
            var afterTax = saving * (1 - s.TaxRate);
            var disc = 1 / Math.Pow(1 + s.DiscountRate, r.T);
            var pv = afterTax * disc;
            return new RfrRow(r.T, r.Revenue, royaltyRate, saving, afterTax, disc, pv);
        }).ToList();
        var pvExplicit = rows.Sum(r => r.Pv);
        var last = rows[^1];
        var tv = s.DiscountRate <= s.TerminalGrowth
            ? 0
            : last.AfterTax * (1 + s.TerminalGrowth) / (s.DiscountRate - s.TerminalGrowth) * last.Disc;
        return new RfrResult(rows, pvExplicit, tv, pvExplicit + tv);
    }

    private static PremiumProfitResult ComputePremiumProfit(BrandInput s)
    {
        var p = s.PremiumProfit;
        var pricePremiumPct = (p.BrandPrice - p.ComparablePrice) / p.ComparablePrice;
        var annual = (p.BrandPrice - p.ComparablePrice) * p.AnnualVolume * p.AttributableMargin / 1_000_000;
        return new PremiumProfitResult(pricePremiumPct, annual, annual * p.CapMultiple);
    }

    private static IcfResult ComputeIncrementalCf(BrandInput s, ForecastResult forecast)
    {
        var rows = forecast.Rows.Select(r =>
        {
            var withBrand = r.Nopat;
            var withoutBrand = withBrand * s.NoBrandFactor;
            var incremental = withBrand - withoutBrand;
            var disc = 1 / Math.Pow(1 + s.DiscountRate, r.T);
            return new IcfRow(r.T, withBrand, withoutBrand, incremental, disc, incremental * disc);
        }).ToList();
        return new IcfResult(rows, rows.Sum(r => r.Pv));
    }

    private static ScenResult ComputeScenarios(BrandInput s, double rfrValue)
    {
        var rows = s.Scenarios.Select(r => new ScenRow(r.Name, r.Prob, r.Mult, r.Assump, rfrValue * r.Mult)).ToList();
        return new ScenResult(rows, rows.Sum(r => r.Prob * r.Value), rows.Sum(r => r.Prob));
    }

    private static McResult ComputeMonteCarlo(BrandInput s, double rfrValue, RoyaltyResult royalty)
    {
        var n = s.Mc.N;
        var values = new double[n];
        for (var i = 0; i < n; i++)
        {
            var g = FinanceMath.Triangular(s.Mc.GrowthLow, s.Mc.GrowthMode, s.Mc.GrowthHigh);
            var roy = FinanceMath.Triangular(royalty.TriLow, royalty.TriMode, royalty.TriHigh);
            var disc = FinanceMath.Triangular(s.Mc.DiscLow, s.Mc.DiscMode, s.Mc.DiscHigh);
            var revF = FinanceMath.Triangular(s.Mc.RevFLow, s.Mc.RevFMode, s.Mc.RevFHigh);
            values[i] = rfrValue * (roy / royalty.Adjusted) * revF
                        * ((1 + s.DiscountRate) / (1 + disc)) * ((1 + g) / (1 + s.Mc.GrowthMode));
        }
        var (mean, p10, p50, p90, min, max, stdev, hist, labels) = FinanceMath.Summarize(values);
        return new McResult(values, mean, p50, p10, p90, min, max, stdev, hist, labels);
    }

    private static List<TornadoRow> ComputeTornado(BrandInput s, double rfrValue)
    {
        var rows = new (string Name, double Low, double High)[]
        {
            ("Revenue", rfrValue * (1 - s.Shock), rfrValue * (1 + s.Shock)),
            ("Royalty Rate", rfrValue * (1 - s.Shock), rfrValue * (1 + s.Shock)),
            ("Discount Rate", rfrValue * (1 + s.Shock), rfrValue * (1 - s.Shock)),
            ("Long-term Growth", rfrValue * (1 - s.Shock * 0.7), rfrValue * (1 + s.Shock * 0.7)),
            ("Tax Rate", rfrValue * (1 + s.Shock * 0.4), rfrValue * (1 - s.Shock * 0.4)),
            ("Brand Strength", rfrValue * (1 - s.Shock * 0.6), rfrValue * (1 + s.Shock * 0.6)),
        }.Select(r =>
        {
            var impactLow = r.Low - rfrValue;
            var impactHigh = r.High - rfrValue;
            return new TornadoRow(r.Name, r.Low, r.High, impactLow, impactHigh, Math.Abs(impactLow) + Math.Abs(impactHigh));
        })
        .OrderByDescending(r => r.Range)
        .ToList();
        return rows;
    }

    private static FinalResult ComputeFinal(BrandInput s, double rfr, double pp, double icf, double scen, double mc)
    {
        var w = s.FinalWeights;
        var items = new List<FinalItem>
        {
            new("Relief from Royalty", rfr, w.Rfr),
            new("Premium Profit", pp, w.Pp),
            new("Incremental Cash Flow", icf, w.Icf),
            new("Scenario Expected Value", scen, w.Scen),
            new("Monte Carlo Mean", mc, w.Mc),
        };
        var weightSum = items.Sum(r => r.Weight);
        return new FinalResult(items, weightSum, items.Sum(r => r.Value * r.Weight));
    }

    // ── Input model ──
    public sealed class BrandInput
    {
        public string? BrandName { get; set; }
        public string? Industry { get; set; }
        public int BaseYear { get; set; }
        public string? Currency { get; set; }
        public double TaxRate { get; set; }
        public double DiscountRate { get; set; }
        public double TerminalGrowth { get; set; }
        public double LegalWeight { get; set; }
        public double BrandWeight { get; set; }
        public double NoBrandFactor { get; set; }
        public double Shock { get; set; }
        public List<IndustryRow>? IndustryDb { get; set; }
        public List<WeightedRow> Scorecard { get; set; } = [];
        public List<WeightedRow> Legal { get; set; } = [];
        public ForecastInput Forecast { get; set; } = new();
        public PremiumProfitInput PremiumProfit { get; set; } = new();
        public List<ScenInput> Scenarios { get; set; } = [];
        public McInput Mc { get; set; } = new();
        public FinalWeights FinalWeights { get; set; } = new();
    }

    public sealed record IndustryRow(string Name, double Low, double Base, double High, string? Note);
    public sealed class ForecastInput
    {
        public List<double> Revenue { get; set; } = [];
        public List<double> Growth { get; set; } = [];
        public List<double> Margin { get; set; } = [];
        public List<double> TaxYear { get; set; } = [];
    }
    public sealed class PremiumProfitInput
    {
        public double BrandPrice { get; set; }
        public double ComparablePrice { get; set; }
        public double AnnualVolume { get; set; }
        public double AttributableMargin { get; set; }
        public double CapMultiple { get; set; }
    }
    public sealed class ScenInput
    {
        public string Name { get; set; } = string.Empty;
        public double Prob { get; set; }
        public double Mult { get; set; }
        public string? Assump { get; set; }
    }
    public sealed class McInput
    {
        public int N { get; set; } = 1000;
        public double GrowthLow { get; set; }
        public double GrowthMode { get; set; }
        public double GrowthHigh { get; set; }
        public double DiscLow { get; set; }
        public double DiscMode { get; set; }
        public double DiscHigh { get; set; }
        public double RevFLow { get; set; }
        public double RevFMode { get; set; }
        public double RevFHigh { get; set; }
    }
    public sealed class FinalWeights
    {
        public double Rfr { get; set; }
        public double Pp { get; set; }
        public double Icf { get; set; }
        public double Scen { get; set; }
        public double Mc { get; set; }
    }

    // ── Result model ──
    public sealed record StrengthResult(List<WeightedRow> Rows, double Score100, double Factor);
    public sealed record RoyaltyResult(IndustryRow Industry, double BrandStrength, double LegalStrength, double Composite, double Adjusted, double TriLow, double TriMode, double TriHigh);
    public sealed record ForecastRow(int T, double Revenue, double Growth, double Margin, double Ebit, double TaxYear, double Nopat);
    public sealed record ForecastResult(List<ForecastRow> Rows);
    public sealed record RfrRow(int T, double Revenue, double Rate, double Saving, double AfterTax, double Disc, double Pv);
    public sealed record RfrResult(List<RfrRow> Rows, double PvExplicit, double Tv, double Value);
    public sealed record PremiumProfitResult(double PricePremiumPct, double AnnualPremiumProfit, double Value);
    public sealed record IcfRow(int T, double WithBrand, double WithoutBrand, double Incremental, double Disc, double Pv);
    public sealed record IcfResult(List<IcfRow> Rows, double Value);
    public sealed record ScenRow(string Name, double Prob, double Mult, string? Assump, double Value);
    public sealed record ScenResult(List<ScenRow> Rows, double ExpectedValue, double ProbCheck);
    public sealed record McResult(double[] Values, double Mean, double Median, double P10, double P90, double Min, double Max, double Stdev, int[] Hist, string[] HistLabels);
    public sealed record TornadoRow(string Name, double Low, double High, double ImpactLow, double ImpactHigh, double Range);
    public sealed record FinalItem(string Name, double Value, double Weight);
    public sealed record FinalResult(List<FinalItem> Items, double WeightSum, double Hybrid);
    public sealed record BrandResult(
        StrengthResult Scorecard, StrengthResult Legal, RoyaltyResult Royalty, ForecastResult Forecast,
        RfrResult Rfr, PremiumProfitResult Pp, IcfResult Icf, ScenResult Scen, McResult Mc,
        List<TornadoRow> Tornado, FinalResult Final);
}
