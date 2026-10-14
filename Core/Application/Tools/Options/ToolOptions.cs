using Microsoft.Extensions.Options;

namespace Application.Tools.Options;

/* ── Option record shapes used by the generated reference tables ─────────── */

public sealed class PviLeafOption
{
    public string Id { get; set; } = string.Empty;
    public double F { get; set; }
}

public sealed class PviParamOption
{
    public double Weight { get; set; }
    public PviLeafOption[] Leaves { get; set; } = [];
}

public sealed class PviGroupOption
{
    public double Weight { get; set; }
    public PviParamOption[] Params { get; set; } = [];
}

public sealed class DigitalQuestionOption
{
    public string Id { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public bool Critical { get; set; }
    public string RiskFa { get; set; } = string.Empty;
    public string RiskEn { get; set; } = string.Empty;
}

public sealed class DigitalIndicatorOption
{
    public string Id { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Base { get; set; } = string.Empty;
    /** "l" = low is bad, "h" = high is bad. */
    public string Dir { get; set; } = "l";
    public double Red { get; set; }
    public double Yellow { get; set; }
    public bool Critical { get; set; }
}

public sealed class MaturityDomainOption
{
    public string Key { get; set; } = string.Empty;
    public double Weight { get; set; }
}

public sealed class MaturityQuestionOption
{
    public int Index { get; set; }
    public string Domain { get; set; } = string.Empty;
    public bool Critical { get; set; }
}

/** One specialist assessment row: the goal toggle and the domain whose score drives it. */
public sealed class MaturitySpecialOption
{
    public string GoalKey { get; set; } = string.Empty;
    public string DomainKey { get; set; } = string.Empty;
}

public sealed class ExportDomainOption
{
    public string Id { get; set; } = string.Empty;
    public double Weight { get; set; }
    public int QuestionCount { get; set; }
}

/* ── Per-tool strongly-typed options (bound from "Tools:{code}" sections) ── */

/// <summary>Patent Value Index — band thresholds and the group/param/leaf weights.</summary>
public sealed class PviOptions
{
    public const string SectionName = "Tools:Pvi";

    /** [key, min] pairs, highest first; last band catches everything below. */
    public (string Key, double Min)[] Bands { get; set; } =
        [("band1", 80), ("band2", 65), ("band3", 50), ("band4", 35), ("band5", double.NegativeInfinity)];

    public double DefaultScore { get; set; } = 5;

    public PviGroupOption[] Groups { get; set; } = PviTables.Groups;
}

/// <summary>Digital &amp; AI readiness — answer-code scores, coverage/level thresholds, tables.</summary>
public sealed class DigitalAiOptions
{
    public const string SectionName = "Tools:DigitalAi";

    /** Code 1/2/3 → sub-score. */
    public double[] CodeScores { get; set; } = [100, 50, 0];

    /** Questions per section (the coverage denominator the source used). */
    public int QuestionsPerSection { get; set; } = 12;

    /** Coverage at/above which a section level is considered reliable. */
    public double CoverageOk { get; set; } = 0.75;

    /** Level cutoffs on the section mean sub-score. */
    public double Level1Max { get; set; } = 50;
    public double Level2Max { get; set; } = 80;

    public DigitalQuestionOption[] Questions { get; set; } = DigitalTables.Questions;
    public DigitalIndicatorOption[] Indicators { get; set; } = DigitalTables.Indicators;
}

/// <summary>Maturity assessment — domain weights, evidence coefficients, gate/level thresholds.</summary>
public sealed class MaturityOptions
{
    public const string SectionName = "Tools:Maturity";

    /** Evidence text → coefficient (source: valid 1 · limited 0.75 · none 0.5). */
    public Dictionary<string, double> EvidenceCoefficients { get; set; } = new()
    {
        ["مدرک معتبر و جاری"] = 1.0,
        ["مدرک محدود یا قدیمی"] = 0.75,
    };

    public double EvidenceDefaultCoefficient { get; set; } = 0.5;

    /** A critical indicator scoring below this fails the gate. */
    public double CriticalThreshold { get; set; } = 3;

    /** Raw-sum divisor: 6 indicators × max score 5. */
    public double DomainMaxRaw { get; set; } = 30;

    /** Indicator count (the completion denominator). */
    public int TotalQuestions { get; set; } = 60;

    /** The "goal selected" marker the page sends in the goals map (YES = 'بله'). */
    public string YesValue { get; set; } = "بله";

    /** Maturity level cutoffs on the weighted total. */
    public double Level1Max { get; set; } = 40;
    public double Level2Max { get; set; } = 60;
    public double Level3Max { get; set; } = 80;

    /** Domain analysis cutoffs on the 0–100 domain score. */
    public double AnalysisGoodMin { get; set; } = 75;
    public double AnalysisWeakMax { get; set; } = 50;

    public MaturityDomainOption[] Domains { get; set; } = MaturityTables.Domains;
    public MaturityQuestionOption[] Questions { get; set; } = MaturityTables.Questions;
    public MaturitySpecialOption[] Specials { get; set; } = MaturityTables.Specials;
}

/// <summary>Export readiness — domain weights and status/level/recommendation cutoffs.</summary>
public sealed class ExportReadinessOptions
{
    public const string SectionName = "Tools:ExportReadiness";

    /** Max score per question (the 0–5 anchor scale). */
    public double QuestionMax { get; set; } = 5;

    /** Domain status cutoffs on the 0–100 domain score. */
    public double GoodMin { get; set; } = 75;
    public double WeakMax { get; set; } = 50;

