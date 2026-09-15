using MediatR;
using Domain.Assessments;
using Application.Common;
using Application.Assessments.Mappers;
using Application.Assessments.Responses;

namespace Application.Assessments.Queries;

public class GetAssessmentResultHandler : IRequestHandler<GetAssessmentResultQuery, AssessmentResultResponse?>
{
    private readonly IAssessmentAttemptRepository _attemptRepo;

    public GetAssessmentResultHandler(IAssessmentAttemptRepository attemptRepo) => _attemptRepo = attemptRepo;

    public async Task<AssessmentResultResponse?> Handle(GetAssessmentResultQuery request, CancellationToken ct)
    {
        var attempt = await _attemptRepo.GetByIdAsync(request.AttemptId, ct)
            ?? throw new InvalidOperationException("Attempt not found");
        return attempt.Result?.ToResultResponse();
    }
}
