namespace RequestHandlers.Attempts.Queries.GetAttempt;

public sealed class GetAssessmentAttemptQueryHandler(
    IAssessmentAttemptQueryRepository repository
) : IRequestHandler<GetAssessmentAttemptQuery, AttemptResponse?>
{
    public async Task<AttemptResponse?> Handle(GetAssessmentAttemptQuery request, CancellationToken ct)
    {
        var attempt = await repository.GetWithAnswersAsync(request.Id, ct);
        return attempt is null ? null : attempt.ToAttemptResponse();
    }
}
