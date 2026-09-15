using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Infrastructure.Persistence;

namespace Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AssessmentDb")
            ?? "Server=localhost;Database=ValuationSuite;Trusted_Connection=True;TrustServerCertificate=True;";

        services.AddDbContext<AssessmentDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(AssessmentDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            }));

        return services;
    }
}
