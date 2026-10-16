namespace Domain.Tools;

/// <summary>
///     Single source of truth mapping a tool code to its three per-tool entity types:
///     submissions (user answers), questions (input definitions) and localizations
///     (per-language tool info + question labels). Used by the write paths
///     (RunToolCommandHandler, seeders), the read paths (union queries) and the
///     EF model configuration.
///     Adding a tool: one entry here (+ its entity classes and one migration).
/// </summary>
public static class ToolRegistry
{
    public sealed record ToolTypes(Type Submission, Type Question, Type Localization);

    public static readonly IReadOnlyDictionary<string, ToolTypes> ByToolCode =
        new Dictionary<string, ToolTypes>(StringComparer.OrdinalIgnoreCase)
        {
            ["IDEA-ASSESS"] = new(typeof(IdeaAssessmentSubmission), typeof(IdeaAssessmentQuestion), typeof(IdeaAssessmentLocalization)),
            ["INNOV-READ"] = new(typeof(InnovationReadinessSubmission), typeof(InnovationReadinessQuestion), typeof(InnovationReadinessLocalization)),
            ["IP-AUDIT"] = new(typeof(IpAuditSubmission), typeof(IpAuditQuestion), typeof(IpAuditLocalization)),
            ["JOB-EVAL"] = new(typeof(JobEvaluationSubmission), typeof(JobEvaluationQuestion), typeof(JobEvaluationLocalization)),
            ["WIPO-DIAG"] = new(typeof(WipoDiagnosticsSubmission), typeof(WipoDiagnosticsQuestion), typeof(WipoDiagnosticsLocalization)),
            ["IPSCORE"] = new(typeof(IpscoreSubmission), typeof(IpscoreQuestion), typeof(IpscoreLocalization)),
            ["PATENT-SEARCH"] = new(typeof(PatentSearchSubmission), typeof(PatentSearchQuestion), typeof(PatentSearchLocalization)),
            ["STARTUP-VAL"] = new(typeof(StartupValuationSubmission), typeof(StartupValuationQuestion), typeof(StartupValuationLocalization)),
            ["BRAND-VAL"] = new(typeof(BrandValuationSubmission), typeof(BrandValuationQuestion), typeof(BrandValuationLocalization)),
            ["TRADEMARK-VAL"] = new(typeof(TrademarkValuationSubmission), typeof(TrademarkValuationQuestion), typeof(TrademarkValuationLocalization)),
            ["PATENT-VAL"] = new(typeof(PatentValuationSubmission), typeof(PatentValuationQuestion), typeof(PatentValuationLocalization)),
            ["PHARMA-IP"] = new(typeof(PharmaPatentValuationSubmission), typeof(PharmaPatentValuationQuestion), typeof(PharmaPatentValuationLocalization)),
            ["INTANGIBLE"] = new(typeof(IntangibleAssetsSubmission), typeof(IntangibleAssetsQuestion), typeof(IntangibleAssetsLocalization)),
            ["KNOWHOW"] = new(typeof(KnowhowValuationSubmission), typeof(KnowhowValuationQuestion), typeof(KnowhowValuationLocalization)),
            ["ICON-SCORECARD"] = new(typeof(IconScorecardSubmission), typeof(IconScorecardQuestion), typeof(IconScorecardLocalization)),
            ["BINOMIAL"] = new(typeof(BinomialValuationSubmission), typeof(BinomialValuationQuestion), typeof(BinomialValuationLocalization)),
            ["SOFT-VAL"] = new(typeof(SoftwareValuationSubmission), typeof(SoftwareValuationQuestion), typeof(SoftwareValuationLocalization)),
            ["RNPV"] = new(typeof(RnpvCalculatorSubmission), typeof(RnpvCalculatorQuestion), typeof(RnpvCalculatorLocalization)),
            ["PVI"] = new(typeof(PatentValueIndexSubmission), typeof(PatentValueIndexQuestion), typeof(PatentValueIndexLocalization)),
            ["KNOWHOW-PRO"] = new(typeof(KnowhowProSubmission), typeof(KnowhowProQuestion), typeof(KnowhowProLocalization)),
            ["EXPORT-READ"] = new(typeof(ExportReadinessSubmission), typeof(ExportReadinessQuestion), typeof(ExportReadinessLocalization)),
            ["DIGITAL-AI"] = new(typeof(DigitalAiReadinessSubmission), typeof(DigitalAiReadinessQuestion), typeof(DigitalAiReadinessLocalization)),
            ["MATURITY"] = new(typeof(MaturityAssessmentSubmission), typeof(MaturityAssessmentQuestion), typeof(MaturityAssessmentLocalization)),
        };

    public static IEnumerable<Type> SubmissionTypes => ByToolCode.Values.Select(v => v.Submission);
    public static IEnumerable<Type> QuestionTypes => ByToolCode.Values.Select(v => v.Question);
    public static IEnumerable<Type> LocalizationTypes => ByToolCode.Values.Select(v => v.Localization);

    public static bool TryGetSubmission(string toolCode, out Type entityType)
    {
        entityType = null!;
        return ByToolCode.TryGetValue(toolCode, out var t) && (entityType = t.Submission) is not null;
    }

    public static bool TryGetQuestion(string toolCode, out Type entityType)
    {
        entityType = null!;
        return ByToolCode.TryGetValue(toolCode, out var t) && (entityType = t.Question) is not null;
    }

    public static bool TryGetLocalization(string toolCode, out Type entityType)
    {
        entityType = null!;
        return ByToolCode.TryGetValue(toolCode, out var t) && (entityType = t.Localization) is not null;
    }

    /// <summary>
    ///     Creates the empty typed submission row for a tool and fills the shared columns.
    ///     Throws when the tool has no per-tool table (unknown code).
    /// </summary>
    public static ToolSubmissionBase CreateSubmission(
        string toolCode, Guid userId, string? name, string inputJson, string resultJson,
        double? overallScore, string language)
    {
        if (!TryGetSubmission(toolCode, out var type))
            throw new KeyNotFoundException($"No submission table registered for tool '{toolCode}'.");

        var row = (ToolSubmissionBase)Activator.CreateInstance(type)!;
        row.ToolCode = toolCode;
        row.UserId = userId;
        row.Name = name;
        row.InputJson = inputJson;
        row.ResultJson = resultJson;
        row.OverallScore = overallScore;
        row.Language = language;
        row.Status = ToolSubmissionStatus.Completed;
        return row;
    }

    /// <summary>Creates an empty typed question row for a tool (seeders).</summary>
    public static ToolQuestionBase CreateQuestion(string toolCode)
    {
        if (!TryGetQuestion(toolCode, out var type))
            throw new KeyNotFoundException($"No question table registered for tool '{toolCode}'.");

        var row = (ToolQuestionBase)Activator.CreateInstance(type)!;
        row.ToolCode = toolCode;
        return row;
    }

    /// <summary>Creates an empty typed localization row for a tool (seeders).</summary>
    public static ToolLocalizationBase CreateLocalization(string toolCode, string language, string questionId)
    {
        if (!TryGetLocalization(toolCode, out var type))
            throw new KeyNotFoundException($"No localization table registered for tool '{toolCode}'.");

        var row = (ToolLocalizationBase)Activator.CreateInstance(type)!;
        row.ToolCode = toolCode;
        row.Language = language;
        row.QuestionId = questionId;
        return row;
    }
}
