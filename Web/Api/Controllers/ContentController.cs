using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Controllers;

/// <summary>
///     Thin HTTP surface for site content: footer links and blog posts. Every
///     action maps to a MediatR request handled in <see cref="Handlers" /> —
///     validation, HTML sanitisation and uniqueness live there. Public GETs
///     power the site footer and the blog; admin CRUD is permission-gated.
/// </summary>
[ApiController]
[Route("api/content")]
public class ContentController(IMediator mediator) : ControllerBase
{
    // ─── Footer links — public ──────────────────────────────────────────────

    /// <summary>Active footer links grouped by column, in sort order.</summary>
    [HttpGet("footer-links")]
    [AllowAnonymous]
    public async Task<IActionResult> FooterLinks(CancellationToken ct)
        => Ok(await mediator.Send(new Handlers.GetFooterLinksQuery(), ct));

    // ─── Footer links — admin CRUD ──────────────────────────────────────────

    [HttpGet("admin/footer-links")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> AdminFooterLinks(CancellationToken ct)
        => Ok(await mediator.Send(new Handlers.GetAdminFooterLinksQuery(), ct));

    [HttpPost("admin/footer-links")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> CreateFooterLink(
        [FromBody] Handlers.UpsertFooterLinkRequest request, CancellationToken ct)
        => Ok(new { id = await mediator.Send(new Handlers.CreateFooterLinkCommand(request), ct) });

    [HttpPut("admin/footer-links/{id:guid}")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> UpdateFooterLink(
        Guid id, [FromBody] Handlers.UpsertFooterLinkRequest request, CancellationToken ct)
    {
        var updated = await mediator.Send(new Handlers.UpdateFooterLinkCommand(id, request), ct);
        return updated is null ? NotFound() : Ok(new { id = updated });
    }

    [HttpDelete("admin/footer-links/{id:guid}")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> DeleteFooterLink(Guid id, CancellationToken ct)
        => await mediator.Send(new Handlers.DeleteFooterLinkCommand(id), ct) ? NoContent() : NotFound();

    // ─── Blog posts — public ────────────────────────────────────────────────

    /// <summary>Latest published posts for the landing blog section.</summary>
    [HttpGet("blog")]
    [AllowAnonymous]
    public async Task<IActionResult> PublicBlog([FromQuery] int take = 6, CancellationToken ct = default)
        => Ok(await mediator.Send(new Handlers.GetPublicBlogQuery(take), ct));

    /// <summary>One published post, for the /blog/[slug] page.</summary>
    [HttpGet("blog/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> PublicPost(string slug, CancellationToken ct)
    {
        var post = await mediator.Send(new Handlers.GetPublicBlogPostQuery(slug), ct);
        return post is null ? NotFound() : Ok(post);
    }

    // ─── Blog posts — admin CRUD ────────────────────────────────────────────

    /// <summary>One post with bodies, for the admin editor.</summary>
    [HttpGet("admin/blog/{id:guid}")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> AdminBlogPost(Guid id, CancellationToken ct)
    {
        var post = await mediator.Send(new Handlers.GetAdminBlogPostQuery(id), ct);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpGet("admin/blog")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> AdminBlog(CancellationToken ct)
        => Ok(await mediator.Send(new Handlers.GetAdminBlogQuery(), ct));

    [HttpPost("admin/blog")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> CreateBlogPost(
        [FromBody] Handlers.UpsertBlogRequest request, CancellationToken ct)
        => Ok(new { id = await mediator.Send(new Handlers.CreateBlogPostCommand(request), ct) });

    [HttpPut("admin/blog/{id:guid}")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> UpdateBlogPost(
        Guid id, [FromBody] Handlers.UpsertBlogRequest request, CancellationToken ct)
    {
        var updated = await mediator.Send(new Handlers.UpdateBlogPostCommand(id, request), ct);
        return updated is null ? NotFound() : Ok(new { id = updated });
    }

    /// <summary>Publish / unpublish (draft ⇄ live). Optional `at` overrides the release date.</summary>
    [HttpPost("admin/blog/{id:guid}/publish")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> PublishBlogPost(
        Guid id, [FromQuery] bool publish = true, [FromQuery] DateTime? at = null, CancellationToken ct = default)
    {
        var result = await mediator.Send(new Handlers.PublishBlogPostCommand(id, publish, at), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("admin/blog/{id:guid}")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> DeleteBlogPost(Guid id, CancellationToken ct)
        => await mediator.Send(new Handlers.DeleteBlogPostCommand(id), ct) ? NoContent() : NotFound();
}
