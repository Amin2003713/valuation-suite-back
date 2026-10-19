using System.Text;
using ApiFramework;
using Application;
using Domain.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Persistence;
using Persistence.Services;
using RequestHandlers;
using Swashbuckle.AspNetCore.SwaggerGen;
using Web.Api.Filters;
using Web.Api.Services;

namespace Web.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddWebApi(builder.Configuration);

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1",
                new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Valuation Suite API",
                    Version = "v1",
                    Description = "Backend API for Valuation Suite - IP Assessment, WIPO Diagnostics, Idea Assessment, Job Evaluation, and more"
                });

            c.OperationFilter<ExamplesOperationFilter>();
            c.SchemaFilter<ExamplesSchemaFilter>();
            c.IgnoreObsoleteActions();
            c.CustomSchemaIds(t => t.FullName);

            // JWT bearer support in Swagger UI
            c.AddSecurityDefinition("Bearer",
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter your JWT token (from /api/auth/login) below."
                });

            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        builder.Services.AddApplication();
        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddHandlers();

        // Tool reference tables, bands and thresholds for the math engines.
        // Defaults live in code; any "Tools:*" appsettings section can override them.
        builder.Services.Configure<Application.Tools.Options.PviOptions>(
            builder.Configuration.GetSection(Application.Tools.Options.PviOptions.SectionName));

        builder.Services.Configure<Application.Tools.Options.DigitalAiOptions>(
            builder.Configuration.GetSection(Application.Tools.Options.DigitalAiOptions.SectionName));

        builder.Services.Configure<Application.Tools.Options.MaturityOptions>(
            builder.Configuration.GetSection(Application.Tools.Options.MaturityOptions.SectionName));

        builder.Services.Configure<Application.Tools.Options.ExportReadinessOptions>(
            builder.Configuration.GetSection(Application.Tools.Options.ExportReadinessOptions.SectionName));

        builder.Services.Configure<Application.Tools.Options.RnpvOptions>(
            builder.Configuration.GetSection(Application.Tools.Options.RnpvOptions.SectionName));

        builder.Services.Configure<Application.Tools.Options.BinomialOptions>(
            builder.Configuration.GetSection(Application.Tools.Options.BinomialOptions.SectionName));

        // Supported languages (single config list; default + fallback = fa).
        builder.Services.Configure<Application.Common.LanguageOptions>(
            builder.Configuration.GetSection(Application.Common.LanguageOptions.SectionName));

        builder.Services.Configure<Application.Tools.Options.SvfOptions>(
            builder.Configuration.GetSection(Application.Tools.Options.SvfOptions.SectionName));

        builder.Services.Configure<Application.Tools.Options.KnowhowOptions>(
            builder.Configuration.GetSection(Application.Tools.Options.KnowhowOptions.SectionName));

        // Current-user accessor used by tool handlers (composition root wiring)
        builder.Services.AddScoped<Application.Interfaces.ICurrentUserAccessor, RequestHandlers.Tools.HttpCurrentUserAccessor>();

        // Request language (?lang > Accept-Language > default fa) + tool content localization.
        builder.Services.AddScoped<Application.Interfaces.ILanguageResolver, RequestHandlers.Tools.LanguageResolver>();
        builder.Services.AddScoped<Application.Tools.IToolContentResolver, Application.Tools.ToolContentResolver>();

        // ---- JWT auth ----
        builder.Services.AddScoped<TokenService>();
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured"))),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        builder.Services.AddAuthorization(Application.Admin.PermissionPolicies.AddPolicies);
        // ---- Admin bootstrap (roles + first admin from appsettings) ----
        builder.Services.AddScoped<Application.Admin.AdminBootstrapper>();

        // ---- Zarinpal payment gateway ----
        builder.Services.Configure<ZarinpalOptions>(builder.Configuration.GetSection("Zarinpal"));
        builder.Services.AddHttpClient<ZarinpalService>();

        var app = builder.Build();

        //if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Valuation Suite API v1");
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                c.DefaultModelsExpandDepth(3);
                c.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);
            });
        }

        app.UseHttpsRedirection();

        // Resolves the request language once (query > header > default) and stamps
        // Content-Language on every response; tool handlers read it via ILanguageResolver.
        app.UseMiddleware<RequestHandlers.Tools.LanguageMiddleware>();

        app.UseWebApi();

        // Seed the IP assessment (moved from the frontend's hard-coded questions)
        // and the tool form definitions (defaults + reference data for the pure-UI client).
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                // SQL Server (local/dev default): apply EF migrations.
                // SQLite (Docker profile): no portable migrations — generate the
                // schema from the model instead, then seeding runs identically.
                var db      = scope.ServiceProvider.GetRequiredService<Persistence.DbContexts.ValuationDbContext>();
                var markers = scope.ServiceProvider.GetRequiredService<Persistence.DependencyInjection.DbInitMarkers>();

                if (markers.Provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("Database:Provider=Sqlite — ensuring database created from model...");
                    await db.Database.EnsureCreatedAsync();
                    logger.LogInformation("SQLite database ready");
                }
                else
                {
                    logger.LogInformation("Applying EF Core migrations...");
                    await db.Database.MigrateAsync();
                    logger.LogInformation("Database migrations applied");
                }

                await scope.ServiceProvider.GetRequiredService<Application.Assessments.Seeding.IpAssessmentSeederRunner>()
                    .RunAsync();

                await Application.Tools.Seeding.ToolFormsSeeder.SeedAsync(
                    scope.ServiceProvider.GetRequiredService<Application.Tools.Seeding.IToolFormsDbContext>(),
                    logger);

                await Application.Tools.Seeding.ToolQuestionsSeeder.SeedAsync(
                    scope.ServiceProvider.GetRequiredService<Application.Tools.Seeding.IToolFormsDbContext>(),
                    logger);

                await scope.ServiceProvider.GetRequiredService<Application.Admin.AdminBootstrapper>().RunAsync();
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "Startup seeding failed");
            }
        }

        await app.RunAsync();
    }
}