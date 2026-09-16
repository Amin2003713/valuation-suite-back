using Application.Results.Commands.CalculateResult;
using Domain.Results;

namespace RequestHandlers.Results.Commands.CalculateResult;

public sealed class CalculateResultCommandHandler(
    IAssessmentResultCommandRepository resultRepository,
    IAssessmentAttemptCommandRepository attemptRepository,
    IAssessmentVersionQueryRepository versionRepository,
    IQuestionEngine questionEngine
) : IRequestHandler<CalculateResultCommand, Guid>
{
    public async Task<Guid> Handle(CalculateResultCommand request, CancellationToken ct)
    {
        var attempt = await attemptRepository.GetTrackedAsync(request.AttemptId, ct)
            ?? throw ValuationException.NotFound("Attempt not found.");

        if (attempt.Status != Domain.Assessments.AttemptStatus.Completed)
            throw ValuationException.Conflict("Attempt must be completed before calculating a result.");

        var version = await versionRepository.GetWithStepsAsync(attempt.VersionId, ct)
            ?? throw ValuationException.NotFound("Assessment version not found.");

        var (overallScore, level, levelLabel, stepScores, scoredAnswers) =
            questionEngine.CalculateResults(version, attempt.Answers);

        var result = attempt.Result ?? AssessmentResult.Create(attempt.Id, version.AssessmentId);
        result.Calculate(overallScore, level, levelLabel, stepScores, scoredAnswers);

        if (attempt.Result is null)
            await resultRepository.AddAsync(result, ct, saveNow: true);
        else
            await resultRepository.SaveChangesAsync(ct);

        return result.Id;
    }
}
