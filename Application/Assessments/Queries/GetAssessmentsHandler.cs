using MediatR;
using Domain.Assessments;
using Application.Common;
using Application.Assessments.Mappers;
using Application.Assessments.Responses;

namespace Application.Assessments.Queries;

public class GetAssessmentsHandler : IRequestHandler<GetAssessmentsQuery, PagedResult<AssessmentResponse>>
{
    private readonly IAssessmentRepository _repo;

    public GetAssessmentsHandler(IAssessmentRepository repo) => _repo = repo;

    public async Task<PagedResult<AssessmentResponse>> Handle(GetAssessmentsQuery request, CancellationToken ct)
    {
        var assessments = await _repo.GetByIdAsync(request.CompanyId, ct) != null
            ? new List<Assessment>() : throw new InvalidOperationException("Company not found");
        var paged = assessments
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => a.ToResponse())
            .ToList();

        return new PagedResult<AssessmentResponse>
        {
            Items = paged,
            TotalCount = assessments.Count,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
