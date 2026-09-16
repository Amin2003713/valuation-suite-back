using ApiFramework.Controller;
using Application.Results.Commands.CalculateResult;
using Application.Results.Queries.GetResult;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Controllers;

[ApiController]
[Route("api/results")]
public class ResultsController : ApiBaseController
{
    [HttpPost("calculate")]
    public Task<IActionResult> Calculate([FromBody] CalculateResultCommand command, CancellationToken ct)
        => ExecuteCreateAsync(command, "GetResult", ct);

    [HttpGet("{attemptId:guid}", Name = "GetResult")]
    public Task<IActionResult> GetResult(Guid attemptId, CancellationToken ct)
        => ExecuteAsync(new GetAssessmentResultQuery { AttemptId = attemptId }, ct);

    [HttpGet("assessment/{assessmentId:guid}")]
    public Task<IActionResult> GetResults(Guid assessmentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => ExecuteAsync(new GetAssessmentResultsQuery { AssessmentId = assessmentId, Page = page, PageSize = pageSize }, ct);
}
