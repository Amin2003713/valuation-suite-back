using ApiFramework.Controller;
using Application.Attempts.Commands.AbandonAttempt;
using Application.Attempts.Commands.CompleteAttempt;
using Application.Attempts.Commands.CreateAttempt;
using Application.Attempts.Commands.SyncAnswers;
using Application.Attempts.Queries.GetAttempt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Controllers;

/// <summary>
///     Assessment attempts. Every action requires an authenticated caller, and the
///     acting user always comes from the JWT inside the handlers — never from a
///     query string or body field — so callers only ever touch their own attempts.
/// </summary>
[ApiController]
[Route("api/attempts")]
[Authorize]
public class AttemptsController : ApiBaseController
{
    [HttpPost]
    public Task<IActionResult> CreateAttempt([FromBody] CreateAttemptCommand command, CancellationToken ct)
        => ExecuteCreateAsync(command, nameof(GetAttempt), ct);

    [HttpGet("{id:guid}", Name = "GetAttempt")]
    public Task<IActionResult> GetAttempt(Guid id, CancellationToken ct)
        => ExecuteAsync(new GetAssessmentAttemptQuery { Id = id }, ct);

    [HttpGet("active")]
    public Task<IActionResult> GetActiveAttempt([FromQuery] Guid versionId, CancellationToken ct)
        => ExecuteAsync(new GetActiveAttemptQuery { VersionId = versionId }, ct);

    [HttpGet("{attemptId:guid}/answers")]
    public Task<IActionResult> GetAnswers(Guid attemptId, CancellationToken ct)
        => ExecuteAsync(new GetAttemptAnswersQuery { AttemptId = attemptId }, ct);

    [HttpGet("history")]
    public Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => ExecuteAsync(new GetAssessmentHistoryQuery { Page = page, PageSize = pageSize }, ct);

    [HttpPut("{id:guid}/answers")]
    public Task<IActionResult> SyncAnswers(Guid id, [FromBody] SyncAnswersCommand command, CancellationToken ct)
    {
        command.AttemptId = id;
        return ExecuteAsync(command, ct);
    }

    [HttpPost("{id:guid}/complete")]
    public Task<IActionResult> Complete(Guid id, CancellationToken ct)
        => ExecuteAsync(new CompleteAttemptCommand { Id = id }, ct);

    [HttpPost("{id:guid}/abandon")]
    public Task<IActionResult> Abandon(Guid id, CancellationToken ct)
        => ExecuteAsync(new AbandonAttemptCommand { Id = id }, ct);
}
