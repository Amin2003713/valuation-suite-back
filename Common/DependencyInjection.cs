namespace Common;

public static class DependencyInjection
{
    public static IServiceCollection AddCommon(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Behaviours.ValidationBehavior<,>));

        return services;
    }
}
