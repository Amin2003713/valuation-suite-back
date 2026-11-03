using ApiFramework.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiFramework;

public static class DependencyInjection
{
    public static IServiceCollection AddWebApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ApiExceptionFilter>();
            options.Filters.Add<Attributes.ValidateModelStateAttribute>();
        });

        services.AddProblemDetails();
        services.AddEndpointsApiExplorer();

        /* JSON responses here are large (tool forms, submissions, benchmarks)
         * and compress very well. Brotli/Gzip are the default providers; the
         * only change is enabling it over HTTPS too, which the framework
         * switches off by default for the BREACH-era rationale. These payloads
         * contain no secrets tied to a session cookie (auth is a bearer
         * header), so the trade-off is safe. */
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes
                .Concat(new[] { "application/json", "application/problem+json" });
        });

        services.AddCors(options => options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

        return services;
    }

    public static WebApplication UseWebApi(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseResponseCompression();

        /* CORS must be registered BEFORE authentication. The JWT middleware
         * short-circuits 401/403 responses; anything the auth middleware emits
         * never flows back through middleware registered after it. With
         * UseCors later in the pipeline those responses left the server
         * without Access-Control-Allow-Origin, the browser blocked every
         * unauthenticated tool request with a CORS error, and the UI was
         * permanently pinned on the “computing on server…” state. */
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Simple liveness/readiness probe for Docker / orchestrators.
        // Anonymous on purpose: probes have no token, and the fallback policy
        // would otherwise require authentication for every endpoint.
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
            .AllowAnonymous()
            .WithTags("health")
            .ExcludeFromDescription();

        return app;
    }
}
