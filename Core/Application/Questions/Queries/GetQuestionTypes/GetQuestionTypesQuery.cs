using Domain.Common;
using MediatR;

namespace Application.Questions.Queries.GetQuestionTypes;

public class GetQuestionTypesQuery : IRequest<List<QuestionType>>;

public class GetQuestionTypeDetailsQuery : IRequest<List<QuestionTypeInfo>>;

public record QuestionTypeInfo(QuestionType Type, string Name, string Description, bool SupportsCalculation, bool SupportsConditional);
