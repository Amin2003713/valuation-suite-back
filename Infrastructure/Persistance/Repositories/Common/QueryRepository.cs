using Application.Interfaces.Base;

namespace Persistence.Repositories.Common;

public class QueryRepository<TEntity>(
    ReadOnlyDbContext dbContext,
    ILogger<QueryRepository<TEntity>> logger
)
    : RepositoryBase<TEntity, ReadOnlyDbContext>(dbContext, logger),
        IQueryRepository<TEntity>
    where TEntity : class, IEntity
{
    public virtual ValueTask<TEntity?> GetByIdAsync(CancellationToken cancellationToken, params object[] ids)
    {
        try
        {
            Logger.LogInformation("Fetching entity of type {EntityType}", typeof(TEntity).Name);
            return Entities.FindAsync(ids, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching entity of type {EntityType}", typeof(TEntity).Name);
            throw ValuationException.InternalServerError();
        }
    }

    public virtual TEntity? GetById(params object[] ids)
    {
        try
        {
            Logger.LogInformation("Fetching entity of type {EntityType}", typeof(TEntity).Name);
            return Entities.Find(ids);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching entity of type {EntityType}", typeof(TEntity).Name);
            return null;
        }
    }

    public async Task<TEntity?> GetSingleAsync(
        CancellationToken cancellationToken,
        Expression<Func<TEntity, bool>>? predicate = null)
    {
        try
        {
            Logger.LogInformation("Fetching single entity of type {EntityType} with predicate", typeof(TEntity).Name);
            return predicate is null
                ? await TableNoTracking.SingleOrDefaultAsync(cancellationToken)
                : await TableNoTracking.SingleOrDefaultAsync(predicate, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching single entity of type {EntityType}", typeof(TEntity).Name);
            return null;
        }
    }

    public async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return predicate is null
                ? await TableNoTracking.FirstOrDefaultAsync(cancellationToken)
                : await TableNoTracking.FirstOrDefaultAsync(predicate, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching entity of type {EntityType}", typeof(TEntity).Name);
            return null;
        }
    }

    public TEntity? GetSingle(Expression<Func<TEntity, bool>>? predicate = null)
    {
        try
        {
            return predicate is null
                ? TableNoTracking.SingleOrDefault()
                : TableNoTracking.SingleOrDefault(predicate);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching single entity of type {EntityType}", typeof(TEntity).Name);
            return null;
        }
    }

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => TableNoTracking.AnyAsync(predicate, cancellationToken);

    public bool Any(Expression<Func<TEntity, bool>> predicate)
        => TableNoTracking.Any(predicate);

    public async Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        => predicate is null
            ? await TableNoTracking.ToListAsync(cancellationToken)
            : await TableNoTracking.Where(predicate).ToListAsync(cancellationToken);

    public void LoadCollection<TProperty>(
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TProperty>>> collectionProperty)
        where TProperty : class
    {
        var collection = DbContext.Entry(entity).Collection(collectionProperty);
        if (!collection.IsLoaded)
            collection.Load();
    }

    public async Task LoadCollectionAsync<TProperty>(
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TProperty>>> collectionProperty,
        CancellationToken cancellationToken)
        where TProperty : class
    {
        var collection = DbContext.Entry(entity).Collection(collectionProperty);
        if (!collection.IsLoaded)
            await collection.LoadAsync(cancellationToken);
    }

    public void LoadReference<TProperty>(
        TEntity entity,
        Expression<Func<TEntity, TProperty>> referenceProperty)
        where TProperty : class
    {
        var reference = DbContext.Entry(entity).Reference(referenceProperty);
        if (!reference.IsLoaded)
            reference.Load();
    }

    public async Task LoadReferenceAsync<TProperty>(
        TEntity entity,
        Expression<Func<TEntity, TProperty>> referenceProperty,
        CancellationToken cancellationToken)
        where TProperty : class
    {
        var reference = DbContext.Entry(entity).Reference(referenceProperty);
        if (!reference.IsLoaded)
            await reference.LoadAsync(cancellationToken);
    }
}
