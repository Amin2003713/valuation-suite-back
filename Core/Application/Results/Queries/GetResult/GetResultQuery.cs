using Application.Assessments.Responses;
using Application.Common;
using MediatR;

namespace Application.Results.Queries.GetResult;

public class GetAssessmentResultQuery : IRequest<AssessmentResultResponse?>
{
    public Guid AttemptId { get; set; }
}

public class GetAssessmentResultsQuery : IRequest<PagedResult<AssessmentResultResponse>>
{
    public Guid AssessmentId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
