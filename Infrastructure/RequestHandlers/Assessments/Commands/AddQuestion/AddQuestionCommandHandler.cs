using Application.Assessments.Commands.AddQuestion;

namespace RequestHandlers.Assessments.Commands.AddQuestion;

public sealed class AddQuestionCommandHandler(
    IAssessmentVersionCommandRepository versionRepository,
    IAssessmentVersionQueryRepository versionQueryRepository
) : IRequestHandler<AddQuestionCommand, Guid>
{
    public async Task<Guid> Handle(AddQuestionCommand request, CancellationToken ct)
    {
        var step = await versionQueryRepository.TableNoTracking
            .SelectMany(v => v.Steps)
            .FirstOrDefaultAsync(s => s.Id == request.StepId, ct)
            ?? throw ValuationException.NotFound("Step not found.");

        var version = await versionRepository.GetTrackedWithStepsAsync(step.VersionId, ct)
            ?? throw ValuationException.NotFound("Version not found.");

        if (!version.IsDraft)
            throw ValuationException.Conflict("Questions can only be added to draft versions.");

        var trackedStep = version.Steps.First(s => s.Id == request.StepId);

        var question = new Domain.Assessments.Question
        {
            StepId = trackedStep.Id,
            Text = request.Text,
            Type = request.Type,
            Order = request.Order
        };

        trackedStep.AddQuestion(question);
        await versionRepository.SaveChangesAsync(ct);

        return question.Id;
    }
}
