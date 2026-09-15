using MediatR;
using Domain.Attempts;
using Application.Common;

namespace Application.Attempts.Commands;

public class CompleteAttemptHandler : IRequestHandler<CompleteAttemptCommand>
{
    private readonly IAssessmentAttemptRepository _repo;
    private readonly IUnitOfWork _uow;

    public CompleteAttemptHandler(IAssessmentAttemptRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(CompleteAttemptCommand request, CancellationToken ct)
    {
        var attempt = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new InvalidOperationException("Attempt not found");
        attempt.Complete();
        await _uow.SaveChangesAsync(ct);
    }
}
