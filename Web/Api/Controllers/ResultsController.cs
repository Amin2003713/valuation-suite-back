using ApiFramework.Controller;
using Application.Results.Commands.CalculateResult;
using Application.Results.Queries.GetResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Controllers;

/// <summary>
///     Assessment results. Calculate/read are owner-scoped inside the handlers
///     (acting user from the JWT); the per-assessment listing is a cross-user
///     reporting view, so it sits behind the submissions-read permission.
/// </summary>
[ApiController]
[Route("api/results")]
[Authorize]
public class ResultsController : ApiBaseController
{
    [HttpPost("calculate")]
    public Task<IActionResult> Calculate([FromBody] CalculateResultCommand command, CancellationToken ct)
        => ExecuteCreateAsync(command, "GetResult", ct);

    [HttpGet("{attemptId:guid}", Name = "GetResult")]
    public Task<IActionResult> GetResult(Guid attemptId, CancellationToken ct)
        => ExecuteAsync(new GetAssessmentResultQuery { AttemptId = attemptId }, ct);

    [HttpGet("assessment/{assessmentId:guid}")]
    [Authorize(Policy = "perm:submissions.read")]
    public Task<IActionResult> GetResults(Guid assessmentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => ExecuteAsync(new GetAssessmentResultsQuery { AssessmentId = assessmentId, Page = page, PageSize = pageSize }, ct);
}
