using Application.Attempts.Commands.CreateAttempt;
using Domain.Attempts;
using Domain.Assessments;

namespace RequestHandlers.Attempts.Commands.CreateAttempt;

public sealed class CreateAttemptCommandHandler(
    IAssessmentAttemptCommandRepository attemptRepository,
    IAssessmentAttemptQueryRepository attemptQueryRepository,
    IAssessmentVersionQueryRepository versionRepository,
    IUserQueryRepository userRepository,
    ICurrentUserAccessor currentUser
) : IRequestHandler<CreateAttemptCommand, Guid>
{
    public async Task<Guid> Handle(CreateAttemptCommand request, CancellationToken ct)
    {
        var version = await versionRepository.GetWithStepsAsync(request.VersionId, ct)
            ?? throw ValuationException.NotFound("Assessment version not found.");

        if (!version.IsPublished)
            throw ValuationException.Conflict("Assessment version is not published.");

        // The attempt always belongs to the caller: identity and company come from
        // the token / the user record, never from the request body.
        var userId = currentUser.RequireUserId();
        var companyId = currentUser.CompanyId
            ?? (await userRepository.GetWithCompanyAsync(userId, ct))?.CompanyId
            ?? Guid.Empty;

        var existing = await attemptQueryRepository.GetActiveAttemptAsync(request.VersionId, userId, ct);
        if (existing is not null)
            throw ValuationException.Conflict("An active attempt already exists for this assessment.");

        var attempt = AssessmentAttempt.Create(
            request.VersionId, userId, companyId, version.Steps.Count);

        await attemptRepository.AddAsync(attempt, ct, saveNow: true);
        return attempt.Id;
    }
}
