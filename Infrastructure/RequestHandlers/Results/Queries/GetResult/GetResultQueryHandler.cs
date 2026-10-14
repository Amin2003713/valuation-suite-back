namespace RequestHandlers.Results.Queries.GetResult;

public sealed class GetAssessmentResultQueryHandler(
    IAssessmentResultQueryRepository repository,
    IAssessmentAttemptQueryRepository attempts,
    ICurrentUserAccessor currentUser
) : IRequestHandler<GetAssessmentResultQuery, AssessmentResultResponse?>
{
    public async Task<AssessmentResultResponse?> Handle(GetAssessmentResultQuery request, CancellationToken ct)
    {
        var result = await repository.GetByAttemptAsync(request.AttemptId, ct);
        if (result is null)
            return null;

        // Results inherit the attempt's ownership: only the user who took the
        // assessment may read its score.
        var ownerId = await attempts.TableNoTracking
            .Where(a => a.Id == result.AttemptId)
            .Select(a => (Guid?)a.UserId)
            .FirstOrDefaultAsync(ct);

        if (ownerId is null)
            throw ValuationException.NotFound("Attempt not found.");

        currentUser.RequireOwnership(ownerId.Value);

        return result.ToResultResponse();
    }
}
