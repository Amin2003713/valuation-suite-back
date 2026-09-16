using Application.Assessments.Commands.SetQuestionCalculation;

namespace RequestHandlers.Assessments.Commands.SetQuestionCalculation;

public sealed class SetQuestionCalculationCommandHandler(
    IAssessmentVersionCommandRepository versionRepository
) : IRequestHandler<SetQuestionCalculationCommand>
{
    public async Task Handle(SetQuestionCalculationCommand request, CancellationToken ct)
    {
        var version = await versionRepository.GetTrackedByQuestionIdAsync(request.QuestionId, ct)
            ?? throw ValuationException.NotFound("Question not found.");

        var question = version.Steps.SelectMany(s => s.Questions).First(q => q.Id == request.QuestionId);

        if (!version.IsDraft)
            throw ValuationException.Conflict("Only draft versions can be edited.");

        question.SetCalculation(new Domain.Common.CalculationConfig
        {
            Expression = request.Calculation.Expression,
            InputQuestionIds = request.Calculation.InputQuestionIds,
            OutputKey = request.Calculation.OutputKey,
            Formula = request.Calculation.Formula,
            Weights = request.Calculation.Weights,
            ResultLabel = request.Calculation.ResultLabel
        });

        await versionRepository.SaveChangesAsync(ct);
    }
}
