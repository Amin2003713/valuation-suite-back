using Application.Assessments.Seeding;
using Application.Tools.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Persistence.DbContexts;

/// <summary>
///     Implements the Application-layer seeding contracts on top of the write-side context.
/// </summary>
public class SeedingDbContext(WriteOnlyDbContext writeDb) : ISeedingDbContext, IToolFormsDbContext
{
    public DbSet<TEntity> Set<TEntity>() where TEntity : class
        => writeDb.Set<TEntity>();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => writeDb.SaveChangesAsync(cancellationToken);
}
