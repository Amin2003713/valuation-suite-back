namespace Application.Tools;

/// <summary>How far one element of a standard is actually produced by the tool today.</summary>
public enum StandardCoverage
{
    /// <summary>The tool asks for it and outputs it.</summary>
    InApp = 1,
    /// <summary>Partially present (e.g. user-entered, or only a summary of it).</summary>
    Partial = 2,
    /// <summary>Not produced by the product yet.</summary>
    Missing = 3,
}

/// <summary>Status is the serialised name ("InApp"/"Partial"/"Missing") so the API contract needs no enum converter.</summary>
public sealed record StandardElement(string Name, string Status, string Note);

/// <summary>
///     The standard a tool's calculation is modelled on, the methods its engine
///     applies, and an element-by-element coverage list. Declaring the basis,
///     methods and assumptions is itself a requirement of ISO 10668 (brand) and
///     IVS 210 (intangible assets), so every tool form now carries this block.
///     Coverage is deliberately conservative: an element is only "InApp" when the
///     engine actually produces it.
/// </summary>
public sealed record ToolStandardRow(
    string ToolCode,
    string Standard,
    string? Version,
    string? Url,
    string[] Methods,
    StandardElement[] Elements)
{
    /// <summary>Weighted coverage: InApp = 1, Partial = 0.5, Missing = 0.</summary>
    public int CoveragePercent => Elements.Length == 0
        ? 0
        : (int)Math.Round(
            Elements.Sum(e => e.Status switch
            {
                "InApp" => 1.0,
                "Partial" => 0.5,
                _ => 0.0,
            }) / Elements.Length * 100);

    public int CoveredElements => Elements.Count(e => e.Status == "InApp");
    public int TotalElements => Elements.Length;
}

/// <summary>Registry of the standards each tool is aligned to (single source of truth).</summary>
public static class ToolStandards
{
    private static StandardElement E(string name, StandardCoverage status, string note) =>
        new(name, status.ToString(), note);

    private const string Iso10668 = "ISO 10668:2010 — Brand valuation";
    private const string Iso10668Url = "https://www.iso.org/standard/46032.html";
    private const string Iso56005 = "ISO 56005:2020 — IP management in innovation";
    private const string Iso56005Url = "https://www.iso.org/standard/72761.html";
    private const string Ivs210 = "IVS 210 — Intangible assets";
    private const string Ivs210Url = "https://ivsc.org/";
    private const string IpScore = "EPO IPscore 3.0";
    private const string IpScoreUrl = "https://link.epo.org/web/IPscore_3.0_manual_en.pdf";
    private const string Ipev = "IPEV Valuation Guidelines + multi-method blend";
    private const string IpevUrl = "https://www.equidam.com/methodology";
    private const string KthIrl = "KTH Innovation Readiness Levels";
    private const string KthIrlUrl = "https://www.kth.se/en/om/innovation/innovation-readiness-levels-1.1002456";
    private const string WipoDiag = "WIPO IP Diagnostics";
    private const string WipoDiagUrl = "https://www.wipo.int/en/web/wipo-ip-diagnostics";
    private const string Ias38 = "IAS 38 / IVS 210 — intangible assets";
    private const string Ias38Url = "https://www.ifrs.org/issued-standards/list-of-standards/ias-38-intangible-assets/";
    private const string Aims = "ISO/IEC 42001:2023 — AI management system";
    private const string AimsUrl = "https://www.iso.org/standard/42001";
    private const string ItcSme = "ITC SME diagnostic (4 competencies / 19 capabilities)";
    private const string ItcSmeUrl = "https://www.itcbenchmarking.org/resources-and-help/our-methodology-smes";
    private const string Iso56002 = "ISO 56002 / ISO/TR 56004 — innovation management assessment";
    private const string Iso56002Url = "https://www.iso.org/standard/68221.html";
    private const string RnPvPractice = "Risk-adjusted NPV (rNPV) industry practice";
    private const string IvS210Options = "Real options (CRR) / IVS 210 option methods";
    private const string UsptoIndex = "Patent value index practice (IPQ / Patent Asset Index)";
    private const string OceanTomoUrl = "https://oceantomo.com/services/patent-indexes";
    private const string TradeSecret = "Trade-secret protection (WIPO / EU 2016/943)";
    private const string TradeSecretUrl = "https://www.wipo.int/en/web/trade-secrets";
    private const string CareerFit = "O*NET Work Importance Locator / Holland RIASEC career fit";
    private const string CareerFitUrl = "https://www.onetcenter.org/";

