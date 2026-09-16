using Application.Assessments.Commands.UpdateQuestion;
using MediatR;

namespace Application.Assessments.Commands.SetQuestionCalculation;

public class SetQuestionCalculationCommand : IRequest
{
    public Guid QuestionId { get; set; }
    public CalculationData Calculation { get; set; } = default!;
}
