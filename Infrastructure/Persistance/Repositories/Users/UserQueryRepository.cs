using Domain.Users;

namespace Persistence.Repositories.Users;

public class UserQueryRepository(
    ReadOnlyDbContext dbContext,
    ILogger<QueryRepository<ApplicationUser>> logger
)
    : QueryRepository<ApplicationUser>(dbContext, logger),
        IUserQueryRepository
{
    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await TableNoTracking
            .FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<ApplicationUser?> GetWithCompanyAsync(Guid id, CancellationToken ct = default)
        => await TableNoTracking
            .Include(u => u.Company)
            .ThenInclude(c => c!.Assessments)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
}
