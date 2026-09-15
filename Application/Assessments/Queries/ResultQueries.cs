using MediatR;
using Domain.Results;
using Application.Assessments.Responses;
using Application.Common;

namespace Application.Assessments.Queries;

public record GetAssessmentResultQuery(Guid AttemptId) : IRequest<AssessmentResultResponse?>;
public record GetAssessmentResultsQuery(Guid AssessmentId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<AssessmentResultResponse>>;
