namespace RequestHandlers.Results.Queries.GetResult;

public sealed class GetAssessmentResultQueryHandler(
    IAssessmentResultQueryRepository repository
) : IRequestHandler<GetAssessmentResultQuery, AssessmentResultResponse?>
{
    public async Task<AssessmentResultResponse?> Handle(GetAssessmentResultQuery request, CancellationToken ct)
    {
        var result = await repository.GetByAttemptAsync(request.AttemptId, ct);
        return result is null ? null : result.ToResultResponse();
    }
}
