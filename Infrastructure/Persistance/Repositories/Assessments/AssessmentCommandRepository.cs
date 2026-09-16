using Domain.Assessments;

namespace Persistence.Repositories.Assessments;

public class AssessmentCommandRepository(
    WriteOnlyDbContext dbContext,
    IdentityService identityService,
    ILogger<CommandRepository<Assessment>> logger
)
    : CommandRepository<Assessment>(dbContext, identityService, logger),
        IAssessmentCommandRepository
{
    public async Task<Assessment?> GetTrackedAsync(Guid id, CancellationToken ct = default)
        => await Table.FirstOrDefaultAsync(a => a.Id == id, ct);
}
