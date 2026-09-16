using Application.Assessments.Commands.UpdateQuestion;
using MediatR;

namespace Application.Assessments.Commands.SetQuestionVisibility;

public class SetQuestionVisibilityCommand : IRequest
{
    public Guid QuestionId { get; set; }
    public List<VisibilityData> Conditions { get; set; } = [];
}
