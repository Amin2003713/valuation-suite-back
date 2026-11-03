using System.Text.RegularExpressions;
using Application.Interfaces.Base;
using Common.Exceptions;
using Domain.Content;
using Ganss.Xss;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Web.Api.Handlers;

/* Site content — footer links and blog posts.
 *
 * Handlers own validation, HTML sanitisation, uniqueness and persistence; the
 * controller is a plain HTTP mapping. The sanitiser lives here (not in the
 * Application layer) because the HtmlSanitizer dependency belongs to this web
 * assembly.
 */

// ─── Blog body HTML ─────────────────────────────────────────────────────────

/// <summary>
///     Allowlist sanitizer for post bodies written by the admin rich-text
///     editor: formatting, lists, links, images and simple tables only — no
///     scripts, event handlers, inline styles or data: URLs. Bodies saved before
///     the HTML editor (plain text + "## " headings) contain no tags and pass
///     through untouched for the legacy renderer.
/// </summary>
internal static class BlogBody
{
    private static readonly Regex HtmlTag = new("<[a-z!/][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HtmlSanitizer Sanitizer = Create();

    private static HtmlSanitizer Create()
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

    public static string Sanitize(string body) =>
        HtmlTag.IsMatch(body) ? Sanitizer.Sanitize(body).Trim() : body;
}

// ─── Read models ────────────────────────────────────────────────────────────

public sealed record FooterLinkDto(
    string ColumnFa, string ColumnEn, string LabelFa, string LabelEn, string Href, int SortOrder);

public sealed record AdminFooterLinkDto(
    Guid Id, string ColumnFa, string ColumnEn, string LabelFa, string LabelEn,
    string Href, int SortOrder, bool IsActive);

public sealed record UpsertFooterLinkRequest(
    string? ColumnFa, string? ColumnEn, string? LabelFa, string? LabelEn, string? Href, int SortOrder);

public sealed record BlogPostDto(
    Guid Id, string Slug, string TitleFa, string TitleEn,
    string SummaryFa, string SummaryEn, DateTime? PublishedAt);

public sealed record BlogFullDto(
    Guid Id, string Slug, string TitleFa, string TitleEn, string SummaryFa, string SummaryEn,
    string BodyFa, string BodyEn, DateTime? PublishedAt);

public sealed record AdminBlogDto(
    Guid Id, string Slug, string TitleFa, string TitleEn,
    BlogPostStatus Status, DateTime? PublishedAt, DateTime CreatedAt, bool IsActive);

public sealed record AdminBlogFullDto(
    Guid Id, string Slug, string TitleFa, string TitleEn, string SummaryFa, string SummaryEn,
    string BodyFa, string BodyEn, BlogPostStatus Status, DateTime? PublishedAt,
    DateTime CreatedAt, bool IsActive);

public sealed record UpsertBlogRequest(
    string? Slug, string? TitleFa, string? TitleEn, string? SummaryFa, string? SummaryEn,
    string? BodyFa, string? BodyEn, DateTime? PublishedAt);

public sealed record BlogPublishResult(Guid Id, string Status, DateTime? PublishedAt);

// ─── Requests ───────────────────────────────────────────────────────────────

public sealed record GetFooterLinksQuery : IRequest<List<FooterLinkDto>>;
public sealed record GetAdminFooterLinksQuery : IRequest<List<AdminFooterLinkDto>>;
public sealed record CreateFooterLinkCommand(UpsertFooterLinkRequest Request) : IRequest<Guid>;
public sealed record UpdateFooterLinkCommand(Guid Id, UpsertFooterLinkRequest Request) : IRequest<Guid?>;
public sealed record DeleteFooterLinkCommand(Guid Id) : IRequest<bool>;

public sealed record GetPublicBlogQuery(int Take) : IRequest<List<BlogPostDto>>;
public sealed record GetPublicBlogPostQuery(string Slug) : IRequest<BlogFullDto?>;
public sealed record GetAdminBlogQuery : IRequest<List<AdminBlogDto>>;
public sealed record GetAdminBlogPostQuery(Guid Id) : IRequest<AdminBlogFullDto?>;
public sealed record CreateBlogPostCommand(UpsertBlogRequest Request) : IRequest<Guid>;
public sealed record UpdateBlogPostCommand(Guid Id, UpsertBlogRequest Request) : IRequest<Guid?>;
public sealed record PublishBlogPostCommand(Guid Id, bool Publish, DateTime? At) : IRequest<BlogPublishResult?>;
public sealed record DeleteBlogPostCommand(Guid Id) : IRequest<bool>;

// ─── Shared rules ───────────────────────────────────────────────────────────

internal static class ContentRules
{
    public static bool IsValidHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>Footer link validation. Returns a Persian error message, or null.</summary>
    public static string? ValidateFooterLink(UpsertFooterLinkRequest request)
    {
        var columnFa = request.ColumnFa?.Trim() ?? "";
        var columnEn = request.ColumnEn?.Trim() ?? "";
        var labelFa = request.LabelFa?.Trim() ?? "";
        var labelEn = request.LabelEn?.Trim() ?? "";
        var href = request.Href?.Trim() ?? "";

        if (columnFa.Length is < 1 or > 60 || columnEn.Length is < 1 or > 60)
            return "عنوان ستون را (فارسی و انگلیسی) وارد کنید.";
        if (labelFa.Length is < 1 or > 80 || labelEn.Length is < 1 or > 80)
            return "برچسب لینک را (فارسی و انگلیسی) وارد کنید.";
        if (href.Length is < 1 or > 500)
            return "مقصد لینک را وارد کنید.";
        if (!href.StartsWith('/') && !IsValidHttpUrl(href))
            return "مقصد لینک باید مسیر داخلی یا نشانی http/https باشد.";

        return null;
    }

