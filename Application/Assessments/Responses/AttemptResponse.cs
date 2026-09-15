using Domain.Assessments;

namespace Application.Assessments.Responses;

public record AttemptResponse(
    Guid Id,
    Guid VersionId,
    Guid UserId,
    Guid CompanyId,
    AttemptStatus Status,
    int TotalSteps,
    int CompletedSteps,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int ClientRevision,
    int AnswerCount
);
