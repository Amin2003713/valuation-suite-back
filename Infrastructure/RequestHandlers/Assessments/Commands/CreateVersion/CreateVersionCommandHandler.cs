namespace RequestHandlers.Assessments.Commands.CreateVersion;

public sealed class CreateVersionCommandHandler(
    IAssessmentCommandRepository assessmentRepository,
    IAssessmentVersionCommandRepository versionRepository
) : IRequestHandler<CreateVersionCommand, Guid>
{
    public async Task<Guid> Handle(CreateVersionCommand request, CancellationToken ct)
    {
        var assessment = await assessmentRepository.GetTrackedAsync(request.AssessmentId, ct)
            ?? throw ValuationException.NotFound("Assessment not found.");

        var existingVersions = await versionRepository.TableNoTracking
            .Where(v => v.AssessmentId == assessment.Id)
            .CountAsync(ct);

        var version = Domain.Assessments.AssessmentVersion.Create(assessment, existingVersions + 1, isDraft: true);
        assessment.Versions.Add(version);

        await versionRepository.AddAsync(version, ct, saveNow: true);
        return version.Id;
    }
}

public sealed class PublishAssessmentCommandHandler(
    IAssessmentCommandRepository assessmentRepository,
    IAssessmentVersionCommandRepository versionRepository
) : IRequestHandler<PublishAssessmentCommand>
{
    public async Task Handle(PublishAssessmentCommand request, CancellationToken ct)
    {
        var assessment = await assessmentRepository.GetTrackedAsync(request.AssessmentId, ct)
            ?? throw ValuationException.NotFound("Assessment not found.");

        var version = await versionRepository.GetTrackedWithStepsAsync(request.VersionId, ct)
            ?? throw ValuationException.NotFound("Version not found.");

        if (version.AssessmentId != request.AssessmentId)
            throw ValuationException.BadRequest("Version does not belong to assessment.");
        if (!version.IsDraft)
            throw ValuationException.Conflict("Only draft versions can be published.");
        if (version.Steps.Count == 0)
            throw ValuationException.BadRequest("A version with at least one step is required before publishing.");

        assessment.PublishVersion(request.VersionId);
        await assessmentRepository.SaveChangesAsync(ct);
    }
}
