using Application.Assessments.Commands.UpdateStep;

namespace RequestHandlers.Assessments.Commands.UpdateStep;

public sealed class UpdateStepCommandHandler(
    IAssessmentVersionCommandRepository versionRepository
) : IRequestHandler<UpdateStepCommand>
{
    public async Task Handle(UpdateStepCommand request, CancellationToken ct)
    {
        var version = await versionRepository.GetTrackedByStepIdAsync(request.Id, ct)
            ?? throw ValuationException.NotFound("Step not found.");

        var step = version.Steps.First(s => s.Id == request.Id);

        if (!version.IsDraft)
            throw ValuationException.Conflict("Only draft versions can be edited.");

        if (!string.IsNullOrWhiteSpace(request.Title))
            step.Title = request.Title;
        if (request.Description is not null)
            step.SetDescription(request.Description);

        await versionRepository.SaveChangesAsync(ct);
    }
}

public sealed class ReorderStepsCommandHandler(
    IAssessmentVersionCommandRepository versionRepository
) : IRequestHandler<ReorderStepsCommand>
{
    public async Task Handle(ReorderStepsCommand request, CancellationToken ct)
    {
        var version = await versionRepository.GetTrackedWithStepsAsync(request.VersionId, ct)
            ?? throw ValuationException.NotFound("Version not found.");

        if (!version.IsDraft)
            throw ValuationException.Conflict("Only draft versions can be edited.");

        if (request.StepIdsInOrder.Count != version.Steps.Count ||
            request.StepIdsInOrder.Any(id => version.Steps.All(s => s.Id != id)))
            throw ValuationException.BadRequest("Step list does not match the version's steps.");

        version.ReorderSteps(request.StepIdsInOrder);
        await versionRepository.SaveChangesAsync(ct);
    }
}

public sealed class RemoveStepCommandHandler(
    IAssessmentVersionCommandRepository versionRepository
) : IRequestHandler<RemoveStepCommand>
{
    public async Task Handle(RemoveStepCommand request, CancellationToken ct)
    {
        var version = await versionRepository.GetTrackedByStepIdAsync(request.StepId, ct)
            ?? throw ValuationException.NotFound("Step not found.");

        if (!version.IsDraft)
            throw ValuationException.Conflict("Only draft versions can be edited.");

        version.RemoveStep(request.StepId);
        await versionRepository.SaveChangesAsync(ct);
    }
}
