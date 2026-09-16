using System.Linq.Expressions;
using Common.Base;
using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces.Base;

public interface ICommandRepository<TEntity>
    where TEntity : class, IEntity
{
    DbSet<TEntity> Entities { get; }
    IQueryable<TEntity> Table { get; }
    IQueryable<TEntity> TableNoTracking { get; }

    /// <summary>Persists all tracked changes of the write-side context.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true);
    void Add(TEntity entity, bool saveNow = true);

    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);
    void AddRange(IEnumerable<TEntity> entities, bool saveNow = true);

    Task AddBulkAsync(
        IEnumerable<TEntity> entities,
        Expression<Func<TEntity, bool>>? insertCondition = null,
        CancellationToken cancellationToken = default,
        bool saveNow = true);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true);
    void Update(TEntity entity, bool saveNow = true);

    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);
    void UpdateRange(IEnumerable<TEntity> entities, bool saveNow = true);

    Task UpdateBulkAsync(
        IEnumerable<TEntity> entities,
        Expression<Func<TEntity, bool>>? updateCondition = null,
        CancellationToken cancellationToken = default,
        bool saveNow = true);

    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true);
    Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken, bool saveNow = true);
    void Delete(TEntity entity, bool saveNow = true);

    Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true);
    void DeleteRange(IEnumerable<TEntity> entities, bool saveNow = true);

    Task DeleteBulkAsync(
        IEnumerable<TEntity> entities,
        Expression<Func<TEntity, bool>>? deleteCondition = null,
        CancellationToken cancellationToken = default,
        bool saveNow = true);

    Task HardDeleteBulkAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    void Attach(TEntity entity);
    void Detach(TEntity entity);
    void ClearChangeTracker();
}
