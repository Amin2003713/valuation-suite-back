namespace RequestHandlers.Attempts.Queries.GetAttempt;

public sealed class GetAssessmentAttemptQueryHandler(
    IAssessmentAttemptQueryRepository repository,
    ICurrentUserAccessor currentUser
) : IRequestHandler<GetAssessmentAttemptQuery, AttemptResponse?>
{
    public async Task<AttemptResponse?> Handle(GetAssessmentAttemptQuery request, CancellationToken ct)
    {
        var attempt = await repository.GetWithAnswersAsync(request.Id, ct);
        if (attempt is null)
            return null;

        // Owner-scoped: guessing an attempt id no longer exposes another user's data.
        currentUser.RequireOwnership(attempt.UserId);

        return attempt.ToAttemptResponse();
    }
}
