using Application.Assessments.Responses;
using Application.Common;
using MediatR;

namespace Application.Attempts.Queries.GetAttempt;

public class GetAssessmentAttemptQuery : IRequest<AttemptResponse?>
{
    public Guid Id { get; set; }
}

public class GetActiveAttemptQuery : IRequest<AttemptResponse?>
{
    public Guid VersionId { get; set; }
    public Guid UserId { get; set; }
}

public class GetAttemptAnswersQuery : IRequest<List<AnswerResponse>>
{
    public Guid AttemptId { get; set; }
}

public class GetAssessmentHistoryQuery : IRequest<PagedResult<AttemptResponse>>
{
    public Guid UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
