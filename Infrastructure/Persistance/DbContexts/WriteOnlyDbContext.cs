namespace Persistence.DbContexts;

/// <summary>Write-side context. Same model, used by command repositories.</summary>
public class WriteOnlyDbContext(
    DbContextOptions<ValuationDbContext> options,
    IdentityService identityService
)
    : ValuationDbContext(options, identityService);
