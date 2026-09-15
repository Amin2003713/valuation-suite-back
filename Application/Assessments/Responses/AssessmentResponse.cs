using MediatR;

namespace Application.Assessments.Responses;

public record AssessmentResponse(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    Guid CompanyId,
    bool IsPublished,
    bool IsArchived,
    int PublishedVersion,
    int VersionCount,
    DateTime CreatedAt
);
