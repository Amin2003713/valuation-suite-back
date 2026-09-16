using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Assessments.Seeding;

/// <summary>
///     Runs the IP assessment seed at startup. Resolves <see cref="ISeedingDbContext"/>
///     which is implemented and registered by the Persistence layer.
/// </summary>
public class IpAssessmentSeederRunner(IServiceProvider provider)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var db = provider.GetRequiredService<ISeedingDbContext>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("IpAssessmentSeeder");
        await IpAssessmentSeeder.SeedAsync(db, logger, ct);
    }
}
