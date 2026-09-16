namespace RequestHandlers.Assessments.Queries.GetAssessment;

public sealed class GetAssessmentQueryHandler(
    IAssessmentQueryRepository repository
) : IRequestHandler<GetAssessmentQuery, AssessmentResponse?>
{
    public async Task<AssessmentResponse?> Handle(GetAssessmentQuery request, CancellationToken ct)
    {
        var assessment = await repository.GetWithVersionsAsync(request.Id, ct);
        return assessment is null ? null : assessment.ToResponse();
    }
}
