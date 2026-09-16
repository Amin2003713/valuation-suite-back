using Application.Attempts.Commands.CompleteAttempt;
using Domain.Assessments;

namespace RequestHandlers.Attempts.Commands.CompleteAttempt;

public sealed class CompleteAttemptCommandHandler(
    IAssessmentAttemptCommandRepository attemptRepository
) : IRequestHandler<CompleteAttemptCommand>
{
    public async Task Handle(CompleteAttemptCommand request, CancellationToken ct)
    {
        var attempt = await attemptRepository.GetTrackedAsync(request.Id, ct)
            ?? throw ValuationException.NotFound("Attempt not found.");

        if (attempt.Status != AttemptStatus.InProgress)
            throw ValuationException.Conflict("Attempt is no longer in progress.");

        attempt.Complete();
        await attemptRepository.SaveChangesAsync(ct);
    }
}
