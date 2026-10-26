using Application.Adviser;
using Application.Interfaces;
using Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Controllers;

/// <summary>
///     The per-user, per-tool "Talk to the Adviser" chat (Phase 2 of the suite
///     upgrade). Ask builds context (tool inputs, latest result, company/area)
///     server-side, answers, and PERSISTS both messages so the conversation
///     survives reloads; history/clear manage the same thread. Rate limited —
///     see <see cref="AdviserRateLimiter"/>.
/// </summary>
[ApiController]
[Route("api/adviser")]
[Authorize]
public class AdviserController(IMediator mediator) : ControllerBase
{
    /// <summary>Ask the adviser one question about the current tool.</summary>
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskBody body, CancellationToken ct)
        => Ok(await mediator.Send(new AskAdviserCommand(body.ToolCode, body.Question), ct));

    public sealed record AskBody(string ToolCode, string Question);

    /// <summary>The caller's persisted chat history for one tool (oldest first).</summary>
    [HttpGet("history/{toolCode}")]
    public async Task<IActionResult> History(string toolCode, CancellationToken ct)
        => Ok(await mediator.Send(new GetAdviserHistoryQuery(toolCode), ct));

    /// <summary>"New conversation" — clears the caller's history for one tool.</summary>
    [HttpDelete("history/{toolCode}")]
    public async Task<IActionResult> Clear(string toolCode, CancellationToken ct)
    {
        await mediator.Send(new ClearAdviserHistoryCommand(toolCode), ct);
        return Ok(new { cleared = true });
    }
}
