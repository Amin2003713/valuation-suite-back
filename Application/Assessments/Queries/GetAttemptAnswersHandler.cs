using MediatR;
using Domain.Assessments;
using Application.Common;
using Application.Assessments.Mappers;
using Application.Assessments.Responses;

namespace Application.Assessments.Queries;

public class GetAttemptAnswersHandler : IRequestHandler<GetAttemptAnswersQuery, List<AnswerResponse>>
{
    private readonly IAssessmentAttemptRepository _attemptRepo;

    public GetAttemptAnswersHandler(IAssessmentAttemptRepository attemptRepo) => _attemptRepo = attemptRepo;

    public async Task<List<AnswerResponse>> Handle(GetAttemptAnswersQuery request, CancellationToken ct)
    {
        var attempt = await _attemptRepo.GetByIdAsync(request.AttemptId, ct)
            ?? throw new InvalidOperationException("Attempt not found");
        return attempt.Answers.Select(a => a.ToAnswerResponse()).ToList();
    }
}
