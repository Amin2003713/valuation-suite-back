using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace ApiFramework.Tools;

public class ApiProblemDetails : ProblemDetails
{
    /// <summary>Stable machine-readable error code (same value as the exception's Code when applicable).</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Correlation id so the client can quote it in support requests.</summary>
    public string? TraceId { get; set; }

    public static ApiProblemDetails From(ValuationException exception, string? traceId)
    {
        var problem = new ApiProblemDetails
        {
            Title = exception.Message,
            Detail = exception.Message,
            Status = (int)exception.HttpStatusCode,
            TraceId = traceId
        };

        if (exception.AdditionalData is not null)
            problem.Extensions["additionalData"] = exception.AdditionalData;

        return problem;
    }

    public static ApiProblemDetails From(Exception exception, string? traceId)
    {
        var problem = new ApiProblemDetails
        {
            Title = "An unexpected error occurred.",
            Detail = exception.Message,
            Status = StatusCodes.Status500InternalServerError,
            ErrorCode = "INTERNAL_ERROR",
            TraceId = traceId
        };

        return problem;
    }

    public static ApiProblemDetails FromValidation(IDictionary<string, string[]> errors, string? traceId)
    {
        var problem = new ApiProblemDetails
        {
            Title = "One or more validation errors occurred.",
            Detail = "See the errors property for details.",
            Status = StatusCodes.Status400BadRequest,
            ErrorCode = "VALIDATION_FAILED",
            TraceId = traceId
        };

        problem.Extensions["errors"] = errors;
        return problem;
    }
}
