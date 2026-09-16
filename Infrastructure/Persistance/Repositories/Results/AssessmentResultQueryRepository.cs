using Domain.Results;

namespace Persistence.Repositories.Results;

public class AssessmentResultQueryRepository(
    ReadOnlyDbContext dbContext,
    ILogger<QueryRepository<AssessmentResult>> logger
)
    : QueryRepository<AssessmentResult>(dbContext, logger),
        IAssessmentResultQueryRepository
{
    public async Task<AssessmentResult?> GetByAttemptAsync(Guid attemptId, CancellationToken ct = default)
        => await TableNoTracking
            .FirstOrDefaultAsync(r => r.AttemptId == attemptId, ct);

    public async Task<List<AssessmentResult>> GetByAssessmentAsync(Guid assessmentId, CancellationToken ct = default)
        => await TableNoTracking
            .Where(r => r.AssessmentId == assessmentId)
            .OrderByDescending(r => r.CalculatedAt)
            .ToListAsync(ct);
}
