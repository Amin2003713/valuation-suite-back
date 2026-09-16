using Application.Attempts.Commands.CreateAttempt;
using Domain.Attempts;
using Domain.Assessments;

namespace RequestHandlers.Attempts.Commands.CreateAttempt;

public sealed class CreateAttemptCommandHandler(
    IAssessmentAttemptCommandRepository attemptRepository,
    IAssessmentAttemptQueryRepository attemptQueryRepository,
    IAssessmentVersionQueryRepository versionRepository
) : IRequestHandler<CreateAttemptCommand, Guid>
{
    public async Task<Guid> Handle(CreateAttemptCommand request, CancellationToken ct)
    {
        var version = await versionRepository.GetWithStepsAsync(request.VersionId, ct)
            ?? throw ValuationException.NotFound("Assessment version not found.");

        if (!version.IsPublished)
            throw ValuationException.Conflict("Assessment version is not published.");

        var existing = await attemptQueryRepository.GetActiveAttemptAsync(request.VersionId, request.UserId, ct);
        if (existing is not null)
            throw ValuationException.Conflict("An active attempt already exists for this assessment.");

        var attempt = AssessmentAttempt.Create(
            request.VersionId, request.UserId, request.CompanyId, version.Steps.Count);

        await attemptRepository.AddAsync(attempt, ct, saveNow: true);
        return attempt.Id;
    }
}
