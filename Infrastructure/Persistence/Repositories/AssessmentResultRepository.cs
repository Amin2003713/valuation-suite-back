using Domain.Results;
using Infrastructure.Persistence.Repositories;
using Application.Common;

namespace Infrastructure.Persistence.Repositories;

public class AssessmentResultRepository
{
    private readonly IAssessmentAttemptRepository _attemptRepo;

    public AssessmentResultRepository(IAssessmentAttemptRepository attemptRepo)
        => _attemptRepo = attemptRepo;

    public async Task<AssessmentResult?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var attempt = await _attemptRepo.GetByIdAsync(id, ct);
        return attempt?.Result;
    }

    public async Task AddAsync(AssessmentResult result, CancellationToken ct = default)
    {
        // Stored via AssessmentAttempt navigation property
        // This is handled through the attempt update
    }
}
