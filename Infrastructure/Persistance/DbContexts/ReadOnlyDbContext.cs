namespace Persistence.DbContexts;

/// <summary>Read-side context. Same model, used by query repositories.</summary>
public class ReadOnlyDbContext(
    DbContextOptions<ValuationDbContext> options,
    IdentityService identityService
)
    : ValuationDbContext(options, identityService);
