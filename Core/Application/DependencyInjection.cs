using Application.Assessments;
using Application.Assessments.Seeding;
using Application.Tools;
using Application.Tools.Runners;
using Application.Tools.Seeding;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(applicationAssembly));

        services.AddValidatorsFromAssembly(applicationAssembly);

        services.AddSingleton<IQuestionEngine, QuestionEngine>();
        services.AddSingleton<IMathEngine, MathEngine>();

        services.AddScoped<IpAssessmentSeederRunner>();

        // Tools: math engines (one runner per tool) + resolver
        services.AddToolRunners();

        // Entitlements (pro / per-tool grants / pick-credits)
        services.AddScoped<Application.Tools.IEntitlementService, Application.Tools.EntitlementService>();

        return services;
    }
}
