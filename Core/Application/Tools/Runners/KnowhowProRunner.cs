using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Tools.Options;
using Common.Exceptions;
using Microsoft.Extensions.Options;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Know-how valuation pro — ported from app/knowhow-valuation-pro/logic.ts:
 *   cost      = Σ obsolescence factors × dev/repro costs (higher of the two)
 *   dcf       = 6 built years, 5 explicit + a 3-year tail grown off year 6
 *   royalty   = 5 years of after-tax royalty, discounted
 *   excess    = risk-adjusted DCF − contributory-asset charges (floored at 0)
 *   market    = avg(value/revenue) multiple × own revenue — unguarded, so a zero
 *               comparable revenue propagates the source's documented
 *               NaN/Infinity quirk verbatim (Math.Max keeps NaN/Infinity).
 *   weighted  = cost·wCost + dcf·wDCF + royalty·wRoyalty + excess·wExcess + market·wMarket
 *   adjustedFair = max(0, weighted − Σ overlap adjustments)
 * Monte Carlo is computed only when the input asks for it (`monteCarlo: true`)
 * — it is the client's button-triggered run. The key is named "scenarioTable"
 * (client: "scenarios") so the AdvancedSections strip list never removes it.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class KnowhowProRunner(IOptions<KnowhowOptions> options) : IToolRunner
{
    public string ToolCode => "KNOWHOW-PRO";

    public ToolRunOutcome Run(JsonElement input)
    {
        var s = ToolInput.Bind<KnowhowInput>(input);
        var o = options.Value;

        // ── 1. Cost approach ─────────────────────────────────────────────────
        var factor = (1 - s.PhysObsol) * (1 - s.TechObsol) * (1 - s.EconObsol);
        var reproduction = s.HistDevCost * factor;
        var replacement = s.ReplEngCost * factor;
        var costValue = Math.Max(reproduction, replacement);
        var cost = new CostResultK(reproduction, replacement, costValue, factor);

        // ── 2. Incremental DCF ───────────────────────────────────────────────
        DcfResultK Dcf(double growth, double discount) =>
            ComputeDcf(s, growth, discount, o);
        var dcf = Dcf(s.GrowthLikely, s.DiscLikely);

        // ── 3. Relief from royalty ───────────────────────────────────────────
        var royalty = ComputeRoyalty(s, s.GrowthLikely, s.RoyLikely, s.DiscLikely, o.RoyaltyYears);

        // ── 4. Excess earnings ───────────────────────────────────────────────
        var excess = new ExcessResultK(Math.Max(0, dcf.RiskAdjusted - s.TangibleCharge - s.WcCharge - s.OtherIntangCharge));

        // ── 5. Market approach (documented unguarded division) ───────────────
        var m1 = s.Comp1Value / s.Comp1Revenue;
        var m2 = s.Comp2Value / s.Comp2Revenue;
        var avg = (m1 + m2) / 2;
        var market = new MarketResultK(m1, m2, avg, avg * s.AnnualRevenue);

        // ── Reconciliation ───────────────────────────────────────────────────
        var weightedValue =
            costValue * s.WCost +
            dcf.RiskAdjusted * s.WDcf +
            royalty.RiskAdjusted * s.WRoyalty +
            excess.Value * s.WExcess +
            market.Value * s.WMarket;

        var overlapItems = new (string Key, double Value, double Overlap)[]
        {
            ("brand", s.BrandValue, s.BrandOverlap),
            ("custRel", s.CustRelValue, s.CustRelOverlap),
            ("software", s.SoftwareValue, s.SoftwareOverlap),
            ("contracts", s.ContractsValue, s.ContractsOverlap),
        };
        double overlapTotal = 0;
        var overlapRows = overlapItems.Select(it =>
        {
            var adj = it.Value * it.Overlap;
            overlapTotal += adj;
            return new OverlapRowK(it.Key, it.Value, it.Overlap, adj);
        }).ToArray();
        var doubleCount = new DoubleCountK(overlapRows, overlapTotal);

        var adjustedFair = Math.Max(0, weightedValue - doubleCount.Total);
        var revalIncrease = Math.Max(0, adjustedFair - s.CurrentBookValue);
        var weightSum = s.WCost + s.WDcf + s.WRoyalty + s.WExcess + s.WMarket;
        var techScore = (s.Scalability + s.Transferability + s.DocCompleteness
                         + (5 - s.KeyPersonDependency) + (5 - s.ObsolescenceRisk)) / 5;

        var critical = new CriticalConditionK[]
        {
            new("docs", s.CompanyName is { Length: > 0 }),
            new("ownership", s.TransferredToCompany),
            new("weight", Math.Abs(weightSum - 1) < 0.001),
        };

        // ── Scenarios (pessimistic / likely / optimistic) ────────────────────
        var scenarioTable = new[]
        {
            ("pess", s.GrowthPess, s.DiscPess, s.RoyPess),
            ("likely", s.GrowthLikely, s.DiscLikely, s.RoyLikely),
            ("opt", s.GrowthOpt, s.DiscOpt, s.RoyOpt),
        }.Select(sc =>
        {
            var d = Dcf(sc.Item2, sc.Item3);
            var r = ComputeRoyalty(s, sc.Item2, sc.Item4, sc.Item3, o.RoyaltyYears);
            return new ScenarioK(sc.Item1, sc.Item2, sc.Item3, sc.Item4,
                d.RiskAdjusted, r.RiskAdjusted,
                o.ScenarioDcfWeight * d.RiskAdjusted + (1 - o.ScenarioDcfWeight) * r.RiskAdjusted);
        }).ToArray();

        // ── Sensitivity (discount and growth, ±deltas around the likely case) ─
        var sensDiscount = o.SensDeltas.Select(d =>
        {
            var r = Dcf(s.GrowthLikely, s.DiscLikely + d);
            return new SensPointK(s.DiscLikely + d, r.RiskAdjusted);
        }).ToArray();
        var sensGrowth = o.SensDeltas.Select(d =>
        {
            var r = Dcf(s.GrowthLikely + d, s.DiscLikely);
            return new SensPointK(s.GrowthLikely + d, r.RiskAdjusted);
        }).ToArray();

        // ── Monte Carlo (button-triggered only) ──────────────────────────────
        MonteCarloK? mc = s.MonteCarlo ? RunMonteCarlo(s, o) : null;

        var result = new KnowhowResultK(cost, dcf, royalty, excess, market,
            weightedValue, doubleCount, adjustedFair, revalIncrease, weightSum, techScore,
            critical, scenarioTable, sensDiscount, sensGrowth, mc);

        // The documented NaN/Infinity quirk must not poison the summary column
        // (and cannot be JSON-serialised there) — show it as "no score".
        return new ToolRunOutcome(result, double.IsFinite(adjustedFair) ? adjustedFair : null, s.Name);
    }

    private static DcfResultK ComputeDcf(KnowhowInput s, double growth, double discount, KnowhowOptions o)
    {
        var years = new List<DcfYearK>();
        var revenue = s.AnnualRevenue;

        for (var t = 1; t <= o.DcfYears; t++)
        {
            revenue *= 1 + growth;
            var ebitda = revenue * s.EbitdaMargin;
            var tax = ebitda * s.TaxRate;
            years.Add(new DcfYearK(t, revenue, ebitda, tax,
                ebitda - tax - s.Capex - s.Wc - s.OtherCash));
        }

        double pv = 0;
        for (var t = 1; t <= o.DcfPvYears; t++)
            pv += years[t - 1].Fcf / Math.Pow(1 + discount, t);

        var fcf6 = years[o.DcfYears - 1].Fcf;
        double terminal = 0;
        for (var j = 0; j < o.DcfTailYears; j++)
            terminal += fcf6 * Math.Pow(1 + growth, j) / Math.Pow(1 + discount, o.DcfYears + j);

        pv += terminal;
        return new DcfResultK(years.ToArray(), pv, pv * s.SuccessProb, terminal);
    }

    private static RoyaltyResultK ComputeRoyalty(
        KnowhowInput s, double growth, double royaltyRate, double discount, int years)
    {
        var rows = new List<RoyaltyYearK>();
        var revenue = s.AnnualRevenue;
        double pv = 0;

        for (var t = 1; t <= years; t++)
        {
            revenue *= 1 + growth;
            var gross = revenue * royaltyRate;
            var tax = gross * s.TaxRate;
            var afterTax = gross - tax;
            var df = 1 / Math.Pow(1 + discount, t);
            var pvT = afterTax * df;
            pv += pvT;
            rows.Add(new RoyaltyYearK(t, revenue, gross, tax, afterTax, df, pvT));
        }

        return new RoyaltyResultK(rows.ToArray(), pv, pv * s.SuccessProb);
    }

    private static MonteCarloK RunMonteCarlo(KnowhowInput s, KnowhowOptions o)
    {
        static double Triangular(double min, double mode, double max, double u)
        {
            if (max <= min) return min;
            var split = (mode - min) / (max - min);
            return u < split
                ? min + Math.Sqrt(u * (max - min) * (mode - min))
                : max - Math.Sqrt((1 - u) * (max - min) * (max - mode));
        }

        var n = o.MonteCarloRuns;
        var combos = new double[n];
        for (var i = 0; i < n; i++)
        {
            var g = Triangular(s.GrowthPess, s.GrowthLikely, s.GrowthOpt, Random.Shared.NextDouble());
            var r = Triangular(s.DiscOpt, s.DiscLikely, s.DiscPess, Random.Shared.NextDouble());
            var roy = Triangular(s.RoyPess, s.RoyLikely, s.RoyOpt, Random.Shared.NextDouble());

            var dcf = ComputeDcf(s, g, r, o).RiskAdjusted;
            var royC = ComputeRoyalty(s, g, roy, r, o.RoyaltyYears).RiskAdjusted;
            combos[i] = o.ScenarioDcfWeight * dcf + (1 - o.ScenarioDcfWeight) * royC;
        }

        Array.Sort(combos);
        double Pct(double p) => combos[Math.Min(combos.Length - 1, (int)Math.Floor(p * combos.Length))];
        var mean = combos.Sum() / n;
        var variance = combos.Sum(b => (b - mean) * (b - mean)) / n;
        return new MonteCarloK(combos, Pct(0.1), Pct(0.25), Pct(0.5), Pct(0.75), Pct(0.9),
            mean, Math.Sqrt(variance));
    }

    /* ── I/O shapes ───────────────────────────────────────────────────────── */

    public sealed class KnowhowInput
    {
        public string? Name { get; set; }
        public string CompanyName { get; set; } = "";
        public string TechName { get; set; } = "";
        public string CompletedBy { get; set; } = "";
        public string ValuationDate { get; set; } = "";
        public string InitialBuyer { get; set; } = "";
        public double InitialPurchaseYear { get; set; }
        public double InitialPurchasePrice { get; set; }
        public bool TransferredToCompany { get; set; }
        public double TransferYear { get; set; }
        public double TransferConsideration { get; set; }
        public bool CurrentlyRecognized { get; set; }
        public double CurrentBookValue { get; set; }

        public double Trl { get; set; }
        public double RemainingLife { get; set; }
        public double AnnualRevenue { get; set; }
        public double EbitdaMargin { get; set; }
        public double TaxRate { get; set; }
        public double SuccessProb { get; set; }
        public double Capex { get; set; }
        public double Wc { get; set; }
        public double OtherCash { get; set; }

        public double GrowthPess { get; set; }
        public double GrowthLikely { get; set; }
        public double GrowthOpt { get; set; }
        public double DiscPess { get; set; }
        public double DiscLikely { get; set; }
        public double DiscOpt { get; set; }
        public double RoyPess { get; set; }
        public double RoyLikely { get; set; }
        public double RoyOpt { get; set; }

        public double Scalability { get; set; }
        public double Transferability { get; set; }
        public double DocCompleteness { get; set; }
        public double KeyPersonDependency { get; set; }
        public double ObsolescenceRisk { get; set; }
        public double ProductsUsingTech { get; set; }
        public double MajorCustomers { get; set; }
        public double ExportPotential { get; set; }
        public double CompetitiveAdvantage { get; set; }
        public double LicensingPotential { get; set; }

        public double HistDevCost { get; set; }
        public double ReplEngCost { get; set; }
        public double PhysObsol { get; set; }
        public double TechObsol { get; set; }
        public double EconObsol { get; set; }

        public string Comp1Name { get; set; } = "";
        public double Comp1Value { get; set; }
        public double Comp1Revenue { get; set; }
        public string Comp2Name { get; set; } = "";
        public double Comp2Value { get; set; }
        public double Comp2Revenue { get; set; }

        public double TangibleCharge { get; set; }
        public double WcCharge { get; set; }
        public double OtherIntangCharge { get; set; }

        public double BrandValue { get; set; }
        public double BrandOverlap { get; set; }
        public double CustRelValue { get; set; }
        public double CustRelOverlap { get; set; }
        public double SoftwareValue { get; set; }
        public double SoftwareOverlap { get; set; }
        public double ContractsValue { get; set; }
        public double ContractsOverlap { get; set; }

        public double WCost { get; set; }
        [JsonPropertyName("wDCF")] public double WDcf { get; set; }
        public double WRoyalty { get; set; }
        public double WExcess { get; set; }
        public double WMarket { get; set; }

        public Dictionary<string, string>? DdStatus { get; set; }
        public Dictionary<string, string>? ChecklistStatus { get; set; }

        /** Client-side flag: run the Monte Carlo (the Analysis tab's button). */
        public bool MonteCarlo { get; set; }
    }

    public sealed record CostResultK(double Reproduction, double Replacement, double Value, double Factor);

    public sealed record DcfYearK(int T, double Revenue, double Ebitda, double Tax, double Fcf);

    public sealed record DcfResultK(DcfYearK[] Years, double Pv, double RiskAdjusted, double Terminal);

    public sealed record RoyaltyYearK(int T, double Revenue, double Gross, double Tax, double AfterTax, double Df, double Pv);

    public sealed record RoyaltyResultK(RoyaltyYearK[] Years, double Value, double RiskAdjusted);

    public sealed record ExcessResultK(double Value);

    public sealed record MarketResultK(double M1, double M2, double Avg, double Value);

    public sealed record OverlapRowK(string Key, double Value, double Overlap, double Adj);

    public sealed record DoubleCountK(OverlapRowK[] Rows, double Total);

    public sealed record CriticalConditionK(string Key, bool Ok);

    public sealed record ScenarioK(
        string Key, double Growth, double Discount, double Royalty,
        double Dcf, double RoyaltyVal, double Combo);

    public sealed record SensPointK(double X, double Value);

    public sealed record MonteCarloK(
        double[] Combos, double P10, double P25, double P50, double P75, double P90,
        double Mean, double Std);

    public sealed record KnowhowResultK(
        CostResultK Cost,
        DcfResultK Dcf,
        RoyaltyResultK Royalty,
        ExcessResultK Excess,
        MarketResultK Market,
        double WeightedValue,
        DoubleCountK DoubleCount,
        double AdjustedFair,
        double RevalIncrease,
        double WeightSum,
        double TechScore,
        CriticalConditionK[] Critical,
        [property: JsonPropertyName("scenarioTable")] ScenarioK[] ScenarioTable,
        SensPointK[] SensDiscount,
        SensPointK[] SensGrowth,
        MonteCarloK? MonteCarlo);
}
