using System.Text.Json;
using Application.Tools.Finance;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Patent Valuation (general + pharma) — ported from
 * app/general-ip-valuation/logic.ts (pharma variant only differs in
 * default inputs, so a single engine serves both tool codes).
 * Scorecard, Cost, Royalty Benchmark, PCS, WACC, DCF, RFR,
 * Black-Scholes, Binomial Tree, Monte Carlo → fair value.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class PatentValuationRunner : IToolRunner
{
    private static readonly (string Name, double Low, double Typ, double High)[] RoyaltyTable =
    [
        ("نرم‌افزار/فناوری اطلاعات", 0.03, 0.05, 0.08),
        ("دارو/بیوتکنولوژی", 0.04, 0.07, 0.10),
        ("الکترونیک/نیمه‌هادی", 0.02, 0.04, 0.06),
        ("خودرو", 0.01, 0.025, 0.04),
        ("کالاهای صنعتی/تجهیزات", 0.02, 0.04, 0.06),
        ("عمومی صنعتی/فناوری (میانه همه صنایع)", 0.01, 0.045, 0.08),
    ];

    private const double LegacyRoyaltyBase = 0.04;
    private static readonly double[] PcsCurve = [0.05, 0.08, 0.12, 0.18, 0.28, 0.40, 0.55, 0.70, 0.85, 0.95];
    private const double DcfMargin = 0.25;
    private static readonly double[] DcfRevenue =
        [10000, 10000, 10000, 12000, 13000, 14000, 15000, 16000, 17000, 18000, 19000, 20000, 21000, 22000, 23000];
    private static readonly double[] DcfCapexWc =
        [1000, 700, 700, 700, 700, 700, 700, 700, 700, 700, 700, 700, 700, 700, 700];

    public string ToolCode => "PATENT-VAL";

    public ToolRunOutcome Run(JsonElement input)
    {
        var s = ToolInput.Bind<PatentInput>(input);

        var scorecard = ComputeScorecard(s);
        var cost = ComputeCost(s);
        var benchmark = ComputeBenchmark(s);
        var pcs = ComputePcs(s);
        var wacc = ComputeWacc(s);
        var dcf = ComputeDcf(s, wacc.Dynamic);
        var rfr = ComputeRfr(s, wacc.Dynamic, benchmark);
        var bs = ComputeBlackScholes(s);
        var binomial = ComputeBinomial(s);
        var mc = ComputeMonteCarlo(s, rfr.Base, benchmark, pcs, wacc.MarketOnly);

        var fairValue = (dcf.DcfValue + rfr.Base) / 2;

        var result = new PatentResult(scorecard, cost, benchmark, pcs, wacc, dcf, rfr, bs, binomial, mc, fairValue);
        return new ToolRunOutcome(result, null, s.Name);
    }

    private static ScorecardResult ComputeScorecard(PatentInput s)
    {
        var rows = new List<ScorecardRowResult>
        {
            new("قدرت ادعاها", 15, s.PatentStrength, 15 * s.PatentStrength),
            new("TRL", 10, s.Trl / 9.0 * 100, 10 * (s.Trl / 9.0 * 100)),
            new("Family Size", 8, Math.Min(s.FamilySize * 10, 100), 8 * Math.Min(s.FamilySize * 10, 100)),
            new("Forward Citation", 8, Math.Min(s.ForwardCitation * 2, 100), 8 * Math.Min(s.ForwardCitation * 2, 100)),
            new("بازارپذیری", 15, s.MarketAttractiveness, 15 * s.MarketAttractiveness),
            new("FTO", 10, s.FtoReadiness, 10 * s.FtoReadiness),
            new("Licensing Potential", 10, s.LicensingPotential, 10 * s.LicensingPotential),
            new("عمر باقی‌مانده", 8, Math.Min(s.RemainingLife / 20.0 * 100, 100), 8 * Math.Min(s.RemainingLife / 20.0 * 100, 100)),
            new("ریسک Design-around", 6, 100 - s.DesignAroundRisk, 6 * (100 - s.DesignAroundRisk)),
            new("ریسک دعوی", 5, 100 - s.LitigationRisk, 5 * (100 - s.LitigationRisk)),
        };
        var sumW = rows.Sum(r => r.W);
        var sumWs = rows.Sum(r => r.Ws);
        var quality = sumWs / sumW;
        var confidence = Math.Min(100, 0.35 * quality + 0.25 * Math.Min(s.RemainingLife / 20.0 * 100, 100) + 0.2 * s.FtoReadiness + 0.2 * (100 - s.LitigationRisk));
        return new ScorecardResult(rows, quality, confidence);
    }

    private static CostResult ComputeCost(PatentInput s)
    {
        var replacement = s.RdCost + s.ProtoCost + s.IpCost + s.EngCost;
        return new CostResult(replacement, replacement * (1 - s.Obsolescence));
    }

    private static BenchmarkResult ComputeBenchmark(PatentInput s)
    {
        var row = RoyaltyTable.FirstOrDefault(r => r.Name == s.Sector);
        if (row == default) row = RoyaltyTable[^1];
        var rule25 = 0.25 * DcfMargin;
        var within = LegacyRoyaltyBase >= row.Low && LegacyRoyaltyBase <= row.High;
        return new BenchmarkResult(row.Name, row.Low, row.Typ, row.High, rule25, within);
    }

    private static PcsResult ComputePcs(PatentInput s)
    {
        var trl = FinanceMath.Clamp(s.Trl, 0, 9);
        var lo = (int)Math.Floor(trl);
        var hi = Math.Min(9, lo + 1);
        var frac = trl - lo;
        var basePcs = PcsCurve[lo] + (PcsCurve[hi] - PcsCurve[lo]) * frac;
        return new PcsResult(basePcs, FinanceMath.Clamp(basePcs - s.PcsBand, 0, 1), FinanceMath.Clamp(basePcs + s.PcsBand, 0, 1));
    }

    private static WaccResult ComputeWacc(PatentInput s)
    {
        var specificRiskPremium = s.MaxSpecificTrl0 * (1 - s.Trl / 9.0);
        var legalRiskAdj = s.MaxLegalRisk * ((s.LitigationRisk + s.DesignAroundRisk) / 2.0 / 100);
        var dynamic = s.RiskFreeRate + s.Erp + s.SizePremium + s.IndustryPremium + specificRiskPremium + legalRiskAdj + s.IlliquidityPremium;
        var marketOnly = s.RiskFreeRate + s.Erp + s.SizePremium + s.IndustryPremium + legalRiskAdj + s.IlliquidityPremium;
        return new WaccResult(specificRiskPremium, legalRiskAdj, dynamic, marketOnly);
    }

    private static DcfResult ComputeDcf(PatentInput s, double wacc)
    {
        var rows = new List<DcfRow>();
        double npv = 0, lastFcf = 0;
        for (var y = 1; y <= 15; y++)
        {
            var rev = DcfRevenue[y - 1];
            var ebitda = rev * DcfMargin;
            var tax = ebitda * s.TaxRate;
            var capexWc = DcfCapexWc[y - 1];
            var fcf = ebitda - tax - capexWc;
            var df = 1 / Math.Pow(1 + wacc, y);
            var pv = fcf * df;
            npv += pv;
            lastFcf = fcf;
            rows.Add(new DcfRow(y, rev, fcf, pv));
        }
        var tv = lastFcf * (1 + s.GrowthRate) / (wacc - s.GrowthRate);
        var pvTv = tv / Math.Pow(1 + wacc, 15);
        return new DcfResult(rows, npv, pvTv, npv + pvTv);
    }

    private static RfrResult ComputeRfr(PatentInput s, double wacc, BenchmarkResult benchmark)
    {
        var rows = new List<RfrRow>();
        double basePv = 0, low = 0, high = 0;
        for (var y = 1; y <= 15; y++)
        {
            var rev = DcfRevenue[y - 1];
            var df = 1 / Math.Pow(1 + wacc, y);
            var pvTyp = rev * benchmark.Typ * (1 - s.TaxRate) * df;
            basePv += pvTyp;
            low += rev * benchmark.Low * df;
            high += rev * benchmark.High * df;
            rows.Add(new RfrRow(y, benchmark.Typ, pvTyp));
        }
        low *= 1 - s.TaxRate;
        high *= 1 - s.TaxRate;
        return new RfrResult(rows, basePv, low, high);
    }

    private static BsResult ComputeBlackScholes(PatentInput s)
    {
        double S0 = s.OptionS, k = s.OptionK, sig = s.Volatility, r = s.RiskFreeRate, t = s.OptionHorizon;
        var d1 = (Math.Log(S0 / k) + (r + sig * sig / 2) * t) / (sig * Math.Sqrt(t));
        var d2 = d1 - sig * Math.Sqrt(t);
        var nd1 = FinanceMath.NormSDist(d1);
        var nd2 = FinanceMath.NormSDist(d2);
        return new BsResult(d1, d2, nd1, nd2, S0 * nd1 - k * Math.Exp(-r * t) * nd2);
    }

    private static BinomialResult ComputeBinomial(PatentInput s)
    {
        double S0 = s.OptionS, k = s.OptionK, sig = s.Volatility, r = s.RiskFreeRate, t = s.OptionHorizon;
        double q = s.Obsolescence, n = s.BinomialSteps;
        var dt = t / n;
        var u = Math.Exp(sig * Math.Sqrt(dt));
        var d = 1 / u;
        var p = (Math.Exp((r - q) * dt) - d) / (u - d);
        var disc = Math.Exp(-r * dt);

        var values = new double[(int)(n + 1)];
        for (var j = 0; j <= n; j++)
        {
            var price = S0 * Math.Pow(u, j) * Math.Pow(d, n - j);
            values[j] = Math.Max(price - k, 0);
        }
        for (var step = n - 1; step >= 0; step--)
        {
            var next = values;
            values = new double[(int)step + 1];
            for (var j = 0; j <= step; j++)
            {
                var price = S0 * Math.Pow(u, j) * Math.Pow(d, step - j);
                var cont = disc * (p * next[j + 1] + (1 - p) * next[j]);
                values[j] = Math.Max(price - k, cont);
            }
        }
        return new BinomialResult(u, d, p, disc, values[0]);
    }

    private static McResult ComputeMonteCarlo(PatentInput s, double rfrBase, BenchmarkResult benchmark, PcsResult pcs, double waccMarketOnly)
    {
        const int n = 5000;
        var vals = new double[n];
        var royaltyTyp = benchmark.Typ;
        for (var i = 0; i < n; i++)
        {
            var revMult = Math.Max(0, FinanceMath.RandNormal(1, 0.2));
            var royalty = benchmark.Low + (benchmark.High - benchmark.Low) * Random.Shared.NextDouble();
            var pcsSample = pcs.Low + (pcs.High - pcs.Low) * Random.Shared.NextDouble();
            var waccSample = Math.Max(0.05, waccMarketOnly + FinanceMath.RandNormal(0, 0.02));
            var obsSample = FinanceMath.Clamp(s.Obsolescence + FinanceMath.RandNormal(0, 0.03), 0, 0.6);
            vals[i] = rfrBase * (royalty / royaltyTyp) * pcsSample * revMult * (1 - obsSample) / (1 - s.Obsolescence)
                      * Math.Pow((1 + waccMarketOnly) / (1 + waccSample), s.RemainingLife);
        }
        Array.Sort(vals);
        double Pctl(double p) => vals[(int)Math.Floor(p * (n - 1))];
        return new McResult(Pctl(0.10), Pctl(0.50), Pctl(0.90), vals.Average());
    }

    // ── Input model ──
    public sealed class PatentInput
    {
        public string? Name { get; set; }
        public double Trl { get; set; }
        public double RemainingLife { get; set; }
        public double FamilySize { get; set; }
        public double ForwardCitation { get; set; }
        public string? Sector { get; set; }
        public double PatentStrength { get; set; }
        public double MarketAttractiveness { get; set; }
        public double FtoReadiness { get; set; }
        public double LicensingPotential { get; set; }
        public double DesignAroundRisk { get; set; }
        public double LitigationRisk { get; set; }
        public double TaxRate { get; set; }
        public double GrowthRate { get; set; }
        public double RdCost { get; set; }
        public double ProtoCost { get; set; }
        public double IpCost { get; set; }
        public double EngCost { get; set; }
        public double Obsolescence { get; set; }
        public double OptionS { get; set; }
        public double OptionK { get; set; }
        public double Volatility { get; set; }
        public double RiskFreeRate { get; set; }
        public double OptionHorizon { get; set; }
        public double BinomialSteps { get; set; } = 5;
        public double Erp { get; set; }
        public double SizePremium { get; set; }
        public double IndustryPremium { get; set; }
        public double IlliquidityPremium { get; set; }
        public double MaxSpecificTrl0 { get; set; }
        public double MaxLegalRisk { get; set; }
        public double PcsBand { get; set; }
    }

    // ── Result model ──
    public sealed record ScorecardRowResult(string Name, double W, double Score, double Ws);
    public sealed record ScorecardResult(List<ScorecardRowResult> Rows, double PatentQualityScore, double Confidence);
    public sealed record CostResult(double Replacement, double Adjusted);
    public sealed record BenchmarkResult(string Name, double Low, double Typ, double High, double Rule25, bool Within);
    public sealed record PcsResult(double Base, double Low, double High);
    public sealed record WaccResult(double SpecificRiskPremium, double LegalRiskAdj, double Dynamic, double MarketOnly);
    public sealed record DcfRow(int Y, double Rev, double Fcf, double Pv);
    public sealed record DcfResult(List<DcfRow> Rows, double Npv, double PvTerminal, double DcfValue);
    public sealed record RfrRow(int Y, double RoyaltyTyp, double Pv);
    public sealed record RfrResult(List<RfrRow> Rows, double Base, double Low, double High);
    public sealed record BsResult(double D1, double D2, double Nd1, double Nd2, double Value);
    public sealed record BinomialResult(double U, double D, double P, double Disc, double Value);
    public sealed record McResult(double P10, double P50, double P90, double Mean);
    public sealed record PatentResult(
        ScorecardResult Scorecard, CostResult Cost, BenchmarkResult Benchmark, PcsResult Pcs, WaccResult Wacc,
        DcfResult Dcf, RfrResult Rfr, BsResult Bs, BinomialResult Binomial, McResult Mc, double FairValue);
}
