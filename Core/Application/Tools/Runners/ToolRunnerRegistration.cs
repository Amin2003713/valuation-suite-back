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
        services.AddSingleton<StartupValuationRunner>();
        services.AddSingleton<IToolRunner, StartupValuationRunner>(sp => sp.GetRequiredService<StartupValuationRunner>());
        services.AddSingleton<BrandValuationRunner>();
        services.AddSingleton<IToolRunner, BrandValuationRunner>(sp => sp.GetRequiredService<BrandValuationRunner>());
        services.AddSingleton<TrademarkValuationRunner>();
        services.AddSingleton<IToolRunner, TrademarkValuationRunner>(sp => sp.GetRequiredService<TrademarkValuationRunner>());
        services.AddSingleton<PatentValuationRunner>();                    // general-ip-valuation engine
        services.AddSingleton<IToolRunner, PatentValuationRunner>(sp => sp.GetRequiredService<PatentValuationRunner>());
        services.AddSingleton<IToolRunner, PharmaPatentValuationRunner>(); // pharma-ip-valuation (same engine)
        services.AddSingleton<IntangibleAssetsRunner>();
        services.AddSingleton<IToolRunner, IntangibleAssetsRunner>(sp => sp.GetRequiredService<IntangibleAssetsRunner>());
        services.AddSingleton<KnowhowValuationRunner>();
        services.AddSingleton<IToolRunner, KnowhowValuationRunner>(sp => sp.GetRequiredService<KnowhowValuationRunner>());
        services.AddSingleton<IToolRunner, IconScorecardRunner>();
        services.AddSingleton<IToolRunner, PatentSearchRunner>(); // patent-search — persists searches so adviser chat works

        services.AddSingleton<IToolRunnerResolver, ToolRunnerResolver>();
        return services;
    }
}
