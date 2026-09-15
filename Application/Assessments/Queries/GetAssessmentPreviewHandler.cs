using MediatR;
using Application.Common;
using Application.Assessments.Mappers;
using Application.Assessments.Responses;

namespace Application.Assessments.Queries;

public class GetAssessmentPreviewHandler : IRequestHandler<GetAssessmentPreviewQuery, AssessmentVersionResponse?>
{
    private readonly IAssessmentVersionRepository _versionRepo;

    public GetAssessmentPreviewHandler(IAssessmentVersionRepository versionRepo) => _versionRepo = versionRepo;

    public async Task<AssessmentVersionResponse?> Handle(GetAssessmentPreviewQuery request, CancellationToken ct)
        => (await _versionRepo.GetByIdAsync(request.VersionId, ct))?.ToVersionResponse();
}
