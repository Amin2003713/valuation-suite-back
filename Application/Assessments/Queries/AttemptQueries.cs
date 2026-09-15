using MediatR;
using Domain.Assessments;
using Domain.Attempts;
using Application.Common;
using Application.Assessments.Responses;

namespace Application.Assessments.Queries;

public record GetAssessmentAttemptQuery(Guid Id) : IRequest<AttemptResponse?>;
public record GetActiveAttemptQuery(Guid VersionId, Guid UserId) : IRequest<AttemptResponse?>;
public record GetAttemptAnswersQuery(Guid AttemptId) : IRequest<List<AnswerResponse>>;
public record GetAssessmentHistoryQuery(Guid UserId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<AttemptResponse>>;
