using Domain.Answers;
using Domain.Assessments;
using Domain.Common;
using Domain.Companies;
using Domain.Results;

namespace Application.Assessments.Responses;

public record AssessmentResponse(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    Guid CompanyId,
    bool IsPublished,
    bool IsArchived,
    int PublishedVersion,
    int VersionCount,
    DateTime CreatedAt
);

public record AssessmentVersionResponse(
    Guid Id,
    Guid AssessmentId,
    int VersionNumber,
    string Title,
    bool IsDraft,
    bool IsPublished,
    DateTime? PublishedAt,
    int StepCount,
    int QuestionCount
);

public record StepResponse(
    Guid Id,
    Guid VersionId,
    string Title,
    string? Description,
    int Order,
    int QuestionCount
);

public record QuestionResponse(
    Guid Id,
    Guid StepId,
    string Text,
    string? Key,
    QuestionType Type,
    int Order,
    bool IsRequired,
    string? HelpText,
    List<OptionResponse> Options,
    List<ValidationRuleResponse> Validations,
    List<VisibilityConditionResponse> VisibilityConditions,
    CalculationConfigResponse? Calculation,
    MathExpressionResponse? MathExpression
);

public record OptionResponse(Guid Id, string Label, string? Value, int Order, bool IsCorrect, double? Score);

public record ValidationRuleResponse(
    string Field,
    ValidationType Type,
    double? MinValue,
    double? MaxValue,
    int? MinLength,
    int? MaxLength,
    string? Pattern,
    string? ErrorMessage
);

public record VisibilityConditionResponse(
    string TargetQuestionId,
    VisibilityConditionType Type,
    string? Value
);

public record CalculationConfigResponse(
    string Expression,
    List<string> InputQuestionIds,
    string OutputKey,
    string? Formula,
    Dictionary<string, double> Weights,
    string? ResultLabel
);

public record MathExpressionResponse(
    string Expression,
    List<MathVariableResponse> Variables,
    List<MathOperationResponse> Operations,
    string? PostfixNotation
);

public record MathVariableResponse(string Name, string Label, double MinValue, double MaxValue, double DefaultValue, bool IsRequired);
public record MathOperationResponse(OperationType Type, string? Operand, double? Constant);

public record AttemptResponse(
    Guid Id,
    Guid VersionId,
    Guid UserId,
    Guid CompanyId,
    AttemptStatus Status,
    int TotalSteps,
    int CompletedSteps,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int ClientRevision,
    int AnswerCount
);

public record AnswerResponse(
    Guid Id,
    Guid AttemptId,
    Guid QuestionId,
    AnswerValueType ValueType,
    string? TextValue,
    double? NumericValue,
    bool? BooleanValue,
    DateTime? DateValue,
    List<string>? ChoiceValues,
    int ClientRevision,
    DateTime SynchronizedAt
);

public record AssessmentResultResponse(
    Guid Id,
    Guid AttemptId,
    Guid AssessmentId,
    int OverallScore,
    string? Level,
    string? LevelLabel,
    bool IsCalculated,
    DateTime CalculatedAt,
    Dictionary<string, int> StepScores,
    List<ScoredAnswerResponse> ScoredAnswers
);

public record ScoredAnswerResponse(
    Guid QuestionId,
    string QuestionKey,
    QuestionType QuestionType,
    int Score,
    int MaxScore,
    double? CalculatedValue
);

public record CompanyResponse(
    Guid Id,
    string Name,
    string Slug,
    string? LogoUrl,
    string? Industry,
    Plan Plan,
    bool IsActive,
    int AssessmentCount
);
