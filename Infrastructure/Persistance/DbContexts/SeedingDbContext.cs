using Application.Assessments.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Persistence.DbContexts;

/// <summary>
///     Implements the Application-layer seeding contract on top of the write-side context.
/// </summary>
public class SeedingDbContext(WriteOnlyDbContext writeDb) : ISeedingDbContext
{
    public DbSet<TEntity> Set<TEntity>() where TEntity : class
        => writeDb.Set<TEntity>();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => writeDb.SaveChangesAsync(cancellationToken);
}
