using Application.Assessments.Commands.AddStep;

namespace RequestHandlers.Assessments.Commands.AddStep;

public sealed class AddStepCommandHandler(
    IAssessmentVersionCommandRepository versionRepository
) : IRequestHandler<AddStepCommand, Guid>
{
    public async Task<Guid> Handle(AddStepCommand request, CancellationToken ct)
    {
        var version = await versionRepository.GetTrackedWithStepsAsync(request.VersionId, ct)
            ?? throw ValuationException.NotFound("Version not found.");

        if (!version.IsDraft)
            throw ValuationException.Conflict("Steps can only be added to draft versions.");

        var step = new Domain.Assessments.Step
        {
            VersionId = request.VersionId,
            Title = request.Title,
            Description = request.Description,
            Order = request.Order
        };

        version.AddStep(step);
        await versionRepository.SaveChangesAsync(ct);

        return step.Id;
    }
}
