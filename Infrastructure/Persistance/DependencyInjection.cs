using Common.General;
using Domain.Payments;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Persistence.Repositories.Assessments;
using Persistence.Repositories.Attempts;
using Persistence.Repositories.Companies;
using Persistence.Repositories.Results;
using Persistence.Repositories.Users;

namespace Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("AssessmentDb")
            ?? ApplicationConstant.AppOptions.ConnectionString;

        ApplicationConstant.AppOptions.ConnectionString = connectionString;

        services.AddHttpContextAccessor();
        services.AddScoped<IdentityService>();

        // ---- ASP.NET Core Identity (IdentityUser-backed users) ----
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireDigit = false;
                options.Lockout.AllowedForNewUsers = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ValuationDbContext>()
            .AddSignInManager();

        // ---- Write side ----
        services.AddScoped<WriteOnlyDbContext>(provider =>
        {
            var options = new DbContextOptionsBuilder<ValuationDbContext>()
                .UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(ValuationDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                })
                .Options;

            var identity = provider.GetRequiredService<IdentityService>();
            return new WriteOnlyDbContext(options, identity);
        });

        // ---- Read side ----
        services.AddScoped<ReadOnlyDbContext>(provider =>
        {
            var options = new DbContextOptionsBuilder<ValuationDbContext>()
                .UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(ValuationDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                })
                .Options;

            var identity = provider.GetRequiredService<IdentityService>();
            return new ReadOnlyDbContext(options, identity);
        });

        // ---- Generic repositories ----
        services.AddScoped(typeof(ICommandRepository<>), typeof(Repositories.Common.CommandRepository<>));
        services.AddScoped(typeof(IQueryRepository<>), typeof(Repositories.Common.QueryRepository<>));

        // ---- Aggregate-specific repositories ----
        services.AddScoped<IAssessmentCommandRepository, AssessmentCommandRepository>();
        services.AddScoped<IAssessmentQueryRepository, AssessmentQueryRepository>();
        services.AddScoped<IAssessmentVersionCommandRepository, AssessmentVersionCommandRepository>();
        services.AddScoped<IAssessmentVersionQueryRepository, AssessmentVersionQueryRepository>();
        services.AddScoped<IAssessmentAttemptCommandRepository, AssessmentAttemptCommandRepository>();
        services.AddScoped<IAssessmentAttemptQueryRepository, AssessmentAttemptQueryRepository>();
        services.AddScoped<IAnswerCommandRepository, AnswerCommandRepository>();
        services.AddScoped<IAnswerQueryRepository, AnswerQueryRepository>();
        services.AddScoped<IAssessmentResultCommandRepository, AssessmentResultCommandRepository>();
        services.AddScoped<IAssessmentResultQueryRepository, AssessmentResultQueryRepository>();
        services.AddScoped<ICompanyCommandRepository, CompanyCommandRepository>();
        services.AddScoped<ICompanyQueryRepository, CompanyQueryRepository>();
        services.AddScoped<IUserCommandRepository, UserCommandRepository>();
        services.AddScoped<IUserQueryRepository, UserQueryRepository>();
        services.AddScoped<ICommandRepository<Payment>, Repositories.Common.CommandRepository<Payment>>();
        services.AddScoped<IQueryRepository<Payment>, Repositories.Common.QueryRepository<Payment>>();
        services.AddScoped<Application.Assessments.Seeding.ISeedingDbContext, DbContexts.SeedingDbContext>();

        return services;
    }
}
