using MediatR;
using Application.Common;
using Application.Assessments.Mappers;
using Application.Assessments.Responses;

namespace Application.Assessments.Queries;

public class GetAssessmentVersionsHandler : IRequestHandler<GetAssessmentVersionsQuery, List<AssessmentVersionResponse>>
{
    private readonly IAssessmentVersionRepository _versionRepo;

    public GetAssessmentVersionsHandler(IAssessmentVersionRepository versionRepo) => _versionRepo = versionRepo;

    public async Task<List<AssessmentVersionResponse>> Handle(GetAssessmentVersionsQuery request, CancellationToken ct)
    {
        // Note: This needs access to AssessmentId through a different approach
        // For now, let's query all versions and filter - in production use a specific query
        throw new NotImplementedException("Use GetAssessmentVersionQuery for specific version lookups");
    }
}