    public static IReadOnlyList<ToolStandardRow> All { get; } =
    [
        new("IDEA-ASSESS", Iso56002, "2019", Iso56002Url,
            ["Weighted multi-criteria screen"],
            [
                E("Opportunity identification", StandardCoverage.InApp, "Five-dimension idea screen."),
                E("Concept validation", StandardCoverage.Partial, "Scored, not stage-gated or evidenced."),
                E("Improvement actions", StandardCoverage.Missing, "No action plan output."),
            ]),

        new("INNOV-READ", KthIrl, "2021", KthIrlUrl,
            ["Nine-lens readiness scoring"],
            [
                E("All nine readiness lenses", StandardCoverage.InApp, "CRL/TRL/BRL/IPRL/TMRL/FRL/LRL/SCRL/SRL."),
                E("Level definitions", StandardCoverage.InApp, "Answer options map to levels."),
                E("Evidence per level", StandardCoverage.Missing, "No attachments."),
                E("Progress over time", StandardCoverage.Missing, "Runs are one-off."),
            ]),

        new("IP-AUDIT", Iso56005, "2020", Iso56005Url,
            ["Structured IP audit scoring"],
            [
                E("IP management review", StandardCoverage.InApp, "Audit questionnaire + score."),
                E("IP strategy linkage", StandardCoverage.Partial, "Assessed, not mapped to clauses."),
                E("Action plan", StandardCoverage.Missing, "Adviser writes it manually."),
                E("IP asset inventory", StandardCoverage.Missing, "No register/portfolio."),
            ]),

        // NOTE: this tool is a work-values / career-fit quiz, NOT job evaluation —
        // it must not be compared against Hay/Mercer point-factor standards.
        new("JOB-EVAL", CareerFit, null, CareerFitUrl,
            ["Work-value profiling", "Career-fit matching"],
            [
                E("Work-value profiling", StandardCoverage.InApp, "16 core values, always/never pick-lists."),
                E("Career-fit recommendations", StandardCoverage.InApp, "Scored job list from the picked values."),
                E("Validated psychometric scoring", StandardCoverage.Missing, "No normed instrument or reliability testing."),
                E("Role grading / pay bands", StandardCoverage.Missing, "Out of scope: not a job-evaluation tool."),
            ]),

        new("WIPO-DIAG", WipoDiag, "2021", WipoDiagUrl,
            ["Section-based IP diagnosis + scoring"],
            [
                E("IP self-assessment sections", StandardCoverage.InApp, "WIPO-style section questionnaire."),
                E("Recommendation report", StandardCoverage.Partial, "Scores + adviser chat, no generated report."),
                E("Pre-assessment branching", StandardCoverage.Missing, "All sections always shown."),
                E("Sector-specific variants", StandardCoverage.Missing, "No agri-food/video-games variants."),
            ]),

        new("IPSCORE", IpScore, "3.0", IpScoreUrl,
            ["IPscore assessment factors", "Financial forecast (NPV)"],
            [
                E("Five assessment categories", StandardCoverage.InApp, "Legal, technology, market, finance, strategy."),
                E("Per-patent scoring", StandardCoverage.InApp, "Patent matrix."),
                E("NPV financial forecast", StandardCoverage.InApp, "Finance inputs drive the value."),
                E("Risk/opportunity profile", StandardCoverage.Partial, "Category scores shown, no ISO-style risk/opportunity split."),
                E("Report output", StandardCoverage.Partial, "CSV export of the stored result."),
            ]),

        new("STARTUP-VAL", Ipev, "2024", IpevUrl,
            ["Scorecard / checklist", "DCF", "VC method", "Monte Carlo", "Scenario ranges"],
            [
                E("Multiple valuation methods", StandardCoverage.InApp, "Blended method results."),
                E("Method weighting by stage", StandardCoverage.Partial, "Fixed blend weights."),
                E("Assumption disclosure", StandardCoverage.InApp, "Inputs and methods returned with the result."),
                E("Regional benchmark anchors", StandardCoverage.Partial, "Industry benchmarks, no country valuation anchors."),
                E("Report output", StandardCoverage.Partial, "CSV export of the stored result."),
            ]),

        new("BRAND-VAL", Iso10668, "2010", Iso10668Url,
            ["Excess earnings", "Relief-from-royalty", "Binomial", "Black–Scholes", "Sensitivity", "Method blending"],
            [
                E("Financial analysis", StandardCoverage.InApp, "Multiple income approaches + blending."),
                E("Sensitivity of the value", StandardCoverage.InApp, "Engine computes sensitivity swings."),
                E("Behavioural analysis", StandardCoverage.Partial, "Brand-driver inputs, no research data."),
                E("Brand-strength / role-of-brand index", StandardCoverage.Missing, "No ISO 10668 brand-strength index."),
                E("Legal analysis", StandardCoverage.Missing, "No registration/class/territory module."),
            ]),

        new("STARTUP-ASSESS", "Investor-readiness practice", null, null,
            ["Axis scoring + peer benchmark"],
            [
                E("Traction/team/market scoring", StandardCoverage.InApp, "Axis scores 0–100."),
                E("Peer benchmark", StandardCoverage.InApp, "Industry radar."),
                E("Investor-readiness checklist", StandardCoverage.Missing, "No checklist output."),
            ]),

        new("TRADEMARK-VAL", Iso10668, "2010", Iso10668Url,
            ["Income approach (relief-from-royalty)"],
            [
                E("Financial analysis", StandardCoverage.InApp, "Mark-level income valuation."),
                E("Value declaration + method disclosure", StandardCoverage.InApp, "Standard/method block on the form."),
                E("Legal status per class/territory", StandardCoverage.Missing, "No class/territory register."),
                E("Royalty-rate evidence", StandardCoverage.Missing, "User-entered only."),
            ]),

        new("PATENT-VAL", Ivs210, "2025", Ivs210Url,
            ["rNPV / DCF", "Market approach"],
            [
                E("Method disclosure", StandardCoverage.InApp, "Standard/method block on the form."),
                E("Financial forecast", StandardCoverage.InApp, "rNPV/DCF engine."),
                E("Legal strength inputs", StandardCoverage.Partial, "Questionnaire-based."),
                E("Citation/family data", StandardCoverage.Missing, "No registry data feed."),
                E("Report output", StandardCoverage.Partial, "CSV export of the stored result."),
            ]),

        new("PHARMA-IP", "rNPV with clinical-stage attrition", null, null,
            ["Risk-adjusted NPV"],
            [
                E("Risk-adjusted cash flows", StandardCoverage.InApp, "Phase POS + rNPV."),
                E("Phase success-rate library", StandardCoverage.Missing, "POS is user-entered."),
                E("Patent life / PTE modelling", StandardCoverage.Missing, "Not modelled."),
            ]),

        new("INTANGIBLE", Ias38, null, Ias38Url,
            ["Component valuation (R&D, human capital, data)"],
            [
                E("Identifiable components", StandardCoverage.InApp, "Three modules with shares."),
                E("Method disclosure", StandardCoverage.InApp, "Standard/method block on the form."),
                E("Useful life / amortisation", StandardCoverage.Partial, "Straight-line schedule for capitalised R&D; HC/data components have none."),
                E("Impairment test", StandardCoverage.Missing, "Not modelled."),
            ]),

        // Seeder: "RFR + Premium + market + control scorecard + Monte Carlo range".
        new("ICON-SCORECARD", Iso10668, "2010", Iso10668Url,
            ["Relief-from-royalty", "Premium", "Market comparison", "Control scorecard", "Monte Carlo"],
            [
                E("Financial analysis (RFR)", StandardCoverage.InApp, "Royalty-relief value with a premium/market blend."),
                E("Control / driver scorecard", StandardCoverage.InApp, "Scorecard drives the value."),
                E("Uncertainty range", StandardCoverage.InApp, "Monte Carlo range."),
                E("Research-backed behavioural data", StandardCoverage.Missing, "No consumer research data."),
                E("Legal analysis", StandardCoverage.Missing, "No class/territory module."),
            ]),

        new("BINOMIAL", IvS210Options, null, null,
            ["Cox–Ross–Rubinstein lattice"],
            [
                E("Option-based valuation", StandardCoverage.InApp, "Binomial real-options engine."),
                E("Volatility / timing inputs", StandardCoverage.InApp, "Explicit model inputs."),
                E("Sensitivity analysis", StandardCoverage.InApp, "Tornado ±20% on S0, K, volatility, time and rate."),
            ]),

        new("SOFT-VAL", "Software IP valuation (cost / income / market)", null, null,
            ["Six valuation methods", "WACC", "Tornado", "Monte Carlo"],
            [
                E("Multiple valuation methods", StandardCoverage.InApp, "Six methods + blending."),
                E("Discount rate construction", StandardCoverage.InApp, "WACC inputs."),
                E("Sensitivity analysis", StandardCoverage.InApp, "Tornado ±20% per driver."),
                E("Uncertainty simulation", StandardCoverage.InApp, "Monte Carlo."),
            ]),

        new("RNPV", RnPvPractice, null, null,
            ["Risk-adjusted NPV", "Tornado sensitivity"],
            [
                E("Stage-wise risk adjustment", StandardCoverage.InApp, "Cumulative POS applied per stage."),
                E("Milestone and sales-milestone risk", StandardCoverage.InApp, "Different POS per cash-flow class."),
                E("Sensitivity analysis", StandardCoverage.InApp, "Tornado on discount, royalty, peak sales and POS (±20%)."),
                E("Stage success-rate library", StandardCoverage.Missing, "POS is user-entered."),
            ]),

        new("PVI", UsptoIndex, null, OceanTomoUrl,
            ["Index-style patent scoring"],
            [
                E("Index score", StandardCoverage.InApp, "Weighted patent attributes."),
                E("Citation / family inputs", StandardCoverage.Missing, "No registry data feed."),
                E("Portfolio benchmarking", StandardCoverage.Missing, "Single patent at a time."),
            ]),

        new("KNOWHOW-PRO", TradeSecret, null, TradeSecretUrl,
            ["Know-how valuation + protection readiness"],
            [
                E("Know-how valuation", StandardCoverage.InApp, "Income-based engine."),
                E("Protection readiness", StandardCoverage.Partial, "Questions, no evidence register."),
                E("Secrecy measures register", StandardCoverage.Missing, "No NDA/access log."),
            ]),

        new("EXPORT-READ", ItcSme, null, ItcSmeUrl,
            ["Readiness scoring + benchmark"],
            [
                E("Competency scoring", StandardCoverage.InApp, "Export-readiness dimensions."),
                E("Peer benchmark", StandardCoverage.InApp, "Industry radar."),
                E("Trade-regulation compliance module", StandardCoverage.Missing, "No market requirement comparison."),
            ]),

        new("DIGITAL-AI", Aims, "2023", AimsUrl,
            ["Digital/AI readiness scoring"],
            [
                E("Readiness dimensions", StandardCoverage.InApp, "Scored questionnaire."),
                E("ISO/IEC 42001 clause mapping", StandardCoverage.Missing, "No clause-by-clause output."),
                E("AI risk register / SoA", StandardCoverage.Missing, "Not produced."),
            ]),

        new("MATURITY", Iso56002, "2019", Iso56002Url,
            ["Dimension maturity scoring"],
            [
                E("Maturity assessment", StandardCoverage.InApp, "Dimension levels + total."),
                E("ISO 56002 element mapping", StandardCoverage.Missing, "No element-by-element view."),
                E("Improvement roadmap", StandardCoverage.Missing, "Not generated."),
            ]),

        new("AMBIDEXTERITY", "O'Reilly & Tushman ambidexterity framework", null, null,
            ["Exploration/exploitation axis scoring"],
            [
                E("Exploration vs exploitation balance", StandardCoverage.InApp, "Axis scoring."),
                E("Leadership/structural view", StandardCoverage.Partial, "Attitude questions only."),
                E("Action roadmap", StandardCoverage.Missing, "Not generated."),
            ]),
    ];

    public static ToolStandardRow? For(string toolCode) =>
        All.FirstOrDefault(t => string.Equals(t.ToolCode, toolCode, StringComparison.OrdinalIgnoreCase));
}
