using MediatR;
using Domain.Assessments;
using Domain.Common;

namespace Application.Assessments.Commands;

public record CreateAssessmentCommand(string Name, string Code, string? Description, Guid CompanyId) : IRequest<Guid>;
public record UpdateAssessmentCommand(Guid Id, string? Name, string? Description) : IRequest;
public record DeleteAssessmentCommand(Guid Id) : IRequest;
public record PublishAssessmentCommand(Guid AssessmentId, Guid VersionId) : IRequest;
public record ArchiveAssessmentCommand(Guid AssessmentId) : IRequest;
public record CreateVersionCommand(Guid AssessmentId) : IRequest<Guid>;
public record AddStepCommand(Guid VersionId, string Title, string? Description, int Order) : IRequest<Guid>;
public record UpdateStepCommand(Guid Id, string? Title, string? Description) : IRequest;
public record ReorderStepsCommand(Guid VersionId, List<Guid> StepIdsInOrder) : IRequest;
public record RemoveStepCommand(Guid StepId) : IRequest;

public record AddQuestionCommand(Guid StepId, string Text, QuestionType Type, int Order) : IRequest<Guid>;
public record UpdateQuestionCommand(Guid Id, string? Text, string? Key, bool? IsRequired, string? HelpText) : IRequest;
public record SetQuestionOptionsCommand(Guid QuestionId, List<OptionData> Options) : IRequest;
public record SetQuestionValidationsCommand(Guid QuestionId, List<ValidationData> Validations) : IRequest;
public record SetQuestionVisibilityCommand(Guid QuestionId, List<VisibilityData> Conditions) : IRequest;
public record SetQuestionCalculationCommand(Guid QuestionId, CalculationData Calculation) : IRequest;
public record SetQuestionMathExpressionCommand(Guid QuestionId, MathExpressionData Expression) : IRequest;
public record RemoveQuestionCommand(Guid QuestionId) : IRequest;

public record OptionData(Guid? Id, string Label, string? Value, int Order, bool IsCorrect, double? Score);
public record ValidationData(string Field, ValidationType Type, double? MinValue, double? MaxValue, int? MinLength, int? MaxLength, string? Pattern, string? ErrorMessage);
public record VisibilityData(string TargetQuestionId, VisibilityConditionType Type, string? Value);
public record CalculationData(string Expression, List<string> InputQuestionIds, string OutputKey, string? Formula, Dictionary<string, double> Weights, string? ResultLabel);
public record MathExpressionData(string Expression, List<MathVariableData> Variables, List<MathOperationData> Operations, string? PostfixNotation);
public record MathVariableData(string Name, string Label, double MinValue, double MaxValue, double DefaultValue, bool IsRequired);
public record MathOperationData(OperationType Type, string? Operand, double? Constant);

public record CreateCompanyCommand(string Name, string Slug, string? Industry) : IRequest<Guid>;
public record UpdateCompanyCommand(Guid Id, string? Name, string? Industry) : IRequest;

public record UpgradeCompanyToProCommand(Guid Id) : IRequest;
public record DowngradeCompanyToFreeCommand(Guid Id) : IRequest;
