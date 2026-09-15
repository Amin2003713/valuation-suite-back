using MediatR;
using Domain.Attempts;
using Application.Common;
using Application.Assessments.Mappers;
using Application.Assessments.Responses;

namespace Application.Assessments.Queries;

public class GetAssessmentAttemptHandler : IRequestHandler<GetAssessmentAttemptQuery, AttemptResponse?>
{
    private readonly IAssessmentAttemptRepository _attemptRepo;

    public GetAssessmentAttemptHandler(IAssessmentAttemptRepository attemptRepo) => _attemptRepo = attemptRepo;

    public async Task<AttemptResponse?> Handle(GetAssessmentAttemptQuery request, CancellationToken ct)
        => (await _attemptRepo.GetByIdAsync(request.Id, ct))?.ToAttemptResponse();
}
