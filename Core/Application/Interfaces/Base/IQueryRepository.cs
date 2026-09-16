using System.Linq.Expressions;
using Common.Base;
using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces.Base;

public interface IQueryRepository<TEntity>
    where TEntity : class, IEntity
{
    DbSet<TEntity> Entities { get; }
    IQueryable<TEntity> Table { get; }
    IQueryable<TEntity> TableNoTracking { get; }

    ValueTask<TEntity?> GetByIdAsync(CancellationToken cancellationToken, params object[] ids);
    TEntity? GetById(params object[] ids);

    Task<TEntity?> GetSingleAsync(
        CancellationToken cancellationToken,
        Expression<Func<TEntity, bool>>? predicate = null);

    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    TEntity? GetSingle(Expression<Func<TEntity, bool>>? predicate = null);

    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    bool Any(Expression<Func<TEntity, bool>> predicate);

    Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    void LoadCollection<TProperty>(
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TProperty>>> collectionProperty)
        where TProperty : class;

    Task LoadCollectionAsync<TProperty>(
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TProperty>>> collectionProperty,
        CancellationToken cancellationToken)
        where TProperty : class;

    void LoadReference<TProperty>(
        TEntity entity,
        Expression<Func<TEntity, TProperty>> referenceProperty)
        where TProperty : class;

    Task LoadReferenceAsync<TProperty>(
        TEntity entity,
        Expression<Func<TEntity, TProperty>> referenceProperty,
        CancellationToken cancellationToken)
        where TProperty : class;
}
