using Domain.Common;
using MediatR;

namespace Application.Assessments.Commands.AddQuestion;

public class AddQuestionCommand : IRequest<Guid>
{
    public Guid StepId { get; set; }
    public string Text { get; set; } = default!;
    public QuestionType Type { get; set; } = QuestionType.Text;
    public int Order { get; set; }
}
