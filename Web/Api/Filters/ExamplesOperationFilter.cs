using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Web.Api.Filters;

public class ExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody == null) return;

        var routeTemplate = context.ApiDescription.RelativePath ?? "";
        var isPost = IsPost(context);

        if (routeTemplate.StartsWith("api/assessments"))
        {
            if (routeTemplate == "api/assessments" || routeTemplate == "api/assessments/{id}/versions"
                || routeTemplate == "api/assessments/{id}/publish" || routeTemplate == "api/assessments/{id}/archive")
            {
                SetExample(operation, "application/json", new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["name"] = new Microsoft.OpenApi.Any.OpenApiString("Example Assessment"),
                    ["code"] = new Microsoft.OpenApi.Any.OpenApiString("EX-001"),
                    ["description"] = new Microsoft.OpenApi.Any.OpenApiString("Example description"),
                    ["companyId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString())
                });
            }
        }

        if (routeTemplate.StartsWith("api/assessment-attempts") && isPost)
        {
            SetExample(operation, "application/json", new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["versionId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
                ["userId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
                ["companyId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
                ["totalSteps"] = new Microsoft.OpenApi.Any.OpenApiInteger(5)
            });
        }

        if (routeTemplate.StartsWith("api/steps") && isPost)
        {
            SetExample(operation, "application/json", new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["versionId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
                ["title"] = new Microsoft.OpenApi.Any.OpenApiString("New Step"),
                ["order"] = new Microsoft.OpenApi.Any.OpenApiInteger(1)
            });
        }

        if (routeTemplate.StartsWith("api/questions") && isPost)
        {
            SetExample(operation, "application/json", new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["text"] = new Microsoft.OpenApi.Any.OpenApiString("Example question?"),
                ["type"] = new Microsoft.OpenApi.Any.OpenApiString("SingleChoice"),
                ["order"] = new Microsoft.OpenApi.Any.OpenApiInteger(1)
            });
        }

        if (routeTemplate == "api/results/calculate" && isPost)
        {
            SetExample(operation, "application/json", new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["attemptId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString())
            });
        }

        if (routeTemplate.StartsWith("api/admin/companies") && isPost)
        {
            SetExample(operation, "application/json", new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["name"] = new Microsoft.OpenApi.Any.OpenApiString("Example Company"),
                ["slug"] = new Microsoft.OpenApi.Any.OpenApiString("example-company")
            });
        }
    }

    private static bool IsPost(OperationFilterContext context)
    {
        try
        {
            var actionDescriptor = context.ApiDescription.ActionDescriptor;
            var httpMethodProp = actionDescriptor.GetType().GetProperty("HttpMethod", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            return httpMethodProp?.GetValue(actionDescriptor)?.ToString() == "POST";
        }
        catch
        {
            return false;
        }
    }

        private static void SetExample(OpenApiOperation operation, string contentType, Microsoft.OpenApi.Any.IOpenApiAny example)
    {
        if (operation.RequestBody?.Content == null || !operation.RequestBody.Content.ContainsKey(contentType)) return;
        operation.RequestBody.Content[contentType].Example = example;
    }
}
