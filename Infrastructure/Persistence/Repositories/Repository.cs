using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Users;

namespace Infrastructure.Persistence;

public interface IAsyncRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class Repository<T> : IAsyncRepository<T> where T : class
{
    protected readonly AssessmentDbContext _db;
    protected readonly DbSet<T> _dbSet;

    public Repository(AssessmentDbContext db)
    {
        _db = db;
        _dbSet = db.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.FindAsync(new object?[] { id }, ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await _dbSet.AddAsync(entity, ct);

    public virtual async Task UpdateAsync(T entity, CancellationToken ct = default)
        => _dbSet.Update(entity);

    public virtual async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        if (entity != null)
            _dbSet.Remove(entity);
    }
}
