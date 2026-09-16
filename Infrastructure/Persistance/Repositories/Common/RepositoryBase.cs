namespace Persistence.Repositories.Common;

public abstract class RepositoryBase<TEntity, TDbContext>(
    TDbContext dbContext,
    ILogger logger
)
    where TEntity : class, IEntity
    where TDbContext : DbContext
{
    protected readonly TDbContext DbContext = dbContext;
    protected readonly ILogger Logger = logger;

    public DbSet<TEntity> Entities => DbContext.Set<TEntity>();

    public virtual IQueryable<TEntity> Table => Entities;
    public virtual IQueryable<TEntity> TableNoTracking => Table.AsNoTracking();
}
