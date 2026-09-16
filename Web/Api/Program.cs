using System.Text;
using ApiFramework;
using Application;
using Domain.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
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
            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
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
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
                            ?? throw new InvalidOperationException("Jwt:Key is not configured"))),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });
        builder.Services.AddAuthorization();

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
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseWebApi();

        // Seed the IP assessment (moved from the frontend's hard-coded questions).
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                await scope.ServiceProvider.GetRequiredService<Application.Assessments.Seeding.IpAssessmentSeederRunner>()
                    .RunAsync();
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "IP assessment seeding failed");
            }
        }

        app.Run();
    }
}
