using Application.Assessments.Commands.SetQuestionOptions;

namespace RequestHandlers.Assessments.Commands.SetQuestionOptions;

public sealed class SetQuestionOptionsCommandHandler(
    IAssessmentVersionCommandRepository versionRepository
) : IRequestHandler<SetQuestionOptionsCommand>
{
    public async Task Handle(SetQuestionOptionsCommand request, CancellationToken ct)
    {
        var version = await versionRepository.GetTrackedByQuestionIdAsync(request.QuestionId, ct)
            ?? throw ValuationException.NotFound("Question not found.");

        var question = version.Steps.SelectMany(s => s.Questions).First(q => q.Id == request.QuestionId);

        if (!version.IsDraft)
            throw ValuationException.Conflict("Only draft versions can be edited.");

        question.SetValidations(question.Validations); // keep existing validations untouched

        question.Options.Clear();
        foreach (var o in request.Options)
        {
            question.AddOption(new Domain.Assessments.Option
            {
                Label = o.Label,
                Value = o.Value,
                Order = o.Order,
                IsCorrect = o.IsCorrect,
                Score = o.Score
            });
        }

        await versionRepository.SaveChangesAsync(ct);
    }
}
