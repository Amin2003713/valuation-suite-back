using System.Text.Json;
using ApiFramework.Controller;
using Application.Tools;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Controllers;

/// <summary>
///     Thin HTTP surface for tools (Uncle Bob: no logic here). The client is pure UI —
///     forms, defaults, reference data, math and persistence all live in the backend.
/// </summary>
[ApiController]
[Route("api/tools")]
public class ToolsController(IMediator mediator) : ControllerBase
{
    /// <summary>List of registered tools (for building menus).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await mediator.Send(new GetToolFormsQuery(), ct));

    /// <summary>Form definition (defaults, labels, reference data) for one tool.</summary>
    [HttpGet("{toolCode}/form")]
    [AllowAnonymous]
    public async Task<IActionResult> Form(string toolCode, CancellationToken ct)
        => Ok(await mediator.Send(new GetToolFormQuery { ToolCode = toolCode }, ct));

    /// <summary>
    ///     Runs a tool: the backend computes the result. Pass <c>?persist=false</c>
    ///     for a preview run that is returned but not stored as a submission —
    ///     the client persists on explicit save or when leaving the page.
    /// </summary>
    [HttpPost("{toolCode}/run")]
    [Authorize]
    public async Task<IActionResult> Run(
        string toolCode, [FromBody] JsonElement input, [FromQuery] bool? persist, CancellationToken ct)
        => Ok(await mediator.Send(new RunToolCommand
        {
            ToolCode = toolCode,
            Input = input,
            Persist = persist ?? true,
        }, ct));

    /// <summary>
    ///     Runs a tool on behalf of a client/company (admin flow). The submission is
    ///     attributed to the target user when they belong to the target company.
    /// </summary>
    [HttpPost("{toolCode}/run-for")]
    [Authorize(Policy = "perm:submissions.read")]
    public async Task<IActionResult> RunFor(string toolCode, [FromBody] RunForBody body, CancellationToken ct)
        => Ok(await mediator.Send(new RunToolCommand
        {
            ToolCode = toolCode,
            Input = body.Input,
            RunFor = new RunForContext { CompanyId = body.CompanyId, UserId = body.UserId, Note = body.Note },
        }, ct));

    public sealed record RunForBody(JsonElement Input, Guid? CompanyId, Guid? UserId, string? Note);

    /// <summary>
    ///     Freemium catalog: tool prices (basics free, advanced paid), packages and
    ///     the current user's access state. Powers the /pricing page.
    /// </summary>
    [HttpGet("pricing")]
    [Authorize]
    public async Task<IActionResult> Pricing(CancellationToken ct)
        => Ok(await mediator.Send(new GetPricingCatalogQuery(), ct));

    /// <summary>The current user's saved submissions (optionally filtered by tool).</summary>
    [HttpGet("submissions")]
    [Authorize]
    public async Task<IActionResult> Submissions([FromQuery] string? toolCode, CancellationToken ct)
        => Ok(await mediator.Send(new GetToolSubmissionsQuery { ToolCode = toolCode }, ct));

    /// <summary>
    ///     The current user's latest advice thread for a tool (latest submission with
    ///     notes or a paid advice payment) — keeps the chat visible across reloads.
    ///     "me" is reserved and cannot collide with tool codes.
    /// </summary>
    [HttpGet("me/advice-thread/{toolCode}")]
    [Authorize]
    public async Task<IActionResult> MyToolAdviceThread(string toolCode, CancellationToken ct)
        => Ok(await mediator.Send(new Application.Admin.GetMyToolAdviceThreadQuery(toolCode), ct));

    /// <summary>
    ///     Customer fetches the advice thread on their own submission (marks staff
    ///     messages seen) together with entitlement info (purchased / can reply).
    /// </summary>
    [HttpGet("my-submissions/{submissionId:guid}/notes")]
    [Authorize]
    public async Task<IActionResult> MySubmissionNotes(Guid submissionId, CancellationToken ct)
        => Ok(await mediator.Send(new Application.Admin.GetMySubmissionNotesQuery(submissionId), ct));

    /// <summary>
    ///     Customer follow-up question in the advice chat (requires paid advice).
    /// </summary>
    [HttpPost("my-submissions/{submissionId:guid}/notes")]
    [Authorize]
    public async Task<IActionResult> ReplyToAdviser(
        Guid submissionId, [FromBody] ReplyBody body, CancellationToken ct)
        => Ok(await mediator.Send(new Application.Admin.AddCustomerReplyCommand(submissionId, body.Text), ct));

    public sealed record ReplyBody(string? Text);
}
