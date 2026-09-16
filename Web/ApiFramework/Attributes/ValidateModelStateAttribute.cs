using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ApiFramework.Attributes;

/// <summary>
/// Short-circuits the action when ModelState is invalid and returns the standard ApiProblemDetails body.
/// </summary>
public sealed class ValidateModelStateAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
            return;

        var errors = context.ModelState
            .Where(kvp => kvp.Value is { Errors.Count: > 0 })
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        var traceId = context.HttpContext.TraceIdentifier;
        var problem = ApiProblemDetails.FromValidation(errors, traceId);

        context.Result = new BadRequestObjectResult(problem);
    }
}
