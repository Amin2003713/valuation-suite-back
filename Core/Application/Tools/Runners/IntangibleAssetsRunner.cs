using System.Text.Json;
using Application.Tools.Finance;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Intangible Assets — ported from app/intangible-assets/logic.ts.
 * Modules: R&D capitalization, Human Capital (4 methods blended),
 * Data Asset (cost/income/market/RFR blended), Data Quality index,
 * Risk discount, Monte Carlo.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class IntangibleAssetsRunner : IToolRunner
{
    public string ToolCode => "INTANGIBLE";

    public ToolRunOutcome Run(JsonElement input)
    {
        var s = ToolInput.Bind<IntangibleInput>(input);

        var rd = ComputeRd(s.Rd);
        var hc = ComputeHc(s.Hc);
        var quality = ComputeQuality(s.Quality);
        var risk = ComputeRisk(s.Risk);
        var data = ComputeData(s, quality.Factor, risk.Discount);
        var mc = ComputeMonteCarlo(s.Mc, s.Data.Income.Benefit, quality.Factor);
        var total = rd.Value + hc.Blended + data.Value;

        var result = new IntangibleResult(rd, hc, quality, risk, data, mc, total);
        return new ToolRunOutcome(result, null, s.Name);
    }

    private static RdResult ComputeRd(RdInput c)
    {
        var capBase = c.Spend * c.CapRate;
        var combinedProb = c.PTech * c.PComm;
        var value = capBase * combinedProb;
        var annualAmort = value / c.Life;
        var rows = new List<RdYearRow>();
        double opening = value;
        for (var y = 1; y <= (int)c.Life; y++)
        {
            var closing = opening - annualAmort;
            rows.Add(new RdYearRow(y, opening, annualAmort, closing));
            opening = closing;
        }
        return new RdResult(capBase, combinedProb, value, annualAmort, rows);
    }

    private static HcResult ComputeHc(HcInput c)
    {
        var totalPayroll = c.NumEmp * c.AvgComp;
        var ls = FinanceMath.GrowingAnnuityPV(totalPayroll, c.Growth, c.DiscRate, c.RemainYears);
        var rc = c.NumEmp * (c.RecruitCost + c.TrainCost + c.ProdLossCost);
        var oc = c.AnnualProfit * (c.MonthsToReplace / 12.0);
        var effRatio = c.FirmRoa / c.IndRoa;
        var herm = FinanceMath.OrdinaryAnnuityPV(totalPayroll * effRatio, c.DiscRate, c.RemainYears);
        var hcRoi = (c.Revenue - (c.Opex - totalPayroll)) / totalPayroll;
        var values = new[] { ls, rc, oc, herm };
        var wsum = c.Weights.Sum();
        var blended = values.Select((v, i) => v * c.Weights[i]).Sum() / wsum;
        return new HcResult(totalPayroll, ls, rc, oc, effRatio, herm, hcRoi, wsum, blended);
    }

    private static QualityResult ComputeQuality(QualityInput q)
    {
        var rows = q.Criteria;
        var wsum = rows.Sum(r => r.Weight);
        var index100 = rows.Sum(r => r.Weight * r.Score) * 10;
        return new QualityResult(rows, wsum, index100, index100 / 100);
    }

    private static RiskResult ComputeRisk(RiskInput r)
    {
        var wsum = r.Items.Sum(x => x.Weight);
        var score100 = r.Items.Sum(x => x.Weight * x.Score) * 10;
        var discount = score100 / 100 * r.MaxDiscount;
        return new RiskResult(r.Items, wsum, score100, discount);
    }

    private static DataResult ComputeData(IntangibleInput s, double qualityFactor, double riskDiscount)
    {
        var c = s.Data;
        var costRaw = c.Cost.Acquisition + c.Cost.Cleaning + c.Cost.InfraAnnual * c.Cost.YearsAccum;
        var costAdj = costRaw * qualityFactor;
        var incomeRaw = FinanceMath.GrowingAnnuityPV(c.Income.Benefit, c.Income.Growth, s.DiscountRate, c.Income.Life);
        var incomeAdj = incomeRaw * qualityFactor;
        var marketRaw = c.Market.RecordsMillions * c.Market.PricePerRecord;
        var marketAdj = marketRaw * qualityFactor;
        var rfrAnnuity = FinanceMath.GrowingAnnuityPV(c.Rfr.LicensingRevenue, c.Rfr.Growth, s.DiscountRate, c.Rfr.Years);
        var rfrRaw = rfrAnnuity * c.Rfr.RoyaltyRate * (1 - s.TaxRate);
        var rfrAdj = rfrRaw * qualityFactor;
        var values = new[] { costAdj, incomeAdj, marketAdj, rfrAdj };
        var wsum = c.Weights.Sum();
        var preRisk = values.Select((v, i) => v * c.Weights[i]).Sum() / wsum;
        var value = preRisk * (1 - riskDiscount);
        return new DataResult(costRaw, costAdj, incomeRaw, incomeAdj, marketRaw, marketAdj, rfrAnnuity, rfrRaw, rfrAdj, wsum, preRisk, value);
    }

    private static McResult ComputeMonteCarlo(McInput m, double baseBenefit, double qualityFactorBase)
    {
        var values = new double[m.N];
        for (var i = 0; i < m.N; i++)
        {
            var g = FinanceMath.Triangular(m.GrowthLow, m.GrowthMode, m.GrowthHigh);
            var life = FinanceMath.Triangular(m.LifeLow, m.LifeMode, m.LifeHigh);
            var d = FinanceMath.Triangular(m.DiscLow, m.DiscMode, m.DiscHigh);
            var q = Math.Min(1, FinanceMath.Triangular(m.QualLow, qualityFactorBase, m.QualHigh));
            var bm = FinanceMath.Triangular(m.BmLow, m.BmMode, m.BmHigh);
            double v;
            try
            {
                v = baseBenefit * bm * (1 + g) / (d - g) * (1 - Math.Pow((1 + g) / (1 + d), life)) * q;
            }
            catch
            {
                v = 0;
            }
            values[i] = double.IsFinite(v) ? v : 0;
        }
        var (mean, p10, p50, p90, min, max, stdev, hist, labels) = FinanceMath.Summarize(values, bins: 10);
        return new McResult(values, mean, p50, p10, p90, min, max, stdev, hist, labels);
    }

    // ── Input model ──
    public sealed class IntangibleInput
    {
        public string? Name { get; set; }
        public string? Currency { get; set; }
        public double DiscountRate { get; set; }
        public double LtGrowth { get; set; }
        public double TaxRate { get; set; }
        public RdInput Rd { get; set; } = new();
        public HcInput Hc { get; set; } = new();
        public QualityInput Quality { get; set; } = new();
        public RiskInput Risk { get; set; } = new();
        public DataInput Data { get; set; } = new();
        public McInput Mc { get; set; } = new();
    }

    public sealed class RdInput { public double Spend { get; set; } public double CapRate { get; set; } public double PTech { get; set; } public double PComm { get; set; } public double Life { get; set; } }
    public sealed class HcInput
    {
        public double NumEmp { get; set; } public double AvgComp { get; set; } public double RemainYears { get; set; }
        public double Growth { get; set; } public double DiscRate { get; set; }
        public double RecruitCost { get; set; } public double TrainCost { get; set; } public double ProdLossCost { get; set; }
        public double AnnualProfit { get; set; } public double MonthsToReplace { get; set; }
        public double FirmRoa { get; set; } public double IndRoa { get; set; }
        public double Revenue { get; set; } public double Opex { get; set; }
        public List<double> Weights { get; set; } = [];
    }
    public sealed class QualityInput { public List<WeightedRow> Criteria { get; set; } = []; }
    public sealed class RiskInput { public List<WeightedRow> Items { get; set; } = []; public double MaxDiscount { get; set; } }
    public sealed class DataInput
    {
        public DataCost Cost { get; set; } = new();
        public DataIncome Income { get; set; } = new();
        public DataMarket Market { get; set; } = new();
        public DataRfr Rfr { get; set; } = new();
        public List<double> Weights { get; set; } = [];
    }
    public sealed class DataCost { public double Acquisition { get; set; } public double Cleaning { get; set; } public double InfraAnnual { get; set; } public double YearsAccum { get; set; } }
    public sealed class DataIncome { public double Benefit { get; set; } public double Growth { get; set; } public double Life { get; set; } }
    public sealed class DataMarket { public double RecordsMillions { get; set; } public double PricePerRecord { get; set; } }
    public sealed class DataRfr { public double LicensingRevenue { get; set; } public double RoyaltyRate { get; set; } public double Growth { get; set; } public double Years { get; set; } }
    public sealed class McInput
    {
        public int N { get; set; } = 1000;
        public double GrowthLow { get; set; } public double GrowthMode { get; set; } public double GrowthHigh { get; set; }
        public double LifeLow { get; set; } public double LifeMode { get; set; } public double LifeHigh { get; set; }
        public double DiscLow { get; set; } public double DiscMode { get; set; } public double DiscHigh { get; set; }
        public double QualLow { get; set; } public double QualHigh { get; set; }
        public double BmLow { get; set; } public double BmMode { get; set; } public double BmHigh { get; set; }
    }

    // ── Result model ──
    public sealed record RdYearRow(int Year, double Opening, double Amort, double Closing);
    public sealed record RdResult(double CapBase, double CombinedProb, double Value, double AnnualAmort, List<RdYearRow> Rows);
    public sealed record HcResult(double TotalPayroll, double Ls, double Rc, double Oc, double EffRatio, double Herm, double HcRoi, double Wsum, double Blended);
    public sealed record QualityResult(List<WeightedRow> Rows, double Wsum, double Index100, double Factor);
    public sealed record RiskResult(List<WeightedRow> Rows, double Wsum, double Score100, double Discount);
    public sealed record DataResult(double CostRaw, double CostAdj, double IncomeRaw, double IncomeAdj, double MarketRaw, double MarketAdj, double RfrAnnuity, double RfrRaw, double RfrAdj, double Wsum, double PreRisk, double Value);
    public sealed record McResult(double[] Values, double Mean, double Median, double P10, double P90, double Min, double Max, double Stdev, int[] Hist, string[] HistLabels);
    public sealed record IntangibleResult(RdResult Rd, HcResult Hc, QualityResult Quality, RiskResult Risk, DataResult Data, McResult Mc, double Total);
}
