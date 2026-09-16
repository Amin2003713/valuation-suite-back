using Domain.Attempts;

namespace Persistence.Repositories.Attempts;

public class AssessmentAttemptCommandRepository(
    WriteOnlyDbContext dbContext,
    IdentityService identityService,
    ILogger<CommandRepository<AssessmentAttempt>> logger
)
    : CommandRepository<AssessmentAttempt>(dbContext, identityService, logger),
        IAssessmentAttemptCommandRepository
{
    public async Task<AssessmentAttempt?> GetTrackedAsync(Guid id, CancellationToken ct = default)
        => await Table
            .Include(a => a.Answers)
            .Include(a => a.Result)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
}
