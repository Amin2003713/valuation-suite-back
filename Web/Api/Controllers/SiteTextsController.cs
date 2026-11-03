using Application.Content;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Controllers;

/// <summary>
///     Admin-managed bilingual UI labels. Every action is a one-line mapping to
///     a MediatR request — validation and persistence live in the handlers.
///     Public reads are anonymous and cacheable; writes need content.manage.
/// </summary>
[ApiController]
[Route("api/content")]
public class SiteTextsController(IMediator mediator) : ControllerBase
{
    /// <summary>Active overrides only. The client keeps its built-in defaults.</summary>
    [HttpGet("site-texts")]
    [AllowAnonymous]
    public async Task<IActionResult> Public(CancellationToken ct)
    {
        // Staff edits are rare; a short shared cache keeps this off the hot path.
        Response.Headers.CacheControl = "public, max-age=60";
        Response.Headers.Vary = "Accept-Language";
        return Ok(await mediator.Send(new GetSiteTextsQuery(), ct));
    }

    [HttpGet("admin/site-texts")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> AdminList(CancellationToken ct)
        => Ok(await mediator.Send(new GetAdminSiteTextsQuery(), ct));

    public sealed record SiteTextBody(
        string? Key, string? Group, string? Label, string? Fa, string? En, int SortOrder, bool IsActive);

    [HttpPost("admin/site-texts")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> Create([FromBody] SiteTextBody body, CancellationToken ct)
        => Ok(new
        {
            id = await mediator.Send(new UpsertSiteTextCommand(
                null, body.Key, body.Group, body.Label, body.Fa, body.En, body.SortOrder, body.IsActive), ct),
        });

    [HttpPut("admin/site-texts/{id:guid}")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SiteTextBody body, CancellationToken ct)
        => Ok(new
        {
            id = await mediator.Send(new UpsertSiteTextCommand(
                id, body.Key, body.Group, body.Label, body.Fa, body.En, body.SortOrder, body.IsActive), ct),
        });

    [HttpDelete("admin/site-texts/{id:guid}")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => await mediator.Send(new DeleteSiteTextCommand(id), ct) ? NoContent() : NotFound();
}
