using MediatR;
using Domain.Assessments;
using Application.Common;
using Application.Assessments.Mappers;
using Application.Assessments.Responses;

namespace Application.Assessments.Queries;

public class GetAssessmentForClientHandler : IRequestHandler<GetAssessmentForClientQuery, AssessmentVersionResponse?>
{
    private readonly IAssessmentVersionRepository _versionRepo;

    public GetAssessmentForClientHandler(IAssessmentVersionRepository versionRepo) => _versionRepo = versionRepo;

    public async Task<AssessmentVersionResponse?> Handle(GetAssessmentForClientQuery request, CancellationToken ct)
        => (await _versionRepo.GetPublishedVersionAsync(request.AssessmentId, ct))?.ToVersionResponse();
}
