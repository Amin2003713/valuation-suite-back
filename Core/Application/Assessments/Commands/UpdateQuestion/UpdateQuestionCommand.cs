using Domain.Common;
using MediatR;

namespace Application.Assessments.Commands.UpdateQuestion;

public class UpdateQuestionCommand : IRequest
{
    public Guid Id { get; set; }
    public string? Text { get; set; }
    public string? Key { get; set; }
    public bool? IsRequired { get; set; }
    public string? HelpText { get; set; }
}

public class RemoveQuestionCommand : IRequest
{
    public Guid QuestionId { get; set; }
}

public record OptionData(Guid? Id, string Label, string? Value, int Order, bool IsCorrect, double? Score);
public record ValidationData(string Field, ValidationType Type, double? MinValue, double? MaxValue, int? MinLength, int? MaxLength, string? Pattern, string? ErrorMessage);
public record VisibilityData(string TargetQuestionId, VisibilityConditionType Type, string? Value);
public record CalculationData(string Expression, List<string> InputQuestionIds, string OutputKey, string? Formula, Dictionary<string, double> Weights, string? ResultLabel);
public record MathExpressionData(string Expression, List<MathVariableData> Variables, List<MathOperationData> Operations, string? PostfixNotation);
public record MathVariableData(string Name, string Label, double MinValue, double MaxValue, double DefaultValue, bool IsRequired);
public record MathOperationData(OperationType Type, string? Operand, double? Constant);
