using Application.Assessments.Queries.GetAssessment;

namespace RequestHandlers.Assessments.Queries.GetAssessment;

public sealed class GetAssessmentsQueryHandler(
    IAssessmentQueryRepository repository
) : IRequestHandler<GetAssessmentsQuery, PagedResult<AssessmentResponse>>
{
    public async Task<PagedResult<AssessmentResponse>> Handle(GetAssessmentsQuery request, CancellationToken ct)
    {
        var query = repository.TableNoTracking
            .AsNoTracking()
            .Where(a => a.CompanyId == request.CompanyId && !a.IsArchived)
            .OrderByDescending(a => a.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(a => a.Versions)
            .ToListAsync(ct);

        return new PagedResult<AssessmentResponse>
        {
            Items = items.Select(a => a.ToResponse()).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
