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

    /// <summary>Runs a tool: backend computes the result and persists a submission.</summary>
    [HttpPost("{toolCode}/run")]
    [Authorize]
    public async Task<IActionResult> Run(string toolCode, [FromBody] JsonElement input, CancellationToken ct)
        => Ok(await mediator.Send(new RunToolCommand { ToolCode = toolCode, Input = input }, ct));

    /// <summary>The current user's saved submissions (optionally filtered by tool).</summary>
    [HttpGet("submissions")]
    [Authorize]
    public async Task<IActionResult> Submissions([FromQuery] string? toolCode, CancellationToken ct)
        => Ok(await mediator.Send(new GetToolSubmissionsQuery { ToolCode = toolCode }, ct));
}
