using Application.Assessments.Queries.GetAssessmentVersion;

namespace RequestHandlers.Assessments.Queries.GetAssessmentVersion;

public sealed class GetAssessmentVersionsQueryHandler(
    IAssessmentVersionQueryRepository repository
) : IRequestHandler<GetAssessmentVersionsQuery, List<AssessmentVersionResponse>>
{
    public async Task<List<AssessmentVersionResponse>> Handle(GetAssessmentVersionsQuery request, CancellationToken ct)
    {
        var versions = await repository.GetByAssessmentAsync(request.AssessmentId, ct);
        return versions.Select(v => v.ToVersionResponse()).ToList();
    }
}

public sealed class GetAssessmentForClientQueryHandler(
    IAssessmentVersionQueryRepository repository
) : IRequestHandler<GetAssessmentForClientQuery, AssessmentVersionResponse?>
{
    public async Task<AssessmentVersionResponse?> Handle(GetAssessmentForClientQuery request, CancellationToken ct)
    {
        var version = await repository.GetPublishedVersionAsync(request.AssessmentId, ct);
        return version is null ? null : version.ToVersionResponse();
    }
}

public sealed class GetAssessmentPreviewQueryHandler(
    IAssessmentVersionQueryRepository repository
) : IRequestHandler<GetAssessmentPreviewQuery, AssessmentVersionResponse?>
{
    public async Task<AssessmentVersionResponse?> Handle(GetAssessmentPreviewQuery request, CancellationToken ct)
    {
        var version = request.VersionId != Guid.Empty
            ? await repository.GetWithStepsAsync(request.VersionId, ct)
            : await repository.GetCurrentDraftAsync(request.AssessmentId, ct);

        return version is null ? null : version.ToVersionResponse();
    }
}
