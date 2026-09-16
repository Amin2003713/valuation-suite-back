using Application.Results.Queries.GetResult;

namespace RequestHandlers.Results.Queries.GetResult;

public sealed class GetAssessmentResultsQueryHandler(
    IAssessmentResultQueryRepository repository
) : IRequestHandler<GetAssessmentResultsQuery, PagedResult<AssessmentResultResponse>>
{
    public async Task<PagedResult<AssessmentResultResponse>> Handle(GetAssessmentResultsQuery request, CancellationToken ct)
    {
        var query = repository.TableNoTracking
            .Where(r => r.AssessmentId == request.AssessmentId)
            .OrderByDescending(r => r.CalculatedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<AssessmentResultResponse>
        {
            Items = items.Select(r => r.ToResultResponse()).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
