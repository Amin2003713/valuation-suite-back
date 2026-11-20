using System.Text;
using System.Text.Json;
using ApiFramework.Controller;
using Application.Interfaces;
using Application.Tools;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using RequestHandlers.Tools;
using Web.Api.Services;

namespace Web.Api.Controllers;

/// <summary>
///     Thin HTTP surface for tools (Uncle Bob: no logic here). The client is pure UI —
///     forms, defaults, reference data, math and persistence all live in the backend.
/// </summary>
[ApiController]
[Route("api/tools")]
public class ToolsController(
    IMediator mediator,
    ICurrentUserAccessor currentUser,
    IMemoryCache cache) : ControllerBase
{
    /// <summary>
    ///     How long a public catalogue/form payload is reused in-process. The
    ///     forms are identical for every anonymous caller in the same language,
    ///     so this removes a full query (plus localization joins) per page view.
    /// </summary>
    private static readonly TimeSpan PublicCacheTtl = TimeSpan.FromSeconds(60);

    /// <summary>Cache key includes the language because translations are payload.</summary>
    private string CacheKey(string suffix) =>
        $"tools:{suffix}:{Request.Query["lang"]}:{Request.Headers.AcceptLanguage}";

    /// <summary>List of registered tools (for building menus).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        // Public, changes only when staff edit tool metadata: let the browser,
        // any CDN and the process itself hold it instead of refetching.
        Response.Headers.CacheControl = "public, max-age=300";
        Response.Headers.Vary = "Accept-Language";

        var key = CacheKey("list");
        if (!cache.TryGetValue(key, out var payload))
        {
            payload = await mediator.Send(new GetToolFormsQuery(), ct);
            cache.Set(key, payload, PublicCacheTtl);
        }

        return Ok(payload);
    }

    /// <summary>Form definition (defaults, labels, reference data) for one tool.</summary>
    [HttpGet("{toolCode}/form")]
    [AllowAnonymous]
    public async Task<IActionResult> Form(string toolCode, CancellationToken ct)
    {
        // Cache key is language-aware, so ?lang= keeps the translations apart.
        Response.Headers.CacheControl = "public, max-age=300";
        Response.Headers.Vary = "Accept-Language";

        var key = CacheKey($"form:{toolCode.ToUpperInvariant()}");
        if (!cache.TryGetValue(key, out var payload))
        {
            payload = await mediator.Send(new GetToolFormQuery { ToolCode = toolCode }, ct);
            cache.Set(key, payload, PublicCacheTtl);
        }

        return Ok(payload);
    }

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
    ///     CSV export of one stored run (owner only; staff allowed for support).
    ///     Plain input/result export — not a formatted valuation report.
    /// </summary>
    [HttpGet("submissions/{id:guid}/export")]
    [Authorize]
    public async Task<IActionResult> ExportSubmission(Guid id, CancellationToken ct)
    {
        var file = await mediator.Send(new ExportToolSubmissionQuery(id), ct);
        // UTF-8 BOM so Excel opens Persian text without mojibake.
        return File(Encoding.UTF8.GetBytes("\uFEFF" + file.Content), file.ContentType, file.FileName);
    }

    /// <summary>
    ///     Paid printable report (HTML, print → PDF) for one stored run. Pro subscribers
    ///     or buyers of that tool's advanced section only; 403 otherwise (server-gated).
    /// </summary>
    [HttpGet("submissions/{id:guid}/report")]
    [Authorize]
    public async Task<IActionResult> Report(Guid id, CancellationToken ct)
    {
        var report = await mediator.Send(new ExportToolSubmissionReportQuery(id), ct);
        return Content(report.Html, "text/html; charset=utf-8");
    }

    /// <summary>
    ///     Standards each tool is aligned to, with the methods the engine applies and
    ///     the element-by-element coverage. Public: it is reference data, not content.
    /// </summary>
    [HttpGet("standards")]
    [AllowAnonymous]
    public IActionResult Standards()
    {
        Response.Headers.CacheControl = "public, max-age=300";
        return Ok(Application.Tools.ToolStandards.All);
    }

    /// <summary>
    ///     Benchmark stats for the spider-web chart of one tool: per-metric
    ///     Min/Max/Avg inside the caller's company industry plus the same stats
    ///     across ALL submissions of the tool.
    /// </summary>
    [HttpGet("benchmark")]
    [Authorize]
    public async Task<IActionResult> Benchmark([FromQuery] string toolCode, CancellationToken ct)
        => Ok(await mediator.Send(new GetToolBenchmarkQuery(toolCode, User.GetUserId()), ct));

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
