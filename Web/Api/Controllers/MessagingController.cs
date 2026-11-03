using Application.Admin;
using Application.Interfaces;
using Application.Interfaces.Base;
using Application.Messaging;
using Common.Exceptions;
using Domain.Messaging;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.Api.Controllers;

/// <summary>
///     Public contact + newsletter endpoints, plus their admin management APIs.
///
///     The public actions contain no rules any more: validation, the honeypot,
///     consent and phishing screening live in
///     <see cref="RequestHandlers.Messaging.SubmitContactMessageHandler" /> and
///     <see cref="RequestHandlers.Messaging.SubscribeNewsletterHandler" />.
/// </summary>
[ApiController]
[Route("api/messaging")]
public class MessagingController(
    IMediator mediator,
    ICommandRepository<ContactMessage> contactCommands,
    IQueryRepository<ContactMessage> contactQueries,
    ICommandRepository<NewsletterSubscriber> subscribersCommands,
    IQueryRepository<NewsletterSubscriber> subscribersQueries) : ControllerBase
{
    public record ContactFormRequest(string? Name, string? Email, string? Subject, string? Message, bool Consent, string? Website = null);

    /// <summary>Public contact endpoint (no signup required).</summary>
    [HttpPost("contact")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitContact([FromBody] ContactFormRequest request, CancellationToken ct)
    {
        var id = await mediator.Send(
            new SubmitContactMessageCommand(request.Name, request.Email, request.Subject, request.Message, request.Consent, request.Website),
            ct);
        return Ok(new { id });
    }

    public record SubscribeNewsletterRequest(string Email, bool Consent, string? Source = null);

    /// <summary>Public newsletter subscribe. Idempotent per address (lower-case).</summary>
    [HttpPost("newsletter")]
    [AllowAnonymous]
    public async Task<IActionResult> SubscribeNewsletter([FromBody] SubscribeNewsletterRequest request, CancellationToken ct)
    {
        await mediator.Send(new SubscribeNewsletterCommand(request.Email, request.Consent, request.Source), ct);
        return Ok(new { subscribed = true });
    }

    // ─── Admin area ─────────────────────────────────────────────────────────

    /// <summary>Paged contact inbox (spam + resolved filtered individually).</summary>
    [HttpGet("admin/contacts")]
    [Authorize(Policy = "perm:messaging.read")]
    public async Task<IActionResult> AdminContacts(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = contactQueries.TableNoTracking;

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ContactMessageStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(m => m.Status == parsed);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip(Math.Max(0, (page - 1)) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Email,
                m.Subject,
                m.Message,
                m.Status,
                m.IsSpam,
                m.AdminNote,
                m.ResolvedAt,
                m.CreatedAt,
            })
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>Triage a contact message (status + optional internal note).</summary>
    public record TriageContactRequest(ContactMessageStatus Status, string? AdminNote = null);

    [HttpPut("admin/contacts/{id:guid}")]
    [Authorize(Policy = "perm:messaging.manage")]
    public async Task<IActionResult> TriageContact(Guid id, [FromBody] TriageContactRequest request, CancellationToken ct)
    {
        var entity = await contactCommands.Table
            .FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException("پیام یافت نشد");

        entity.Status = request.Status;
        if (request.AdminNote is not null) entity.AdminNote = request.AdminNote;
        if (request.Status == ContactMessageStatus.Resolved)
            entity.ResolvedAt = DateTime.UtcNow;

        await contactCommands.SaveChangesAsync(ct);
        return Ok(new { id = entity.Id, entity.Status });
    }

    /// <summary>Paged newsletter subscriber list.</summary>
    [HttpGet("admin/newsletter")]
    [Authorize(Policy = "perm:messaging.read")]
    public async Task<IActionResult> AdminNewsletter(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = subscribersQueries.TableNoTracking;

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip(Math.Max(0, (page - 1)) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.Email,
                s.Source,
                s.ConsentGiven,
                s.IsActive,
                s.CreatedAt,
            })
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>Unsubscribe an address (soft delete — history is kept).</summary>
    [HttpDelete("admin/newsletter/{id:guid}")]
    [Authorize(Policy = "perm:messaging.manage")]
    public async Task<IActionResult> Unsubscribe(Guid id, CancellationToken ct)
    {
        var entity = await subscribersCommands.Table
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("مشترک یافت نشد");

        entity.Unsubscribe();
        await subscribersCommands.SaveChangesAsync(ct);
        return Ok(new { id = entity.Id, isActive = false });
    }
}
