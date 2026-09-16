using Domain.Attempts;
using Domain.Assessments;

namespace Persistence.Repositories.Attempts;

public class AssessmentAttemptQueryRepository(
    ReadOnlyDbContext dbContext,
    ILogger<QueryRepository<AssessmentAttempt>> logger
)
    : QueryRepository<AssessmentAttempt>(dbContext, logger),
        IAssessmentAttemptQueryRepository
{
    public async Task<AssessmentAttempt?> GetWithAnswersAsync(Guid id, CancellationToken ct = default)
        => await TableNoTracking
            .Include(a => a.Answers)
            .Include(a => a.Result)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<AssessmentAttempt?> GetActiveAttemptAsync(Guid versionId, Guid userId, CancellationToken ct = default)
        => await TableNoTracking
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(
                a => a.VersionId == versionId &&
                     a.UserId == userId &&
                     a.Status == AttemptStatus.InProgress,
                ct);

    public async Task<List<AssessmentAttempt>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await TableNoTracking
            .Include(a => a.Answers)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync(ct);
}
