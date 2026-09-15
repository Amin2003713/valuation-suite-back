using MediatR;
using Domain.Results;
using Domain.Evaluation;
using Domain.Assessments;
using Domain.Answers;
using Application.Common;
using Application.Assessments.Mappers;
using Application.Assessments;

namespace Application.Results.Commands;

public class CalculateResultHandler : IRequestHandler<CalculateResultCommand, Guid>
{
    private readonly IAssessmentAttemptRepository _attemptRepo;
    private readonly IAssessmentVersionRepository _versionRepo;
    private readonly IUnitOfWork _uow;
    private readonly IQuestionEngine _questionEngine;
    private readonly IMathEngine _mathEngine;

    public CalculateResultHandler(
        IAssessmentAttemptRepository attemptRepo,
        IAssessmentVersionRepository versionRepo,
        IUnitOfWork uow,
        IQuestionEngine questionEngine,
        IMathEngine mathEngine)
    {
        _attemptRepo = attemptRepo;
        _versionRepo = versionRepo;
        _uow = uow;
        _questionEngine = questionEngine;
        _mathEngine = mathEngine;
    }

    public async Task<Guid> Handle(CalculateResultCommand request, CancellationToken ct)
    {
        var attempt = await _attemptRepo.GetWithAnswersAsync(request.AttemptId, ct)
            ?? throw new InvalidOperationException("Attempt not found");

        var version = await _versionRepo.GetByIdAsync(attempt.VersionId, ct)
            ?? throw new InvalidOperationException("Version not found");

        var (overallScore, level, levelLabel, stepScores, scoredAnswers) = _questionEngine.CalculateResults(
            version, attempt.Answers);

        var result = AssessmentResult.Create(request.AttemptId, version.AssessmentId);
        result.Calculate(overallScore, level, levelLabel, stepScores, scoredAnswers);

        await _uow.SaveChangesAsync(ct);

        return result.Id;
    }
}
