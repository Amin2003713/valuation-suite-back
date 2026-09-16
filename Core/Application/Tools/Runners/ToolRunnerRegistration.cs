using Application.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tools.Runners;

/// <summary>
///     The pharma variant shares the exact same engine as the general patent valuation —
///     only the default inputs differ (handled via form defaults served by the backend).
///     This alias makes the engine resolvable under the pharma tool code.
/// </summary>
public sealed class PharmaPatentValuationRunner(PatentValuationRunner inner) : IToolRunner
{
    public string ToolCode => "PHARMA-IP";

    public ToolRunOutcome Run(JsonElement input) => inner.Run(input);
}

public static class ToolRunnerRegistration
{
    /// <summary>Registers one stateless math engine per tool (singletons — they hold no state).</summary>
    public static IServiceCollection AddToolRunners(this IServiceCollection services)
    {
        // Question-based assessments
        services.AddSingleton<IToolRunner, IdeaAssessmentRunner>();
        services.AddSingleton<IToolRunner, InnovationReadinessRunner>();
        services.AddSingleton<IToolRunner, IpAuditRunner>();
        services.AddSingleton<IToolRunner, JobEvaluationRunner>();
        services.AddSingleton<IToolRunner, WipoDiagnosticsRunner>();
        services.AddSingleton<IToolRunner, IpscoreRunner>();

        // Calculators
        services.AddSingleton<IToolRunner, StartupValuationRunner>();
        services.AddSingleton<IToolRunner, BrandValuationRunner>();
        services.AddSingleton<IToolRunner, TrademarkValuationRunner>();
        services.AddSingleton<IToolRunner, PatentValuationRunner>();       // general-ip-valuation
        services.AddSingleton<IToolRunner, PharmaPatentValuationRunner>(); // pharma-ip-valuation (same engine)
        services.AddSingleton<IToolRunner, IntangibleAssetsRunner>();
        services.AddSingleton<IToolRunner, KnowhowValuationRunner>();

        services.AddSingleton<IToolRunnerResolver, ToolRunnerResolver>();
        return services;
    }
}