    /// <summary>Sanitises an admin-provided release date; anything implausible becomes null.</summary>
    public static DateTime? ClampedPublishedAt(DateTime? at) =>
        at is { } date && date.Year is >= 2000 and <= 2100
            ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
            : null;

    /// <summary>Blog post validation. Returns a Persian error message, or null.</summary>
    public static string? ValidateBlog(UpsertBlogRequest request, out string slug)
    {
        slug = (request.Slug?.Trim() ?? "").ToLowerInvariant();
        var titleFa = request.TitleFa?.Trim() ?? "";
        var titleEn = request.TitleEn?.Trim() ?? "";
        var summaryFa = request.SummaryFa?.Trim() ?? "";
        var summaryEn = request.SummaryEn?.Trim() ?? "";
        var bodyFa = BlogBody.Sanitize(request.BodyFa?.Trim() ?? "");
        var bodyEn = BlogBody.Sanitize(request.BodyEn?.Trim() ?? "");

        if (!Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$") || slug.Length > 120)
            return "نامک (slug) فقط حروف انگلیسی کوچک، عدد و خط تیره.";
        if (titleFa.Length is < 1 or > 200 || titleEn.Length is < 1 or > 200)
            return "عنوان را (فارسی و انگلیسی) وارد کنید.";
        if (summaryFa.Length > 400 || summaryEn.Length > 400)
            return "خلاصه حداکثر ۴۰۰ نویسه.";
        if (bodyFa.Length > 100_000 || bodyEn.Length > 100_000)
            return "متن پست بیش از حد بلند است.";

        return null;
    }
}

// ─── Footer links ───────────────────────────────────────────────────────────

public sealed class GetFooterLinksHandler(IQueryRepository<FooterLink> links)
    : IRequestHandler<GetFooterLinksQuery, List<FooterLinkDto>>
{
    public async Task<List<FooterLinkDto>> Handle(GetFooterLinksQuery request, CancellationToken ct) =>
        await links.TableNoTracking
            .Where(l => l.IsActive)
            .OrderBy(l => l.SortOrder)
            .Select(l => new FooterLinkDto(l.ColumnFa, l.ColumnEn, l.LabelFa, l.LabelEn, l.Href, l.SortOrder))
            .ToListAsync(ct);
}

public sealed class GetAdminFooterLinksHandler(IQueryRepository<FooterLink> links)
    : IRequestHandler<GetAdminFooterLinksQuery, List<AdminFooterLinkDto>>
{
    public async Task<List<AdminFooterLinkDto>> Handle(GetAdminFooterLinksQuery request, CancellationToken ct) =>
        await links.TableNoTracking
            .OrderBy(l => l.ColumnEn).ThenBy(l => l.SortOrder)
            .Select(l => new AdminFooterLinkDto(
                l.Id, l.ColumnFa, l.ColumnEn, l.LabelFa, l.LabelEn, l.Href, l.SortOrder, l.IsActive))
            .ToListAsync(ct);
}

public sealed class CreateFooterLinkHandler(ICommandRepository<FooterLink> links)
    : IRequestHandler<CreateFooterLinkCommand, Guid>
{
    public async Task<Guid> Handle(CreateFooterLinkCommand request, CancellationToken ct)
    {
        var req = request.Request;
        var error = ContentRules.ValidateFooterLink(req);
        if (error is not null) throw new ValuationException(error);

        var entity = new FooterLink
        {
            ColumnFa = req.ColumnFa!.Trim(),
            ColumnEn = req.ColumnEn!.Trim(),
            LabelFa = req.LabelFa!.Trim(),
            LabelEn = req.LabelEn!.Trim(),
            Href = req.Href!.Trim(),
            SortOrder = req.SortOrder,
        };

        links.Add(entity, saveNow: false);
        await links.SaveChangesAsync(ct);
        return entity.Id;
    }
}

public sealed class UpdateFooterLinkHandler(ICommandRepository<FooterLink> links)
    : IRequestHandler<UpdateFooterLinkCommand, Guid?>
{
    public async Task<Guid?> Handle(UpdateFooterLinkCommand request, CancellationToken ct)
    {
        var entity = await links.Table.FirstOrDefaultAsync(l => l.Id == request.Id, ct);
        if (entity is null) return null;

        var req = request.Request;
        var error = ContentRules.ValidateFooterLink(req);
        if (error is not null) throw new ValuationException(error);

        entity.ColumnFa = req.ColumnFa!.Trim();
        entity.ColumnEn = req.ColumnEn!.Trim();
        entity.LabelFa = req.LabelFa!.Trim();
        entity.LabelEn = req.LabelEn!.Trim();
        entity.Href = req.Href!.Trim();
        entity.SortOrder = req.SortOrder;

        await links.SaveChangesAsync(ct);
        return entity.Id;
    }
}

public sealed class DeleteFooterLinkHandler(ICommandRepository<FooterLink> links)
    : IRequestHandler<DeleteFooterLinkCommand, bool>
{
    public async Task<bool> Handle(DeleteFooterLinkCommand request, CancellationToken ct)
    {
        var entity = await links.Table.FirstOrDefaultAsync(l => l.Id == request.Id, ct);
        if (entity is null) return false;

        links.Delete(entity); // soft delete via the DbContext's entity rules
        await links.SaveChangesAsync(ct);
        return true;
    }
}

// ─── Blog posts ─────────────────────────────────────────────────────────────

public sealed class GetPublicBlogHandler(IQueryRepository<BlogPost> posts)
    : IRequestHandler<GetPublicBlogQuery, List<BlogPostDto>>
{
    public async Task<List<BlogPostDto>> Handle(GetPublicBlogQuery request, CancellationToken ct)
    {
        var take = Math.Clamp(request.Take, 1, 24);
        return await posts.TableNoTracking
            .Where(p => p.IsActive && p.Status == BlogPostStatus.Published)
            .OrderByDescending(p => p.PublishedAt)
            .Take(take)
            .Select(p => new BlogPostDto(p.Id, p.Slug, p.TitleFa, p.TitleEn, p.SummaryFa, p.SummaryEn, p.PublishedAt))
            .ToListAsync(ct);
    }
}

public sealed class GetPublicBlogPostHandler(IQueryRepository<BlogPost> posts)
    : IRequestHandler<GetPublicBlogPostQuery, BlogFullDto?>
{
    public async Task<BlogFullDto?> Handle(GetPublicBlogPostQuery request, CancellationToken ct)
    {
        var post = await posts.TableNoTracking
            .FirstOrDefaultAsync(p => p.Slug == request.Slug && p.IsActive && p.Status == BlogPostStatus.Published, ct);

        return post is null
            ? null
            : new BlogFullDto(post.Id, post.Slug, post.TitleFa, post.TitleEn,
                post.SummaryFa, post.SummaryEn, post.BodyFa, post.BodyEn, post.PublishedAt);
    }
}

public sealed class GetAdminBlogHandler(IQueryRepository<BlogPost> posts)
    : IRequestHandler<GetAdminBlogQuery, List<AdminBlogDto>>
{
    public async Task<List<AdminBlogDto>> Handle(GetAdminBlogQuery request, CancellationToken ct) =>
        await posts.TableNoTracking
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new AdminBlogDto(
                p.Id, p.Slug, p.TitleFa, p.TitleEn, p.Status, p.PublishedAt, p.CreatedAt, p.IsActive))
            .ToListAsync(ct);
}

public sealed class GetAdminBlogPostHandler(IQueryRepository<BlogPost> posts)
    : IRequestHandler<GetAdminBlogPostQuery, AdminBlogFullDto?>
{
    public async Task<AdminBlogFullDto?> Handle(GetAdminBlogPostQuery request, CancellationToken ct)
    {
        var post = await posts.TableNoTracking.FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        return post is null
            ? null
            : new AdminBlogFullDto(post.Id, post.Slug, post.TitleFa, post.TitleEn,
                post.SummaryFa, post.SummaryEn, post.BodyFa, post.BodyEn,
                post.Status, post.PublishedAt, post.CreatedAt, post.IsActive);
    }
}

public sealed class CreateBlogPostHandler(
    ICommandRepository<BlogPost> posts,
    IQueryRepository<BlogPost> queries)
    : IRequestHandler<CreateBlogPostCommand, Guid>
{
    public async Task<Guid> Handle(CreateBlogPostCommand request, CancellationToken ct)
    {
        var req = request.Request;
        var error = ContentRules.ValidateBlog(req, out var slug);
        if (error is not null) throw new ValuationException(error);

        var duplicate = await queries.TableNoTracking.AnyAsync(p => p.Slug == slug, ct);
        if (duplicate) throw new ValuationException("این نامک (slug) قبلاً استفاده شده است.");

        var entity = new BlogPost
        {
            Slug = slug,
            TitleFa = req.TitleFa!.Trim(),
            TitleEn = req.TitleEn!.Trim(),
            SummaryFa = req.SummaryFa?.Trim() ?? "",
            SummaryEn = req.SummaryEn?.Trim() ?? "",
            BodyFa = BlogBody.Sanitize(req.BodyFa?.Trim() ?? ""),
            BodyEn = BlogBody.Sanitize(req.BodyEn?.Trim() ?? ""),
            PublishedAt = ContentRules.ClampedPublishedAt(req.PublishedAt),
        };

        posts.Add(entity, saveNow: false);
        await posts.SaveChangesAsync(ct);
        return entity.Id;
    }
}

public sealed class UpdateBlogPostHandler(
    ICommandRepository<BlogPost> posts,
    IQueryRepository<BlogPost> queries)
    : IRequestHandler<UpdateBlogPostCommand, Guid?>
{
    public async Task<Guid?> Handle(UpdateBlogPostCommand request, CancellationToken ct)
    {
        var entity = await posts.Table.FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (entity is null) return null;

        var req = request.Request;
        var error = ContentRules.ValidateBlog(req, out var slug);
        if (error is not null) throw new ValuationException(error);

        var duplicate = await queries.TableNoTracking.AnyAsync(p => p.Slug == slug && p.Id != request.Id, ct);
        if (duplicate) throw new ValuationException("این نامک (slug) قبلاً استفاده شده است.");

        entity.Slug = slug;
        entity.TitleFa = req.TitleFa!.Trim();
        entity.TitleEn = req.TitleEn!.Trim();
        entity.SummaryFa = req.SummaryFa?.Trim() ?? "";
        entity.SummaryEn = req.SummaryEn?.Trim() ?? "";
        entity.BodyFa = BlogBody.Sanitize(req.BodyFa?.Trim() ?? "");
        entity.BodyEn = BlogBody.Sanitize(req.BodyEn?.Trim() ?? "");
        entity.PublishedAt = ContentRules.ClampedPublishedAt(req.PublishedAt);

        await posts.SaveChangesAsync(ct);
        return entity.Id;
    }
}

public sealed class PublishBlogPostHandler(ICommandRepository<BlogPost> posts)
    : IRequestHandler<PublishBlogPostCommand, BlogPublishResult?>
{
    public async Task<BlogPublishResult?> Handle(PublishBlogPostCommand request, CancellationToken ct)
    {
        var entity = await posts.Table.FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (entity is null) return null;

        if (request.Publish && entity.Status == BlogPostStatus.Draft &&
            string.IsNullOrWhiteSpace(entity.BodyFa) && string.IsNullOrWhiteSpace(entity.BodyEn))
            throw new ValuationException("برای انتشار، متن پست لازم است (فارسی یا انگلیسی).");

        if (request.Publish)
        {
            entity.Publish();
            if (request.At is not null) entity.PublishedAt = ContentRules.ClampedPublishedAt(request.At);
        }
        else
        {
            entity.Unpublish();
        }

        await posts.SaveChangesAsync(ct);
        return new BlogPublishResult(entity.Id, entity.Status.ToString(), entity.PublishedAt);
    }
}

public sealed class DeleteBlogPostHandler(ICommandRepository<BlogPost> posts)
    : IRequestHandler<DeleteBlogPostCommand, bool>
{
    public async Task<bool> Handle(DeleteBlogPostCommand request, CancellationToken ct)
    {
        var entity = await posts.Table.FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (entity is null) return false;

        posts.Delete(entity); // soft delete
        await posts.SaveChangesAsync(ct);
        return true;
    }
}
