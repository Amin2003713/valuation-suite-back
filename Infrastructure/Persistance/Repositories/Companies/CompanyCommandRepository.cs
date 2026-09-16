using Domain.Companies;

namespace Persistence.Repositories.Companies;

public class CompanyCommandRepository(
    WriteOnlyDbContext dbContext,
    IdentityService identityService,
    ILogger<CommandRepository<Company>> logger
)
    : CommandRepository<Company>(dbContext, identityService, logger),
        ICompanyCommandRepository
{
    public async Task<Company?> GetTrackedAsync(Guid id, CancellationToken ct = default)
        => await Table.FirstOrDefaultAsync(c => c.Id == id, ct);
}
