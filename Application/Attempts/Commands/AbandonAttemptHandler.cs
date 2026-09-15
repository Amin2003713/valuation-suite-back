using MediatR;
using Domain.Attempts;
using Application.Common;

namespace Application.Attempts.Commands;

public class AbandonAttemptHandler : IRequestHandler<AbandonAttemptCommand>
{
    private readonly IAssessmentAttemptRepository _repo;
    private readonly IUnitOfWork _uow;

    public AbandonAttemptHandler(IAssessmentAttemptRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(AbandonAttemptCommand request, CancellationToken ct)
    {
        var attempt = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new InvalidOperationException("Attempt not found");
        attempt.Abandon();
        await _uow.SaveChangesAsync(ct);
    }
}
