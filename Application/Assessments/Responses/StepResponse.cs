using Domain.Assessments;

namespace Application.Assessments.Responses;

public record StepResponse(
    Guid Id,
    Guid VersionId,
    string Title,
    string? Description,
    int Order,
    int QuestionCount
);
