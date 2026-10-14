using Application.Attempts.Queries.GetAttempt;

namespace RequestHandlers.Attempts.Queries.GetAttempt;

public sealed class GetActiveAttemptQueryHandler(
    IAssessmentAttemptQueryRepository repository,
    ICurrentUserAccessor currentUser
) : IRequestHandler<GetActiveAttemptQuery, AttemptResponse?>
{
    public async Task<AttemptResponse?> Handle(GetActiveAttemptQuery request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var attempt = await repository.GetActiveAttemptAsync(request.VersionId, userId, ct);
        return attempt is null ? null : attempt.ToAttemptResponse();
    }
}

public sealed class GetAttemptAnswersQueryHandler(
    IAssessmentAttemptQueryRepository repository,
    ICurrentUserAccessor currentUser
) : IRequestHandler<GetAttemptAnswersQuery, List<AnswerResponse>>
{
    public async Task<List<AnswerResponse>> Handle(GetAttemptAnswersQuery request, CancellationToken ct)
    {
        var attempt = await repository.GetWithAnswersAsync(request.AttemptId, ct)
            ?? throw ValuationException.NotFound("Attempt not found.");

        currentUser.RequireOwnership(attempt.UserId);

        return attempt.Answers.Select(a => a.ToAnswerResponse()).ToList();
    }
}

public sealed class GetAssessmentHistoryQueryHandler(
    IAssessmentAttemptQueryRepository repository,
    ICurrentUserAccessor currentUser
) : IRequestHandler<GetAssessmentHistoryQuery, PagedResult<AttemptResponse>>
{
    public async Task<PagedResult<AttemptResponse>> Handle(GetAssessmentHistoryQuery request, CancellationToken ct)
    {
        // The caller's own history — the user id is taken from the token, so the
        // endpoint no longer accepts someone else's id as a filter.
        var userId = currentUser.RequireUserId();

        var query = repository.TableNoTracking
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.StartedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(a => a.Answers)
            .ToListAsync(ct);

        return new PagedResult<AttemptResponse>
        {
            Items = items.Select(a => a.ToAttemptResponse()).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
