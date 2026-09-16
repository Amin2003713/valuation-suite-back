using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Web.Api.Filters;

public class ExamplesSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null) return;

        var type = context.Type;

        if (type == typeof(Application.Assessments.Commands.CreateAssessment.CreateAssessmentCommand))
        {
            schema.Example = new OpenApiObject
            {
                ["name"] = new OpenApiString("IP Assessment Tool"),
                ["code"] = new OpenApiString("IP-ASSESS-001"),
                ["description"] = new OpenApiString("Assessment for IP evaluation"),
                ["companyId"] = new OpenApiString(Guid.NewGuid().ToString())
            };
        }
        else if (type == typeof(Application.Assessments.Commands.CreateVersion.CreateVersionCommand))
        {
            schema.Example = new OpenApiObject
            {
                ["assessmentId"] = new OpenApiString(Guid.NewGuid().ToString()),
                ["title"] = new OpenApiString("Draft Version 1")
            };
        }
        else if (type == typeof(Application.Assessments.Commands.AddStep.AddStepCommand))
        {
            schema.Example = new OpenApiObject
            {
                ["versionId"] = new OpenApiString(Guid.NewGuid().ToString()),
                ["title"] = new OpenApiString("Information Gathering"),
                ["description"] = new OpenApiString("Step 1 description"),
                ["order"] = new OpenApiInteger(1)
            };
        }
        else if (type == typeof(Application.Assessments.Commands.AddQuestion.AddQuestionCommand))
        {
            schema.Example = new OpenApiObject
            {
                ["stepId"] = new OpenApiString(Guid.NewGuid().ToString()),
                ["text"] = new OpenApiString("What is your IP?"),
                ["type"] = new OpenApiString("SingleChoice"),
                ["order"] = new OpenApiInteger(1),
                ["isRequired"] = new OpenApiBoolean(true)
            };
        }
        else if (type == typeof(Application.Assessments.Commands.SetQuestionOptions.SetQuestionOptionsCommand))
        {
            schema.Example = new OpenApiObject
            {
                ["questionId"] = new OpenApiString(Guid.NewGuid().ToString()),
                ["options"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["label"] = new OpenApiString("Yes"),
                        ["value"] = new OpenApiString("yes"),
                        ["isCorrect"] = new OpenApiBoolean(true),
                        ["score"] = new OpenApiFloat(100.0f)
                    },
                    new OpenApiObject
                    {
                        ["label"] = new OpenApiString("No"),
                        ["value"] = new OpenApiString("no"),
                        ["isCorrect"] = new OpenApiBoolean(false),
                        ["score"] = new OpenApiFloat(0.0f)
                    }
                }
            };
        }
        else if (type == typeof(Application.Attempts.Commands.SyncAnswers.SyncAnswersCommand))
        {
            schema.Example = new OpenApiObject
            {
                ["attemptId"] = new OpenApiString(Guid.NewGuid().ToString()),
                ["answers"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["questionId"] = new OpenApiString(Guid.NewGuid().ToString()),
                        ["questionKey"] = new OpenApiString("q1"),
                        ["valueType"] = new OpenApiString("Text"),
                        ["textValue"] = new OpenApiString("My answer"),
                        ["numericValue"] = new OpenApiFloat(42.0f),
                        ["booleanValue"] = new OpenApiBoolean(true),
                        ["choiceValues"] = new OpenApiArray
                        {
                            new OpenApiString("yes")
                        },
                        ["clientRevision"] = new OpenApiInteger(1)
                    }
                },
                ["clientRevision"] = new OpenApiInteger(1)
            };
        }
        else if (type == typeof(Application.Attempts.Commands.CreateAttempt.CreateAttemptCommand))
        {
            schema.Example = new OpenApiObject
            {
                ["versionId"] = new OpenApiString(Guid.NewGuid().ToString()),
                ["userId"] = new OpenApiString(Guid.NewGuid().ToString()),
                ["companyId"] = new OpenApiString(Guid.NewGuid().ToString()),
                ["totalSteps"] = new OpenApiInteger(5)
            };
        }
        else if (type == typeof(Application.Companies.Commands.CreateCompany.CreateCompanyCommand))
        {
            schema.Example = new OpenApiObject
            {
                ["name"] = new OpenApiString("Acme Corp"),
                ["slug"] = new OpenApiString("acme-corp"),
                ["industry"] = new OpenApiString("Technology")
            };
        }
        else if (type == typeof(Application.Results.Commands.CalculateResult.CalculateResultCommand))
        {
            schema.Example = new OpenApiObject
            {
                ["attemptId"] = new OpenApiString(Guid.NewGuid().ToString())
            };
        }
    }
}
