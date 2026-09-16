namespace RequestHandlers.Assessments.Queries.GetAssessmentVersion;

public sealed class GetAssessmentVersionQueryHandler(
    IAssessmentVersionQueryRepository repository
) : IRequestHandler<GetAssessmentVersionQuery, AssessmentVersionResponse?>
{
    public async Task<AssessmentVersionResponse?> Handle(GetAssessmentVersionQuery request, CancellationToken ct)
    {
        var version = await repository.GetWithStepsAsync(request.Id, ct);
        return version is null ? null : version.ToVersionResponse();
    }
}
