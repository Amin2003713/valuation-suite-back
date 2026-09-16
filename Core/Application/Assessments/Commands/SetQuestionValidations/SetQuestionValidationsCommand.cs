using Application.Assessments.Commands.UpdateQuestion;
using MediatR;

namespace Application.Assessments.Commands.SetQuestionValidations;

public class SetQuestionValidationsCommand : IRequest
{
    public Guid QuestionId { get; set; }
    public List<ValidationData> Validations { get; set; } = [];
}
