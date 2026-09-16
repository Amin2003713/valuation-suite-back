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

        services.AddCors(options => options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

        return services;
    }

    public static WebApplication UseWebApi(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseCors();

        app.MapControllers();
        return app;
    }
}
