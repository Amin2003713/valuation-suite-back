using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApiFramework.Tools;

namespace ApiFramework.Filters;

/// <summary>
/// Converts ValuationException (and anything unhandled) into a consistent ApiProblemDetails response.
/// Registered globally; works alongside ModelState invalidation handled by ValidateModelStateAttribute.
/// </summary>
public sealed class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var traceId = context.HttpContext.TraceIdentifier;

        var problem = context.Exception switch
        {
            ValuationException valuationException => ApiProblemDetails.From(valuationException, traceId),
            _ => HandleUnexpected(context, traceId)
        };

        if (problem.Extensions.ContainsKey("errors"))
            problem.Extensions["errors"] = problem.Extensions["errors"];

        context.Result = new ObjectResult(problem)
        {
            StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError
        };

        context.ExceptionHandled = true;
    }

    private ApiProblemDetails HandleUnexpected(ExceptionContext context, string traceId)
    {
        _logger.LogError(context.Exception, "Unhandled exception for {TraceId}", traceId);
        return ApiProblemDetails.From(context.Exception, traceId);
    }
}
