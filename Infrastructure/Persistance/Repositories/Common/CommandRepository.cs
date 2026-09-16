using Application.Interfaces.Base;

namespace Persistence.Repositories.Common;

public class CommandRepository<TEntity>(
    WriteOnlyDbContext dbContext,
    IdentityService identityService,
    ILogger<CommandRepository<TEntity>> logger
)
    : RepositoryBase<TEntity, WriteOnlyDbContext>(dbContext, logger),
        ICommandRepository<TEntity>
    where TEntity : class, IEntity
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await DbContext.SaveChangesAsync(cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true)
    {
        try
        {
            Logger.LogInformation("Adding entity of type {EntityType}", typeof(TEntity).Name);
            await Entities.AddAsync(entity, cancellationToken);

            if (saveNow)
            {
                await DbContext.SaveChangesAsync(cancellationToken);
                DbContext.ChangeTracker.Clear();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding entity of type {EntityType}", typeof(TEntity).Name);
            throw ValuationException.InternalServerError();
        }
    }

    public void Add(TEntity entity, bool saveNow = true)
    {
        try
        {
            DbContext.ChangeTracker.Clear();
            Logger.LogInformation("Adding entity of type {EntityType}", typeof(TEntity).Name);
            Entities.Add(entity);

            if (saveNow)
            {
                DbContext.SaveChanges();
                Logger.LogInformation("Entity of type {EntityType} added successfully", typeof(TEntity).Name);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding entity of type {EntityType}", typeof(TEntity).Name);
            throw ValuationException.InternalServerError();
        }
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true)
    {
        try
        {
            await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
            await Entities.AddRangeAsync(entities, cancellationToken);

            if (saveNow)
                await DbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding range of entities of type {EntityType}", typeof(TEntity).Name);
            throw ValuationException.InternalServerError();
        }
    }

    public void AddRange(IEnumerable<TEntity> entities, bool saveNow = true)
    {
        try
        {
            DbContext.ChangeTracker.Clear();
            Logger.LogInformation("Adding range of entities of type {EntityType}", typeof(TEntity).Name);
            Entities.AddRange(entities);

            if (saveNow)
            {
                DbContext.SaveChanges();
                Logger.LogInformation("Entities of type {EntityType} added successfully", typeof(TEntity).Name);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding range of entities of type {EntityType}", typeof(TEntity).Name);
            throw ValuationException.InternalServerError();
        }
    }

    public async Task AddBulkAsync(
        IEnumerable<TEntity> entities,
        Expression<Func<TEntity, bool>>? insertCondition = null,
        CancellationToken cancellationToken = default,
        bool saveNow = true)
    {
        var list = entities.ToList();

        if (insertCondition is not null)
        {
            var condition = insertCondition.Compile();
            list = list.Where(condition).ToList();
        }

        if (list.Count == 0)
            return;

        ApplyBulkInsertRules(list, DateTime.UtcNow);

        Entities.AddRange(list);

        if (saveNow && DbContext.ChangeTracker.HasChanges())
            await DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true)
    {
        try
        {
            Logger.LogInformation("Updating entity of type {EntityType}", typeof(TEntity).Name);
            Entities.Update(entity);

            if (saveNow)
            {
                await DbContext.SaveChangesAsync(cancellationToken);
                Logger.LogInformation("Entity of type {EntityType} updated successfully", typeof(TEntity).Name);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating entity of type {EntityType}", typeof(TEntity).Name);
            throw ValuationException.InternalServerError();
        }
    }

    public void Update(TEntity entity, bool saveNow = true)
    {
        try
        {
            DbContext.ChangeTracker.Clear();
            Logger.LogInformation("Updating entity of type {EntityType}", typeof(TEntity).Name);
            Entities.Update(entity);

            if (saveNow)
            {
                DbContext.SaveChanges();
                Logger.LogInformation("Entity of type {EntityType} updated successfully", typeof(TEntity).Name);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating entity of type {EntityType}", typeof(TEntity).Name);
            throw ValuationException.InternalServerError();
        }
    }

    public async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true)
    {
        try
        {
            Logger.LogInformation("Updating range of entities of type {EntityType}", typeof(TEntity).Name);
            Entities.UpdateRange(entities);

            if (saveNow)
                await DbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating range of entities of type {EntityType}", typeof(TEntity).Name);
            throw ValuationException.InternalServerError();
        }
    }

    public void UpdateRange(IEnumerable<TEntity> entities, bool saveNow = true)
    {
        try
        {
            Logger.LogInformation("Updating range of entities of type {EntityType}", typeof(TEntity).Name);
            Entities.UpdateRange(entities);

            if (saveNow)
                DbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating range of entities of type {EntityType}", typeof(TEntity).Name);
            throw ValuationException.InternalServerError();
        }
    }

    public async Task UpdateBulkAsync(
        IEnumerable<TEntity> entities,
        Expression<Func<TEntity, bool>>? updateCondition = null,
        CancellationToken cancellationToken = default,
        bool saveNow = true)
    {
        var list = entities.ToList();

        if (updateCondition is not null)
        {
            var condition = updateCondition.Compile();
            list = list.Where(condition).ToList();
        }

        if (list.Count == 0)
            return;

        ApplyBulkUpdateRules(list, DateTime.UtcNow);
        Entities.UpdateRange(list);

        if (saveNow && DbContext.ChangeTracker.HasChanges())
            await DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken, bool saveNow = true)
    {
        try
        {
            Logger.LogInformation("Deleting entity of type {EntityType}", typeof(TEntity).Name);
            Entities.Remove(entity);

            if (saveNow)
                await DbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting entity of type {EntityType}", typeof(TEntity).Name);
            throw ValuationException.InternalServerError();
        }
    }

    public async Task DeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken,
        bool saveNow = true)
    {
        var item = await Entities.FirstOrDefaultAsync(predicate, cancellationToken);
        if (item == null)
            throw ValuationException.NotFound();

        await DeleteAsync(item, cancellationToken, saveNow);
    }

    public void Delete(TEntity entity, bool saveNow = true)
    {
        try
        {
            Logger.LogInformation("Deleting entity of type {EntityType}", typeof(TEntity).Name);
            Entities.Remove(entity);

            if (saveNow)
                DbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting entity of type {EntityType}", typeof(TEntity).Name);
            throw ValuationException.InternalServerError();
        }
    }

    public async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken, bool saveNow = true)
    {
        try
        {
            Logger.LogInformation("Deleting range of entities of type {EntityType}", typeof(TEntity).Name);
            Entities.RemoveRange(entities);

            if (saveNow)
                await DbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting range of entities of type {EntityType}", typeof(TEntity).Name);
            throw ValuationException.InternalServerError();
        }
    }

    public void DeleteRange(IEnumerable<TEntity> entities, bool saveNow = true)
    {
        try
        {
            Logger.LogInformation("Deleting range of entities of type {EntityType}", typeof(TEntity).Name);
            Entities.RemoveRange(entities);

            if (saveNow)
                DbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting range of entities of type {EntityType}", typeof(TEntity).Name);
            throw ValuationException.InternalServerError();
        }
    }

    public async Task DeleteBulkAsync(
        IEnumerable<TEntity> entities,
        Expression<Func<TEntity, bool>>? deleteCondition = null,
        CancellationToken cancellationToken = default,
        bool saveNow = true)
    {
        var list = entities.ToList();

        if (deleteCondition is not null)
        {
            var condition = deleteCondition.Compile();
            list = list.Where(condition).ToList();
        }

        if (list.Count == 0)
            return;

        ApplyBulkSoftDeleteRules(list, DateTime.UtcNow);
        Entities.UpdateRange(list);

        if (saveNow && DbContext.ChangeTracker.HasChanges())
            await DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HardDeleteBulkAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var list = entities.ToList();
        if (list.Count == 0)
            return;

        Entities.RemoveRange(list);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public void Attach(TEntity entity)
    {
        if (DbContext.Entry(entity).State == EntityState.Detached)
            Entities.Attach(entity);
    }

    public void Detach(TEntity entity)
        => DbContext.Entry(entity).State = EntityState.Detached;

    public void ClearChangeTracker()
        => DbContext.ChangeTracker.Clear();

    private void ApplyBulkInsertRules(IEnumerable<TEntity> entities, DateTime now)
    {
        var currentUserId = identityService?.GetUserId();

        foreach (var entity in entities)
        {
            if (entity.CreatedAt == default)
                entity.CreatedAt = now;

            if (currentUserId is not null)
                entity.CreatedBy ??= currentUserId;

            entity.IsActive = true;
            entity.DeletedAt = null;
            entity.DeletedBy = null;
        }
    }

    private void ApplyBulkUpdateRules(IEnumerable<TEntity> entities, DateTime now)
    {
        var currentUserId = identityService?.GetUserId();

        foreach (var entity in entities)
        {
            entity.ModifiedAt = now;

            // Do not use ??= here: ModifiedBy should represent the latest modifier.
            if (currentUserId is not null)
                entity.ModifiedBy = currentUserId;
        }
    }

    private void ApplyBulkSoftDeleteRules(IEnumerable<TEntity> entities, DateTime now)
    {
        var currentUserId = identityService?.GetUserId();

        foreach (var entity in entities)
        {
            entity.DeletedAt = now;
            entity.IsActive = false;
            entity.ModifiedAt = now;

            if (currentUserId is not null)
            {
                entity.DeletedBy = currentUserId;
                entity.ModifiedBy = currentUserId;
            }
        }
    }
}
