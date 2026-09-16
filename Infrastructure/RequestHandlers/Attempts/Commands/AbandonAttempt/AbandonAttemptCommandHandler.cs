using Application.Attempts.Commands.AbandonAttempt;
using Domain.Assessments;

namespace RequestHandlers.Attempts.Commands.AbandonAttempt;

public sealed class AbandonAttemptCommandHandler(
    IAssessmentAttemptCommandRepository attemptRepository
) : IRequestHandler<AbandonAttemptCommand>
{
    public async Task Handle(AbandonAttemptCommand request, CancellationToken ct)
    {
        var attempt = await attemptRepository.GetTrackedAsync(request.Id, ct)
            ?? throw ValuationException.NotFound("Attempt not found.");

        if (attempt.Status != AttemptStatus.InProgress)
            throw ValuationException.Conflict("Attempt is no longer in progress.");

        attempt.Abandon();
        await attemptRepository.SaveChangesAsync(ct);
    }
}
