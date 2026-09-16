using Domain.Companies;

namespace Persistence.Repositories.Companies;

public class CompanyQueryRepository(
    ReadOnlyDbContext dbContext,
    ILogger<QueryRepository<Company>> logger
)
    : QueryRepository<Company>(dbContext, logger),
        ICompanyQueryRepository
{
    public async Task<Company?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => await TableNoTracking
            .Include(c => c.Assessments)
            .FirstOrDefaultAsync(c => c.Slug == slug, ct);
}
