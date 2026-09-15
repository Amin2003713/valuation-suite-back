using MediatR;
using Domain.Assessments;
using Application.Common;
using Application.Assessments.Mappers;
using Application.Assessments.Responses;

namespace Application.Assessments.Queries;

public class GetAssessmentVersionHandler : IRequestHandler<GetAssessmentVersionQuery, AssessmentVersionResponse?>
{
    private readonly IAssessmentVersionRepository _versionRepo;

    public GetAssessmentVersionHandler(IAssessmentVersionRepository versionRepo) => _versionRepo = versionRepo;

    public async Task<AssessmentVersionResponse?> Handle(GetAssessmentVersionQuery request, CancellationToken ct)
        => (await _versionRepo.GetByIdAsync(request.Id, ct))?.ToVersionResponse();
}
