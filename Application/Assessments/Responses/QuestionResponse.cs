using Domain.Common;
using Domain.Assessments;

namespace Application.Assessments.Responses;

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
