using Domain.Assessments;

namespace Application.Assessments.Responses;

public record AssessmentVersionResponse(
    Guid Id,
    Guid AssessmentId,
    int VersionNumber,
    string Title,
    bool IsDraft,
    bool IsPublished,
    DateTime? PublishedAt,
    int StepCount,
    int QuestionCount
);
