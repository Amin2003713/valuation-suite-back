using MediatR;
using Domain.Common;

namespace Application.Assessments.Queries;

public record GetQuestionTypesQuery : IRequest<List<QuestionType>>;
public record GetQuestionTypeDetailsQuery : IRequest<List<QuestionTypeInfo>>;

public record QuestionTypeInfo(QuestionType Type, string Name, string Description, bool SupportsCalculation, bool SupportsConditional);
