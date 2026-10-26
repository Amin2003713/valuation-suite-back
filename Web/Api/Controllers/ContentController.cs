using Application.Admin;
using Application.Interfaces;
using Application.Interfaces.Base;
using Common.Exceptions;
using Domain.Content;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Web.Api.Controllers;

/// <summary>
///     Site content: footer links and blog posts. Public GETs power the site
///     footer and the landing blog section; admin CRUD is permission-gated.
///     Strictly additive: touches nothing in payments, pricing or tool logic.
/// </summary>
[ApiController]
[Route("api/content")]
public class ContentController(
    ICommandRepository<FooterLink> footerCommands,
    IQueryRepository<FooterLink> footerQueries,
    ICommandRepository<BlogPost> blogCommands,
    IQueryRepository<BlogPost> blogQueries) : ControllerBase
{
    private static bool IsValidHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);

    // ─── Blog body HTML ─────────────────────────────────────────────────────

    /// <summary>
    ///     Allowlist sanitizer for post bodies written by the admin rich-text
    ///     editor: formatting, lists, links, images and simple tables only — no
    ///     scripts, event handlers, inline styles or data: URLs. Bodies saved
    ///     before the HTML editor (plain text + "## " headings) contain no tags
    ///     and pass through untouched for the legacy renderer.
    /// </summary>
    private static readonly Regex HtmlTag = new("<[a-z!/][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HtmlSanitizer BlogBodySanitizer = CreateBlogBodySanitizer();

    private static HtmlSanitizer CreateBlogBodySanitizer()
    {
        var sanitizer = new HtmlSanitizer { KeepChildNodes = true };

        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(
        [
            "p", "br", "h2", "h3", "h4", "strong", "b", "em", "i", "u", "s", "sub", "sup",
            "ul", "ol", "li", "blockquote", "a", "img", "figure", "figcaption",
            "code", "pre", "hr", "table", "thead", "tbody", "tr", "th", "td",
        ]);

        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.UnionWith(
        [
            "href", "title", "target", "rel", "src", "alt", "width", "height", "dir",
            "colspan", "rowspan",
        ]);

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto"]);

        sanitizer.AllowedCssProperties.Clear();
        sanitizer.AllowedAtRules.Clear();

        return sanitizer;
    }

    private static string SanitizeBlogBody(string body) =>
        HtmlTag.IsMatch(body) ? BlogBodySanitizer.Sanitize(body).Trim() : body;

    // ─── Footer links — public ──────────────────────────────────────────────

    /// <summary>Active footer links grouped by column, in sort order.</summary>
    [HttpGet("footer-links")]
    [AllowAnonymous]
    public async Task<IActionResult> FooterLinks(CancellationToken ct) =>
        Ok(await footerQueries.TableNoTracking
            .Where(l => l.IsActive)
            .OrderBy(l => l.SortOrder)
            .Select(l => new FooterLinkDto(l.ColumnFa, l.ColumnEn, l.LabelFa, l.LabelEn, l.Href, l.SortOrder))
            .ToListAsync(ct));

    public record FooterLinkDto(string ColumnFa, string ColumnEn, string LabelFa, string LabelEn, string Href, int SortOrder);

    // ─── Footer links — admin CRUD ──────────────────────────────────────────

    [HttpGet("admin/footer-links")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> AdminFooterLinks(CancellationToken ct) =>
        Ok(await footerQueries.TableNoTracking
            .OrderBy(l => l.ColumnEn).ThenBy(l => l.SortOrder)
            .Select(l => new AdminFooterLinkDto(l.Id, l.ColumnFa, l.ColumnEn, l.LabelFa, l.LabelEn, l.Href, l.SortOrder, l.IsActive))
            .ToListAsync(ct));

    public record AdminFooterLinkDto(Guid Id, string ColumnFa, string ColumnEn, string LabelFa, string LabelEn, string Href, int SortOrder, bool IsActive);

    public record UpsertFooterLinkRequest(string? ColumnFa, string? ColumnEn, string? LabelFa, string? LabelEn, string? Href, int SortOrder);

    [HttpPost("admin/footer-links")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> CreateFooterLink([FromBody] UpsertFooterLinkRequest req, CancellationToken ct)
    {
        var (colFa, colEn, lblFa, lblEn, href, error) = ValidateFooterLink(req);
        if (error is not null) throw new ValuationException(error);

        var entity = new FooterLink { ColumnFa = colFa!, ColumnEn = colEn!, LabelFa = lblFa!, LabelEn = lblEn!, Href = href, SortOrder = req.SortOrder };
        footerCommands.Add(entity, saveNow: false);
        await footerCommands.SaveChangesAsync(ct);
        return Ok(new { id = entity.Id });
    }

    [HttpPut("admin/footer-links/{id:guid}")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> UpdateFooterLink(Guid id, [FromBody] UpsertFooterLinkRequest req, CancellationToken ct)
    {
        var entity = await footerCommands.Table.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (entity is null) return NotFound();

        var (colFa, colEn, lblFa, lblEn, href, error) = ValidateFooterLink(req);
        if (error is not null) throw new ValuationException(error);

        entity.ColumnFa = colFa!; entity.ColumnEn = colEn!; entity.LabelFa = lblFa!; entity.LabelEn = lblEn!; entity.Href = href;
        entity.SortOrder = req.SortOrder;
        await footerCommands.SaveChangesAsync(ct);
        return Ok(new { id });
    }

    [HttpDelete("admin/footer-links/{id:guid}")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> DeleteFooterLink(Guid id, CancellationToken ct)
    {
        var entity = await footerCommands.Table.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (entity is null) return NotFound();
        footerCommands.Delete(entity); // soft delete via global rules
        await footerCommands.SaveChangesAsync(ct);
        return NoContent();
    }

    private static (string ColFa, string ColEn, string LblFa, string LblEn, string Href, string? Error) ValidateFooterLink(UpsertFooterLinkRequest req)
    {
        var colFa = req.ColumnFa?.Trim() ?? "";
        var colEn = req.ColumnEn?.Trim() ?? "";
        var lblFa = req.LabelFa?.Trim() ?? "";
        var lblEn = req.LabelEn?.Trim() ?? "";
        var href = req.Href?.Trim() ?? "";

        if (colFa.Length is < 1 or > 60 || colEn.Length is < 1 or > 60)
            return ("", "", "", "", "", "عنوان ستون را (فارسی و انگلیسی) وارد کنید.");
        if (lblFa.Length is < 1 or > 80 || lblEn.Length is < 1 or > 80)
            return ("", "", "", "", "", "برچسب لینک را (فارسی و انگلیسی) وارد کنید.");
        if (href.Length is < 1 or > 500)
            return ("", "", "", "", "", "مقصد لینک را وارد کنید.");
        if (!href.StartsWith('/') && !IsValidHttpUrl(href))
            return ("", "", "", "", "", "مقصد لینک باید مسیر داخلی یا نشانی http/https باشد.");

        return (colFa, colEn, lblFa, lblEn, href, null);
    }

    // ─── Blog posts — public ────────────────────────────────────────────────

    /// <summary>Latest published posts for the landing blog section.</summary>
    [HttpGet("blog")]
    [AllowAnonymous]
    public async Task<IActionResult> PublicBlog([FromQuery] int take = 6, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 24);
        return Ok(await blogQueries.TableNoTracking
            .Where(p => p.IsActive && p.Status == BlogPostStatus.Published)
            .OrderByDescending(p => p.PublishedAt)
            .Take(take)
            .Select(p => new BlogPostDto(p.Id, p.Slug, p.TitleFa, p.TitleEn, p.SummaryFa, p.SummaryEn, p.PublishedAt))
            .ToListAsync(ct));
    }

    public record BlogPostDto(Guid Id, string Slug, string TitleFa, string TitleEn, string SummaryFa, string SummaryEn, DateTime? PublishedAt);

    /// <summary>One published post, for the future /blog/[slug] page.</summary>
    [HttpGet("blog/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> PublicPost(string slug, CancellationToken ct)
    {
        var post = await blogQueries.TableNoTracking
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive && p.Status == BlogPostStatus.Published, ct);
        if (post is null) return NotFound();
        return Ok(new BlogFullDto(post.Id, post.Slug, post.TitleFa, post.TitleEn, post.SummaryFa, post.SummaryEn, post.BodyFa, post.BodyEn, post.PublishedAt));
    }

    public record BlogFullDto(Guid Id, string Slug, string TitleFa, string TitleEn, string SummaryFa, string SummaryEn, string BodyFa, string BodyEn, DateTime? PublishedAt);

    // ─── Blog posts — admin CRUD ────────────────────────────────────────────

    /// <summary>One post with bodies, for the admin editor.</summary>
    [HttpGet("admin/blog/{id:guid}")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> AdminBlogPost(Guid id, CancellationToken ct)
    {
        var post = await blogQueries.TableNoTracking.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null) return NotFound();
        return Ok(new AdminBlogFullDto(post.Id, post.Slug, post.TitleFa, post.TitleEn, post.SummaryFa, post.SummaryEn, post.BodyFa, post.BodyEn, post.Status, post.PublishedAt, post.CreatedAt, post.IsActive));
    }

    public record AdminBlogFullDto(Guid Id, string Slug, string TitleFa, string TitleEn, string SummaryFa, string SummaryEn, string BodyFa, string BodyEn, BlogPostStatus Status, DateTime? PublishedAt, DateTime CreatedAt, bool IsActive);

    [HttpGet("admin/blog")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> AdminBlog(CancellationToken ct) =>
        Ok(await blogQueries.TableNoTracking
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new AdminBlogDto(p.Id, p.Slug, p.TitleFa, p.TitleEn, p.Status, p.PublishedAt, p.CreatedAt, p.IsActive))
            .ToListAsync(ct));

    public record AdminBlogDto(Guid Id, string Slug, string TitleFa, string TitleEn, BlogPostStatus Status, DateTime? PublishedAt, DateTime CreatedAt, bool IsActive);

    public record UpsertBlogRequest(string? Slug, string? TitleFa, string? TitleEn, string? SummaryFa, string? SummaryEn, string? BodyFa, string? BodyEn, DateTime? PublishedAt);

    [HttpPost("admin/blog")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> CreateBlogPost([FromBody] UpsertBlogRequest req, CancellationToken ct)
    {
        var (slug, titleFa, titleEn, sumFa, sumEn, bodyFa, bodyEn, error) = ValidateBlog(req, requireBody: false);
        if (error is not null) throw new ValuationException(error);

        var dup = await blogQueries.TableNoTracking.AnyAsync(p => p.Slug == slug, ct);
        if (dup) throw new ValuationException("این نامک (slug) قبلاً استفاده شده است.");

        var entity = new BlogPost { Slug = slug!, TitleFa = titleFa!, TitleEn = titleEn!, SummaryFa = sumFa, SummaryEn = sumEn, BodyFa = bodyFa, BodyEn = bodyEn, PublishedAt = ClampedPublishedAt(req.PublishedAt) };
        blogCommands.Add(entity, saveNow: false);
        await blogCommands.SaveChangesAsync(ct);
        return Ok(new { id = entity.Id });
    }

    [HttpPut("admin/blog/{id:guid}")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> UpdateBlogPost(Guid id, [FromBody] UpsertBlogRequest req, CancellationToken ct)
    {
        var entity = await blogCommands.Table.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return NotFound();

        var (slug, titleFa, titleEn, sumFa, sumEn, bodyFa, bodyEn, error) = ValidateBlog(req, requireBody: false);
        if (error is not null) throw new ValuationException(error);

        var dup = await blogQueries.TableNoTracking.AnyAsync(p => p.Slug == slug && p.Id != id, ct);
        if (dup) throw new ValuationException("این نامک (slug) قبلاً استفاده شده است.");

        entity.Slug = slug!; entity.TitleFa = titleFa!; entity.TitleEn = titleEn!;
        entity.SummaryFa = sumFa; entity.SummaryEn = sumEn; entity.BodyFa = bodyFa; entity.BodyEn = bodyEn;
        entity.PublishedAt = ClampedPublishedAt(req.PublishedAt);
        await blogCommands.SaveChangesAsync(ct);
        return Ok(new { id });
    }

    /// <summary>Publish / unpublish (draft ⇄ live). Optional `at` overrides the release date.</summary>
    [HttpPost("admin/blog/{id:guid}/publish")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> PublishBlogPost(Guid id, [FromQuery] bool publish = true, [FromQuery] DateTime? at = null, CancellationToken ct = default)
    {
        var entity = await blogCommands.Table.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return NotFound();
        if (publish && entity.Status == BlogPostStatus.Draft &&
            (string.IsNullOrWhiteSpace(entity.BodyFa) && string.IsNullOrWhiteSpace(entity.BodyEn)))
            throw new ValuationException("برای انتشار، متن پست لازم است (فارسی یا انگلیسی).");
        if (publish)
        {
            entity.Publish();
            if (at is not null) entity.PublishedAt = ClampedPublishedAt(at);
        }
        else entity.Unpublish();
        await blogCommands.SaveChangesAsync(ct);
        return Ok(new { id, status = entity.Status.ToString(), publishedAt = entity.PublishedAt });
    }

    [HttpDelete("admin/blog/{id:guid}")]
    [Authorize(Policy = "perm:content.manage")]
    public async Task<IActionResult> DeleteBlogPost(Guid id, CancellationToken ct)
    {
        var entity = await blogCommands.Table.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return NotFound();
        blogCommands.Delete(entity); // soft delete
        await blogCommands.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Sanitises an admin-provided release date; anything implausible becomes null.</summary>
    private static DateTime? ClampedPublishedAt(DateTime? at) =>
        at is { } d && d.Year is >= 2000 and <= 2100 ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : null;

    private static (string Slug, string TitleFa, string TitleEn, string SumFa, string SumEn, string BodyFa, string BodyEn, string? Error) ValidateBlog(UpsertBlogRequest req, bool requireBody)
    {
        var slug = (req.Slug?.Trim() ?? "").ToLowerInvariant();
        var titleFa = req.TitleFa?.Trim() ?? "";
        var titleEn = req.TitleEn?.Trim() ?? "";
        var sumFa = req.SummaryFa?.Trim() ?? "";
        var sumEn = req.SummaryEn?.Trim() ?? "";
        var bodyFa = SanitizeBlogBody(req.BodyFa?.Trim() ?? "");
        var bodyEn = SanitizeBlogBody(req.BodyEn?.Trim() ?? "");

        if (!System.Text.RegularExpressions.Regex.IsMatch(slug, @"^[a-z0-9]+(?:-[a-z0-9]+)*$") || slug.Length > 120)
            return ("", "", "", "", "", "", "", "نامک (slug) فقط حروف انگلیسی کوچک، عدد و خط تیره.");
        if (titleFa.Length is < 1 or > 200 || titleEn.Length is < 1 or > 200)
            return ("", "", "", "", "", "", "", "عنوان را (فارسی و انگلیسی) وارد کنید.");
        if (sumFa.Length > 400 || sumEn.Length > 400)
            return ("", "", "", "", "", "", "", "خلاصه حداکثر ۴۰۰ نویسه.");
        if (bodyFa.Length > 100_000 || bodyEn.Length > 100_000)
            return ("", "", "", "", "", "", "", "متن پست بیش از حد بلند است.");

        return (slug, titleFa, titleEn, sumFa, sumEn, bodyFa, bodyEn, null);
    }
}
