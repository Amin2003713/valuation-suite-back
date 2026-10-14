using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Tools.Options;
using Common.Exceptions;
using Microsoft.Extensions.Options;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Software valuation framework — ported from app/software-valuation-framework/logic.ts:
 *   WACC          = (1 − debtRatio)·costOfEquity + debtRatio·costOfDebt·(1 − tax)
 *   costOfEquity  = rf + β·marketPremium + sizePremium + specificPremium   (all %)
 *   specific      = base × max(1 + (3 − maturityAvg)·0.12, 0.4)
 *   DCF/royalty   = Σ_{t=1..life} rev(1+g)^t · driver · (1 − tax) / (1+wacc)^t, floored at 0
 *   life          = round(economicLife) || 5       (the source's `|| 5`)
 *   saasMultiple  = growth-banded rule of thumb (8/5/3/2)
 *   tornado       = ±20% per driver against DCF+royalty with the base WACC held
 *   Monte Carlo   = triangular sampling of growth/WACC/royalty trios, memoised
 *                   per (inputs, n) exactly like the client's MC_CACHE.
 * NOTE on keys: the client's "methods"/"tornado"/"mc" are emitted here as
 * "methodValues"/"tornadoSweep"/"monteCarlo" so AdvancedSections' strip list
 * never removes them — these tools are free-tier and must show everything.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class SoftwareValuationFrameworkRunner(IOptions<SvfOptions> options) : IToolRunner
{
    public string ToolCode => "SOFT-VAL";

    private readonly ConcurrentDictionary<string, SvfMonteCarlo> _mcCache = new();

    public ToolRunOutcome Run(JsonElement input)
    {
        var s = ToolInput.Bind<SvfInput>(input);
        var o = options.Value;

        if (s.Scenario is not ("p" or "l" or "o"))
            throw ValuationException.BadRequest("سناریوی درآمد نامعتبر است (p/l/o).");
        if (s.RoyScenario is not ("p" or "l" or "o"))
            throw ValuationException.BadRequest("سناریوی رویالتی نامعتبر است (p/l/o).");
        if (!o.StageTable.ContainsKey(s.Stage))
            throw ValuationException.BadRequest($"مرحلهٔ ناشناخته: {s.Stage}");
        if (!o.IndustryTable.ContainsKey(s.Industry))
            throw ValuationException.BadRequest($"صنعت ناشناخته: {s.Industry}");

        // ── Formula set (pure functions over the bound state) ────────────────
        double EffectiveSpecificPremium(SvfInput st)
        {
            var avg = (st.MatProduct + st.MatMarket + st.MatRevenue) / 3;
            var adj = 1 + (3 - avg) * o.MaturityTiltFactor;
            return st.SpecificPremium * Math.Max(adj, o.MaturityTiltFloor);
        }

        double CostOfEquity(SvfInput st) =>
            st.RiskFree / 100 + st.Beta * st.MarketPremium / 100 + st.SizePremium / 100
            + EffectiveSpecificPremium(st) / 100;

        double Wacc(SvfInput st)
        {
            var equity = CostOfEquity(st);
            var debtR = st.DebtRatio / 100;
            return (1 - debtR) * equity + debtR * st.CostOfDebt / 100 * (1 - st.TaxRate / 100);
        }

        double Dcf(SvfInput st, double? waccOverride)
        {
            var g = LegOf(st, "growth", st.Scenario) / 100;
            var margin = LegOf(st, "ebitda", st.Scenario) / 100;
            var capex = LegOf(st, "capex", st.Scenario) / 100;
            var dwc = LegOf(st, "dwc", st.Scenario) / 100;
            var wacc = waccOverride ?? Wacc(st);
            var life = LifeOf(st);
            var tax = st.TaxRate / 100;

            double v = 0;
            for (var t = 1.0; t <= life; t++)
            {
                var rev = st.Revenue * Math.Pow(1 + g, t);
                v += rev * (margin - capex - dwc) * (1 - tax) / Math.Pow(1 + wacc, t);
            }
            return Math.Max(v, 0);
        }

        double Royalty(SvfInput st, double? waccOverride)
        {
            var g = LegOf(st, "growth", st.Scenario) / 100;
            var roy = RoyLegOf(st, st.RoyScenario) / 100;
            var wacc = waccOverride ?? Wacc(st);
            var life = LifeOf(st);
            var tax = st.TaxRate / 100;

            double v = 0;
            for (var t = 1.0; t <= life; t++)
            {
                var rev = st.Revenue * Math.Pow(1 + g, t);
                v += rev * roy * (1 - tax) / Math.Pow(1 + wacc, t);
            }
            return Math.Max(v, 0);
        }

        static double Cost(SvfInput st) =>
            Math.Max(st.DevCost * (1 - st.TechDebtPct / 100) * st.RebuildMult, 0);

        double SaasMultiple(double g)
        {
            foreach (var (minG, multiple) in o.SaasBands)
            {
                if (g >= minG) return multiple;
            }
            return 2;
        }

        double Saas(SvfInput st) =>
            Math.Max(st.Revenue * SaasMultiple(LegOf(st, "growth", st.Scenario) / 100) * st.Nrr / 100, 0);

        static double Users(SvfInput st) => Math.Max(st.NumUsers * st.ValuePerUser / 1000, 0);

        static double Scorecard(SvfInput st)
        {
            var mult = st.Team / 3 * 0.4 + st.Market / 3 * 0.35 + st.Product / 3 * 0.25;
            return Math.Max(st.Benchmark * mult, 0);
        }

        // ── Snapshot ─────────────────────────────────────────────────────────
        var wacc = Wacc(s);
        var scenarioVals = new ScenarioVals(
            LegOf(s, "growth", s.Scenario), LegOf(s, "ebitda", s.Scenario),
            LegOf(s, "capex", s.Scenario), LegOf(s, "dwc", s.Scenario));

        var methodValues = new MethodValuesResult(
            Dcf(s, wacc), Royalty(s, wacc), Cost(s), Saas(s), Users(s), Scorecard(s));

        // ── Tornado (fixed base WACC, ±20% per driver, widest swing first) ───
        var baseSum = Dcf(s, wacc) + Royalty(s, wacc);
        var swing = o.TornadoSwing;

        var tornadoParams = new (string Key, string Label)[]
        {
            ("growth", "نرخ رشد"),
            ("ebitda", "حاشیه EBITDA"),
            ("roy", "نرخ رویالتی"),
            ("riskFree", "نرخ بدون ریسک"),
        };

        var tornado = tornadoParams.Select(p =>
        {
            SvfInput Mutate(double factor)
            {
                var next = s.Copy();
                switch (p.Key)
                {
                    case "growth": SetLeg(next, "growth", next.Scenario, LegOf(next, "growth", next.Scenario) * factor); break;
                    case "ebitda": SetLeg(next, "ebitda", next.Scenario, LegOf(next, "ebitda", next.Scenario) * factor); break;
                    case "roy": SetRoyLeg(next, next.RoyScenario, RoyLegOf(next, next.RoyScenario) * factor); break;
                    default: next.RiskFree *= factor; break;
                }
                return next;
            }

            var low = Dcf(Mutate(1 - swing), wacc) + Royalty(Mutate(1 - swing), wacc);
            var high = Dcf(Mutate(1 + swing), wacc) + Royalty(Mutate(1 + swing), wacc);
            return new TornadoRow(p.Label, Math.Min(low, high), Math.Max(low, high), baseSum);
        }).OrderByDescending(r => r.High - r.Low).ToArray();

        // ── Checks (informational, exactly the source's nine) ────────────────
        var growth = LegOf(s, "growth", s.Scenario);
        var ebitda = LegOf(s, "ebitda", s.Scenario);
        var checks = new CheckRow[]
        {
            new("درآمد سالانه بزرگ‌تر از صفر است", s.Revenue > 0),
            new("نرخ رشد (مبنا) در بازه ۰ تا ۳۰۰٪ است", growth >= 0 && growth <= 300),
            new("حاشیه EBITDA (مبنا) در بازه ۰ تا ۹۰٪ است", ebitda >= 0 && ebitda <= 90),
            new("نرخ تنزیل (WACC) بزرگ‌تر از صفر است", wacc > 0),
            new("نرخ مالیات در بازه ۰ تا ۶۰٪ است", s.TaxRate >= 0 && s.TaxRate <= 60),
            new("نسبت بدهی فنی در بازه ۰ تا ۹۰٪ است", s.TechDebtPct >= 0 && s.TechDebtPct <= 90),
            new("تعداد کاربران منفی نیست", s.NumUsers >= 0),
            new("امتیازهای Scorecard در بازه ۱ تا ۵ هستند", s.Team is >= 1 and <= 5 && s.Market is >= 1 and <= 5 && s.Product is >= 1 and <= 5),
            new("عمر اقتصادی بین ۱ تا ۱۵ سال است", s.EconomicLife >= 1 && s.EconomicLife <= 15),
        };

        // ── Monte Carlo (memoised per inputs, like the client's MC_CACHE) ────
        var mc = _mcCache.GetOrAdd(
            $"{o.MonteCarloRuns}:{McSignature(s)}",
            _ => RunMonteCarlo(s, o.MonteCarloRuns));

        var result = new SvfResult(
            wacc, CostOfEquity(s), EffectiveSpecificPremium(s), scenarioVals,
            RoyLegOf(s, s.RoyScenario), methodValues,
            Dcf(s, null), Royalty(s, null), Cost(s), Saas(s),
            SaasMultiple(LegOf(s, "growth", s.Scenario) / 100), Users(s), Scorecard(s),
            tornado, checks, mc);

        return new ToolRunOutcome(result, methodValues.Dcf, s.Name);
    }

    /* ── State readers (the source's num() is folded into binding; missing → 0) ── */

    private static double LegOf(SvfInput st, string baseKey, string scenario) => (baseKey, scenario) switch
    {
        ("growth", "p") => st.GrowthP, ("growth", "l") => st.GrowthL, ("growth", _) => st.GrowthO,
        ("ebitda", "p") => st.EbitdaP, ("ebitda", "l") => st.EbitdaL, ("ebitda", _) => st.EbitdaO,
        ("capex", "p") => st.CapexP, ("capex", "l") => st.CapexL, ("capex", _) => st.CapexO,
        (_, "p") => st.DwcP, (_, "l") => st.DwcL, _ => st.DwcO,
    };

    private static double RoyLegOf(SvfInput st, string scenario) =>
        scenario switch { "p" => st.RoyP, "l" => st.RoyL, _ => st.RoyO };

    private static void SetLeg(SvfInput st, string baseKey, string scenario, double value)
    {
        switch (baseKey)
        {
            case "growth": if (scenario == "p") st.GrowthP = value; else if (scenario == "l") st.GrowthL = value; else st.GrowthO = value; break;
            case "ebitda": if (scenario == "p") st.EbitdaP = value; else if (scenario == "l") st.EbitdaL = value; else st.EbitdaO = value; break;
            case "capex": if (scenario == "p") st.CapexP = value; else if (scenario == "l") st.CapexL = value; else st.CapexO = value; break;
            default: if (scenario == "p") st.DwcP = value; else if (scenario == "l") st.DwcL = value; else st.DwcO = value; break;
        }
    }

    private static void SetRoyLeg(SvfInput st, string scenario, double value)
    {
        if (scenario == "p") st.RoyP = value;
        else if (scenario == "l") st.RoyL = value;
        else st.RoyO = value;
    }

    /** JS Math.round then `|| 5` — 0 (or negative zero) falls back to 5. */
    private static double LifeOf(SvfInput st)
    {
        var life = Math.Floor(st.EconomicLife + 0.5);
        return life == 0 ? 5 : life;
    }

    private static string McSignature(SvfInput s) => string.Join("|",
        s.Revenue, s.Scenario, s.RoyScenario, s.TaxRate, s.EconomicLife,
        s.GrowthP, s.GrowthL, s.GrowthO, s.EbitdaP, s.EbitdaL, s.EbitdaO,
        s.CapexP, s.CapexL, s.CapexO, s.DwcP, s.DwcL, s.DwcO,
        s.WaccP, s.WaccL, s.WaccO, s.RoyP, s.RoyL, s.RoyO);

    private static SvfMonteCarlo RunMonteCarlo(SvfInput s, int n)
    {
        static double Triangular(double a, double m, double b)
        {
            if (b <= a) return m;
            var u = Random.Shared.NextDouble();
            var mm = Math.Min(Math.Max(m, a), b);
            var cutoff = (mm - a) / (b - a);
            return u < cutoff
                ? a + Math.Sqrt(u * (b - a) * (mm - a))
                : b - Math.Sqrt((1 - u) * (b - a) * (b - mm));
        }

        var margin = LegOf(s, "ebitda", s.Scenario) / 100;
        var capex = LegOf(s, "capex", s.Scenario) / 100;
        var dwc = LegOf(s, "dwc", s.Scenario) / 100;
        var tax = s.TaxRate / 100;
        var life = LifeOf(s);

        var gP = s.GrowthP / 100; var gL = s.GrowthL / 100; var gO = s.GrowthO / 100;
        var wP = s.WaccP / 100; var wL = s.WaccL / 100; var wO = s.WaccO / 100;
        var rP = s.RoyP / 100; var rL = s.RoyL / 100; var rO = s.RoyO / 100;

        var results = new double[n];
        for (var i = 0; i < n; i++)
        {
            var g = Triangular(gP, gL, gO);
            var w = Triangular(wO, wL, wP);
            var r = Triangular(rP, rL, rO);

            double dcf = 0, roy = 0;
            for (var t = 1.0; t <= life; t++)
            {
                var rev = s.Revenue * Math.Pow(1 + g, t);
                dcf += rev * (margin - capex - dwc) * (1 - tax) / Math.Pow(1 + w, t);
                roy += rev * r * (1 - tax) / Math.Pow(1 + w, t);
            }
            results[i] = Math.Max(dcf, 0) + Math.Max(roy, 0);
        }

        Array.Sort(results);
        double Pct(double p) => results[Math.Min((int)Math.Floor(p * n), n - 1)];
        return new SvfMonteCarlo(Pct(0.1), Pct(0.25), Pct(0.5), Pct(0.75), Pct(0.9), results[0], results[n - 1]);
    }

    /* ── I/O shapes ───────────────────────────────────────────────────────── */

    public sealed class SvfInput
    {
        public string? Name { get; set; }
        public string ProductType { get; set; } = "saas";
        public string Industry { get; set; } = "enterprise";
        public string Stage { get; set; } = "mvp";
        public double EconomicLife { get; set; }
        public double TaxRate { get; set; }
        public double RiskFree { get; set; }
        public double Beta { get; set; }
        public double MarketPremium { get; set; }
        public double DebtRatio { get; set; }
        public double CostOfDebt { get; set; }
        public double MatProduct { get; set; }
        public double MatMarket { get; set; }
        public double MatRevenue { get; set; }
        public double Revenue { get; set; }
        [JsonPropertyName("growth_p")] public double GrowthP { get; set; }
        [JsonPropertyName("growth_l")] public double GrowthL { get; set; }
        [JsonPropertyName("growth_o")] public double GrowthO { get; set; }
        [JsonPropertyName("ebitda_p")] public double EbitdaP { get; set; }
        [JsonPropertyName("ebitda_l")] public double EbitdaL { get; set; }
        [JsonPropertyName("ebitda_o")] public double EbitdaO { get; set; }
        [JsonPropertyName("capex_p")] public double CapexP { get; set; }
        [JsonPropertyName("capex_l")] public double CapexL { get; set; }
        [JsonPropertyName("capex_o")] public double CapexO { get; set; }
        [JsonPropertyName("dwc_p")] public double DwcP { get; set; }
        [JsonPropertyName("dwc_l")] public double DwcL { get; set; }
        [JsonPropertyName("dwc_o")] public double DwcO { get; set; }
        [JsonPropertyName("wacc_p")] public double WaccP { get; set; }
        [JsonPropertyName("wacc_l")] public double WaccL { get; set; }
        [JsonPropertyName("wacc_o")] public double WaccO { get; set; }
        public string Scenario { get; set; } = "l";
        public double DevCost { get; set; }
        [JsonPropertyName("roy_scenario")] public string RoyScenario { get; set; } = "l";
        public double NumUsers { get; set; }
        public double Team { get; set; }
        public double Market { get; set; }
        public double Product { get; set; }
        // cascade-derived fields the page sends along with the base inputs
        public double SpecificPremium { get; set; }
        public double TechDebtPct { get; set; }
        public double Nrr { get; set; }
        public double SizePremium { get; set; }
        [JsonPropertyName("roy_p")] public double RoyP { get; set; }
        [JsonPropertyName("roy_l")] public double RoyL { get; set; }
        [JsonPropertyName("roy_o")] public double RoyO { get; set; }
        public double RebuildMult { get; set; }
        public double ValuePerUser { get; set; }
        public double Benchmark { get; set; }

        public SvfInput Copy() => (SvfInput)MemberwiseClone();
    }

    public sealed record ScenarioVals(double Growth, double Ebitda, double Capex, double Dwc);

    public sealed record MethodValuesResult(
        double Dcf, double Royalty, double Cost, double Saas, double Users, double Scorecard);

    public sealed record TornadoRow(string Label, double Low, double High, double Base);

    public sealed record CheckRow(string Label, bool Ok);

    public sealed record SvfMonteCarlo(
        double P10, double P25, double P50, double P75, double P90, double Min, double Max);

    public sealed record SvfResult(
        double Wacc,
        double CostOfEquity,
        double SpecificPremium,
        ScenarioVals ScenarioVals,
        [property: JsonPropertyName("royaltyVal")] double RoyaltyVal,
        [property: JsonPropertyName("methodValues")] MethodValuesResult MethodValues,
        double Dcf,
        double Royalty,
        double Cost,
        double Saas,
        double SaasMultiple,
        double Users,
        double Scorecard,
        [property: JsonPropertyName("tornadoSweep")] TornadoRow[] TornadoSweep,
        CheckRow[] Checks,
        SvfMonteCarlo? MonteCarlo);
}
