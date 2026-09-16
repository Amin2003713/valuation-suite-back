using Application.Assessments.Responses;
using Application.Common;
using MediatR;

namespace Application.Assessments.Queries.GetAssessment;

public class GetAssessmentQuery : IRequest<AssessmentResponse?>
{
    public Guid Id { get; set; }
}

public class GetAssessmentsQuery : IRequest<PagedResult<AssessmentResponse>>
{
    public Guid CompanyId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