    /** Readiness level cutoffs on the overall score. */
    public double ReadyMin { get; set; } = 80;
    public double ConditionalMin { get; set; } = 60;
    public double DevelopingMin { get; set; } = 40;

    public ExportDomainOption[] Domains { get; set; } = ExportTables.Domains;
}

/// <summary>rNPV — milestone labels indexed 0..5 (upfront + five milestones).</summary>
public sealed class RnpvOptions
{
    public const string SectionName = "Tools:Rnpv";

    public string[] MilestoneLabels { get; set; } =
        ["امضا/Upfront", "ورود فاز I", "ورود فاز II", "ورود فاز III", "ثبت NDA", "تأیید/عرضه"];

    public int StageCount { get; set; } = 5;
}

/// <summary>Binomial research valuation — slider ranges used for server-side validation.</summary>
public sealed class BinomialOptions
{
    public const string SectionName = "Tools:Binomial";

    public double S0Min { get; set; } = 0;
    public double S0Max { get; set; } = 1_000_000;
    public double KMin { get; set; } = 0;
    public double KMax { get; set; } = 1_000_000;
    public double TMin { get; set; } = 0.01;
    public double TMax { get; set; } = 100;
    public double SigmaMin { get; set; } = 0;
    public double SigmaMax { get; set; } = 5;
    public double RMin { get; set; } = -1;
    public double RMax { get; set; } = 1;
    public int StepsMin { get; set; } = 1;
    public int StepsMax { get; set; } = 200;
}

/// <summary>Software valuation framework — stage/industry cascade tables, MC sample count, bands.</summary>
public sealed class SvfOptions
{
    public const string SectionName = "Tools:Svf";

    public sealed class StageRow
    {
        public double SpecificPremium { get; set; }
        public double TechDebtPct { get; set; }
        public double Nrr { get; set; }
        public double ScorecardBase { get; set; }
    }

    public sealed class IndustryRow
    {
        public double RoyP { get; set; }
        public double RoyL { get; set; }
        public double RoyO { get; set; }
        public double RebuildMult { get; set; }
        public double ValuePerUser { get; set; }
        public double ScorecardMult { get; set; }
        public double SizePremium { get; set; }
    }

    public Dictionary<string, StageRow> StageTable { get; set; } = new()
    {
        ["idea"] = new() { SpecificPremium = 12, TechDebtPct = 30, Nrr = 85, ScorecardBase = 1 },
        ["mvp"] = new() { SpecificPremium = 8, TechDebtPct = 22, Nrr = 92, ScorecardBase = 3 },
        ["growth"] = new() { SpecificPremium = 5, TechDebtPct = 14, Nrr = 105, ScorecardBase = 8 },
        ["scale"] = new() { SpecificPremium = 3, TechDebtPct = 8, Nrr = 112, ScorecardBase = 20 },
        ["mature"] = new() { SpecificPremium = 2, TechDebtPct = 5, Nrr = 102, ScorecardBase = 40 },
    };

    public Dictionary<string, IndustryRow> IndustryTable { get; set; } = new()
    {
        ["enterprise"] = new() { RoyP = 4, RoyL = 6, RoyO = 8, RebuildMult = 1.4, ValuePerUser = 15, ScorecardMult = 1.2, SizePremium = 8 },
        ["fintech"] = new() { RoyP = 3, RoyL = 5, RoyO = 7, RebuildMult = 1.5, ValuePerUser = 10, ScorecardMult = 1.4, SizePremium = 9 },
        ["ecommerce"] = new() { RoyP = 2, RoyL = 4, RoyO = 6, RebuildMult = 1.2, ValuePerUser = 3, ScorecardMult = 1.0, SizePremium = 7 },
        ["health"] = new() { RoyP = 5, RoyL = 7, RoyO = 9, RebuildMult = 1.5, ValuePerUser = 12, ScorecardMult = 1.3, SizePremium = 9 },
        ["edtech"] = new() { RoyP = 3, RoyL = 5, RoyO = 7, RebuildMult = 1.2, ValuePerUser = 2, ScorecardMult = 0.9, SizePremium = 7 },
        ["media"] = new() { RoyP = 2, RoyL = 4, RoyO = 6, RebuildMult = 1.1, ValuePerUser = 1.5, ScorecardMult = 0.8, SizePremium = 6 },
    };

    /** SaaS revenue multiple bands keyed by minimum growth rate (ascending). */
    public (double MinG, double Multiple)[] SaasBands { get; set; } =
        [(0.6, 8), (0.3, 5), (0.1, 3), (double.NegativeInfinity, 2)];

    /** Maturity tilt: adj = 1 + (3 − avg)·factor, floored at Floor. */
    public double MaturityTiltFactor { get; set; } = 0.12;
    public double MaturityTiltFloor { get; set; } = 0.4;

    public double TornadoSwing { get; set; } = 0.2;

    public int MonteCarloRuns { get; set; } = 4000;
}

/// <summary>Know-how valuation pro — sensitivity deltas, MC runs, scenario mix.</summary>
public sealed class KnowhowOptions
{
    public const string SectionName = "Tools:KnowhowPro";

    public double[] SensDeltas { get; set; } = [-0.05, -0.025, 0, 0.025, 0.05];

    public int MonteCarloRuns { get; set; } = 5000;

    /** combo = dcfDcfWeight·dcf + (1 − w)·royalty. */
    public double ScenarioDcfWeight { get; set; } = 0.5;

    /** DCF explicit years, tail years and the last explicit year feeding the tail. */
    public int DcfYears { get; set; } = 6;
    public int DcfPvYears { get; set; } = 5;
    public int DcfTailYears { get; set; } = 3;

    public int RoyaltyYears { get; set; } = 5;
}
