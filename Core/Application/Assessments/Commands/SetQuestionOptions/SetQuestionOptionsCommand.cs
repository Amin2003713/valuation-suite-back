using Application.Assessments.Commands.UpdateQuestion;
using MediatR;

namespace Application.Assessments.Commands.SetQuestionOptions;

public class SetQuestionOptionsCommand : IRequest
{
    public Guid QuestionId { get; set; }
    public List<OptionData> Options { get; set; } = [];
}
