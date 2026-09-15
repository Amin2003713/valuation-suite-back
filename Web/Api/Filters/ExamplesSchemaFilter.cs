using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Web.Api.Filters;

public class ExamplesSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null) return;

        var type = context.Type;

        if (type == typeof(Application.Assessments.Commands.CreateAssessmentCommand))
        {
            schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["name"] = new Microsoft.OpenApi.Any.OpenApiString("IP Assessment Tool"),
                ["code"] = new Microsoft.OpenApi.Any.OpenApiString("IP-ASSESS-001"),
                ["description"] = new Microsoft.OpenApi.Any.OpenApiString("Assessment for IP evaluation"),
                ["companyId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString())
            };
        }
        else if (type == typeof(Application.Assessments.Commands.CreateVersionCommand))
        {
            schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["assessmentId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
                ["title"] = new Microsoft.OpenApi.Any.OpenApiString("Draft Version 1")
            };
        }
        else if (type == typeof(Application.Assessments.Commands.AddStepCommand))
        {
            schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["versionId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
                ["title"] = new Microsoft.OpenApi.Any.OpenApiString("Information Gathering"),
                ["description"] = new Microsoft.OpenApi.Any.OpenApiString("Step 1 description"),
                ["order"] = new Microsoft.OpenApi.Any.OpenApiInteger(1)
            };
        }
        else if (type == typeof(Application.Assessments.Commands.AddQuestionCommand))
        {
            schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["stepId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
                ["text"] = new Microsoft.OpenApi.Any.OpenApiString("What is your IP?"),
                ["type"] = new Microsoft.OpenApi.Any.OpenApiString("SingleChoice"),
                ["order"] = new Microsoft.OpenApi.Any.OpenApiInteger(1),
                ["isRequired"] = new Microsoft.OpenApi.Any.OpenApiBoolean(true)
            };
        }
        else if (type == typeof(Application.Assessments.Commands.SetQuestionOptionsCommand))
        {
            schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["questionId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
                ["options"] = new Microsoft.OpenApi.Any.OpenApiArray
                {
                    new Microsoft.OpenApi.Any.OpenApiObject
                    {
                        ["label"] = new Microsoft.OpenApi.Any.OpenApiString("Yes"),
                        ["value"] = new Microsoft.OpenApi.Any.OpenApiString("yes"),
                        ["isCorrect"] = new Microsoft.OpenApi.Any.OpenApiBoolean(true),
                        ["score"] = new Microsoft.OpenApi.Any.OpenApiFloat(100.0f)
                    },
                    new Microsoft.OpenApi.Any.OpenApiObject
                    {
                        ["label"] = new Microsoft.OpenApi.Any.OpenApiString("No"),
                        ["value"] = new Microsoft.OpenApi.Any.OpenApiString("no"),
                        ["isCorrect"] = new Microsoft.OpenApi.Any.OpenApiBoolean(false),
                        ["score"] = new Microsoft.OpenApi.Any.OpenApiFloat(0.0f)
                    }
                }
            };
        }
        else if (type == typeof(Application.Attempts.Commands.SyncAnswersCommand))
        {
            schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["attemptId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
                ["answers"] = new Microsoft.OpenApi.Any.OpenApiArray
                {
                    new Microsoft.OpenApi.Any.OpenApiObject
                    {
                        ["questionId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
                        ["questionKey"] = new Microsoft.OpenApi.Any.OpenApiString("q1"),
                        ["valueType"] = new Microsoft.OpenApi.Any.OpenApiString("Text"),
                        ["textValue"] = new Microsoft.OpenApi.Any.OpenApiString("My answer"),
                        ["numericValue"] = new Microsoft.OpenApi.Any.OpenApiFloat(42.0f),
                        ["booleanValue"] = new Microsoft.OpenApi.Any.OpenApiBoolean(true),
                        ["choiceValues"] = new Microsoft.OpenApi.Any.OpenApiArray
                        {
                            new Microsoft.OpenApi.Any.OpenApiString("yes")
                        },
                        ["clientRevision"] = new Microsoft.OpenApi.Any.OpenApiInteger(1)
                    }
                },
                ["clientRevision"] = new Microsoft.OpenApi.Any.OpenApiInteger(1)
            };
        }
        else if (type == typeof(Application.Attempts.Commands.CreateAttemptCommand))
        {
            schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["versionId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
                ["userId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
                ["companyId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
                ["totalSteps"] = new Microsoft.OpenApi.Any.OpenApiInteger(5)
            };
        }
        else if (type == typeof(Application.Assessments.Commands.CreateCompanyCommand))
        {
            schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["name"] = new Microsoft.OpenApi.Any.OpenApiString("Acme Corp"),
                ["slug"] = new Microsoft.OpenApi.Any.OpenApiString("acme-corp"),
                ["industry"] = new Microsoft.OpenApi.Any.OpenApiString("Technology")
            };
        }
        else if (type == typeof(Application.Results.Commands.CalculateResultCommand))
        {
            schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["attemptId"] = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString())
            };
        }
    }
}
