using MediatR;
using Domain.Attempts;
using Application.Common;
using Application.Assessments.Mappers;
using Application.Assessments.Responses;

namespace Application.Assessments.Queries;

public class GetActiveAttemptHandler : IRequestHandler<GetActiveAttemptQuery, AttemptResponse?>
{
    private readonly IAssessmentAttemptRepository _attemptRepo;

    public GetActiveAttemptHandler(IAssessmentAttemptRepository attemptRepo) => _attemptRepo = attemptRepo;

    public async Task<AttemptResponse?> Handle(GetActiveAttemptQuery request, CancellationToken ct)
        => (await _attemptRepo.GetActiveAttemptAsync(request.VersionId, request.UserId, ct))?.ToAttemptResponse();
}
