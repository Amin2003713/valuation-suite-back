using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Persistence.DbContexts;

/// <summary>
///     Base identity-aware application DbContext that:
///     <list type="bullet">
///         <item>Discovers and registers all entities from the Domain assembly (the only source of the SQL model).</item>
///         <item>Provides ASP.NET Core Identity stores for <see cref="ApplicationUser"/> (IdentityUser-backed).</item>
///         <item>Applies IEntity auditing rules (CreatedBy/ModifiedBy/DeletedBy, soft delete) using IdentityService.</item>
///         <item>Applies configuration classes from the persistence assembly and the concrete DbContext assembly.</item>
///     </list>
/// </summary>
public class ValuationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    /// <summary>Assembly that contains the persistent model (entities) for this context.</summary>
    private static readonly Assembly ModelAssembly = typeof(ApplicationUser).Assembly;

    /// <summary>Service providing information about the current user, for auditing.</summary>
    private readonly IdentityService? _identityService;

public ValuationDbContext(
        DbContextOptions<ValuationDbContext> options,
        IdentityService? identityService = null)
        : base(options)
        => _identityService = identityService;

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyEntityRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyEntityRules();
        return base.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. Identity mappings first (AspNetUsers/AspNetRoles/...), then domain entities.
        base.OnModelCreating(modelBuilder);

        // 2. Core/persistence entities: only the Domain assembly is scanned for entities.
        modelBuilder.RegisterAllEntities(ModelAssembly);
        modelBuilder.ApplyConfigurationsFromAssembly(ModelAssembly);

        // 3. Configurations from the concrete DbContext (calling) assembly.
        var contextAssembly = GetType().Assembly;
        if (contextAssembly != ModelAssembly && contextAssembly != typeof(ValuationDbContext).Assembly)
            modelBuilder.ApplyConfigurationsFromAssembly(contextAssembly);

        // 4. Configurations from the persistence assembly + conventions and global filters.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ValuationDbContext).Assembly);
        modelBuilder.AddPluralizingTableNameConvention();
        modelBuilder.AddDecimalConvention();
        modelBuilder.AddGlobalIsActiveFilter();
    }

    /// <summary>Applies auditing and soft-delete rules to all tracked IEntity instances.</summary>
    private void ApplyEntityRules()
    {
        if (_identityService is null) return;

        var entries = ChangeTracker.Entries<IEntity>();
        var currentUserId = _identityService.GetUserId();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
            switch (entry.State)
            {
                case EntityState.Added:
                    // Set CreatedBy only if still null to avoid overriding custom values.
                    entry.Entity.CreatedBy ??= currentUserId;
                    entry.Entity.IsActive = true;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedBy ??= currentUserId;
                    break;

                case EntityState.Deleted:
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy ??= currentUserId;
                    entry.Entity.IsActive = false;
                    entry.State = EntityState.Modified; // soft delete
                    break;
            }
    }
}
