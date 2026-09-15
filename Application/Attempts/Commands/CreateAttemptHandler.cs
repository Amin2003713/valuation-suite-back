using MediatR;
using Domain.Attempts;
using Domain.Answers;
using Application.Common;
using Application.Assessments.Mappers;

namespace Application.Attempts.Commands;

public class CreateAttemptHandler : IRequestHandler<CreateAttemptCommand, Guid>
{
    private readonly IAssessmentVersionRepository _versionRepo;
    private readonly IAssessmentAttemptRepository _attemptRepo;
    private readonly IUnitOfWork _uow;

    public CreateAttemptHandler(IAssessmentVersionRepository versionRepo, IAssessmentAttemptRepository attemptRepo, IUnitOfWork uow)
    {
        _versionRepo = versionRepo;
        _attemptRepo = attemptRepo;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateAttemptCommand request, CancellationToken ct)
    {
        var version = await _versionRepo.GetByIdAsync(request.VersionId, ct)
            ?? throw new InvalidOperationException("Version not found");

        var existing = await _attemptRepo.GetActiveAttemptAsync(request.VersionId, request.UserId, ct);
        if (existing != null)
            return existing.Id;

        var attempt = AssessmentAttempt.Create(request.VersionId, request.UserId, request.CompanyId, version.Steps.Count);
        await _attemptRepo.AddAsync(attempt, ct);
        await _uow.SaveChangesAsync(ct);
        return attempt.Id;
    }
}
