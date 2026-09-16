namespace Persistence.DbContexts;

public interface IAppDbContext
{
    DbSet<T> Set<T>()
        where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellation);
}
