using Domain.Common;
using Domain.Companies;

namespace Application.Assessments.Responses;

public record CompanyResponse(
    Guid Id,
    string Name,
    string Slug,
    string? LogoUrl,
    string? Industry,
    Domain.Companies.Plan Plan,
    bool IsActive,
    int AssessmentCount
);
