using Application.Assessments.Commands.UpdateQuestion;
using MediatR;

namespace Application.Assessments.Commands.SetQuestionMathExpression;

public class SetQuestionMathExpressionCommand : IRequest
{
    public Guid QuestionId { get; set; }
    public MathExpressionData Expression { get; set; } = default!;
}
