using Common.General;
using Domain.Payments;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Persistence.Repositories.Assessments;
using Persistence.Repositories.Attempts;
using Persistence.Repositories.Companies;
using Persistence.Repositories.Results;
using Persistence.Repositories.Users;

namespace Persistence;

public static class DependencyInjection
{
    /// <summary>
    ///     Marker option for the SQLite (Docker) profile: the schema is created from the
    ///     EF model (EnsureCreated) instead of migrations — SQLite migrations are not set
    ///     up for this project and SQL Server migration SQL is not portable.
    /// </summary>
    public sealed class DbInitMarkers
    {
        public bool UseEnsureCreated { get; set; }
        public string Provider { get; set; } = "SqlServer";
    }

    private static void ConfigureProvider(
        DbContextOptionsBuilder<ValuationDbContext> builder, bool isSqlite, string connectionString)
    {
        if (isSqlite)
        {
            builder.UseSqlite(connectionString);
        }
        else
        {
            builder.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(ValuationDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            });
        }
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = (configuration["Database:Provider"] ?? "SqlServer").Trim();
        var isSqlite = provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);

        // SQLite (Docker profile): file path from Database:SqlitePath, or a Docker-friendly default.
        var sqlitePath = configuration["Database:SqlitePath"] ?? "/app/data/valuationsuite.db";

        var connectionString = isSqlite
            ? $"Data Source={sqlitePath}"
            : configuration.GetConnectionString("AssessmentDb")
                ?? ApplicationConstant.AppOptions.ConnectionString;

        ApplicationConstant.AppOptions.ConnectionString = connectionString;

        // Program.cs branches on this to EnsureCreated (Sqlite) vs Migrate (SqlServer).
        services.AddSingleton(new DbInitMarkers { UseEnsureCreated = isSqlite, Provider = provider });

        services.AddHttpContextAccessor();
        services.AddScoped<IdentityService>();

        // Identity requires relational access; SQLite is relational so EF stores work as-is.
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
            .AddDefaultTokenProviders()
            .AddSignInManager();

        // ---- Contexts (provider chosen once, applied to all three contexts) ----
        void Configure(DbContextOptionsBuilder b) => ConfigureProvider((DbContextOptionsBuilder<ValuationDbContext>)b, isSqlite, connectionString);

        services.AddDbContext<ValuationDbContext>((_, options) => Configure(options));        services.AddScoped<WriteOnlyDbContext>(provider =>
        {
            var options = new DbContextOptionsBuilder<ValuationDbContext>();
            Configure(options);
            var identity = provider.GetRequiredService<IdentityService>();
            return new WriteOnlyDbContext(options.Options, identity);
        });
        services.AddScoped<ReadOnlyDbContext>(provider =>
        {
            var options = new DbContextOptionsBuilder<ValuationDbContext>();
            Configure(options);
            var identity = provider.GetRequiredService<IdentityService>();
            return new ReadOnlyDbContext(options.Options, identity);
        });

        // ---- Generic repositories ----
        services.AddScoped(typeof(ICommandRepository<>), typeof(Repositories.Common.CommandRepository<>));
        services.AddScoped(typeof(IQueryRepository<>), typeof(Repositories.Common.QueryRepository<>));

        // ---- Aggregate-specific repositories (unchanged wiring) ----
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
        services.AddScoped<ICommandRepository<Domain.Tools.ToolSubmission>, Repositories.Common.CommandRepository<Domain.Tools.ToolSubmission>>();
        services.AddScoped<IQueryRepository<Domain.Tools.ToolSubmission>, Repositories.Common.QueryRepository<Domain.Tools.ToolSubmission>>();
        services.AddScoped<ICommandRepository<Domain.Tools.ToolForm>, Repositories.Common.CommandRepository<Domain.Tools.ToolForm>>();
        services.AddScoped<IQueryRepository<Domain.Tools.ToolForm>, Repositories.Common.QueryRepository<Domain.Tools.ToolForm>>();
        services.AddScoped<Application.Interfaces.IAdminQueryRepository, Repositories.Admin.AdminQueryRepository>();
        services.AddScoped<Application.Assessments.Seeding.ISeedingDbContext, DbContexts.SeedingDbContext>();
        services.AddScoped<Application.Tools.Seeding.IToolFormsDbContext, DbContexts.SeedingDbContext>();

        return services;
    }
}
