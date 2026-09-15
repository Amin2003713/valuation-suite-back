using MediatR;
using Domain.Assessments;
using Application.Assessments.Responses;
using Application.Common;

namespace Application.Assessments.Queries;

public record GetAssessmentQuery(Guid Id) : IRequest<AssessmentResponse?>;

public record GetAssessmentsQuery(Guid CompanyId, int Page = 1, int PageSize = 20) : IRequest<Application.Common.PagedResult<AssessmentResponse>>;

public record GetAssessmentVersionsQuery(Guid AssessmentId) : IRequest<List<AssessmentVersionResponse>>;

public record GetAssessmentVersionQuery(Guid Id) : IRequest<AssessmentVersionResponse?>;

public record GetAssessmentForClientQuery(Guid AssessmentId) : IRequest<AssessmentVersionResponse?>;

public record GetAssessmentPreviewQuery(Guid AssessmentId, Guid VersionId) : IRequest<AssessmentVersionResponse?>;
