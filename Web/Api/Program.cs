using ApiFramework;
using Persistence;
using Persistence.Services;
using RequestHandlers;
using Swashbuckle.AspNetCore.SwaggerGen;
using Web.Api.Filters;

namespace Web.Api;

public class Program
{
    public static void Main(string[] args)
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
        });

        builder.Services.AddApplication();
        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddHandlers();

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
        app.UseWebApi();

        app.Run();
    }
}
