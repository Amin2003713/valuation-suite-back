using Domain.Assessments;

namespace Persistence.Repositories.Assessments;

public class AssessmentQueryRepository(
    ReadOnlyDbContext dbContext,
    ILogger<QueryRepository<Assessment>> logger
)
    : QueryRepository<Assessment>(dbContext, logger),
        IAssessmentQueryRepository
{
    public async Task<Assessment?> GetWithVersionsAsync(Guid id, CancellationToken ct = default)
        => await TableNoTracking
            .Include(a => a.Versions)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<List<Assessment>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default)
        => await TableNoTracking
            .Include(a => a.Versions)
            .Where(a => a.CompanyId == companyId)
            .ToListAsync(ct);
}
