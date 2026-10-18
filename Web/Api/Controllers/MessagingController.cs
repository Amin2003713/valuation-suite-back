using Application.Admin;
using Application.Interfaces;
using Application.Interfaces.Base;
using Common.Exceptions;
using Domain.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Api.Services;

namespace Web.Api.Controllers;

/// <summary>
///     Public contact + newsletter endpoints, plus their admin management APIs.
///     Strictly additive: touches nothing in payments, pricing or tool logic.
/// </summary>
[ApiController]
[Route("api/messaging")]
public class MessagingController(
    ICommandRepository<ContactMessage> contactCommands,
    IQueryRepository<ContactMessage> contactQueries,
    ICommandRepository<NewsletterSubscriber> subscribersCommands,
    IQueryRepository<NewsletterSubscriber> subscribersQueries) : ControllerBase
{
    public record SubmitContactRequest(string Name, string Email, string? Subject, string Message, bool Consent);

    /// <summary>
    ///     Public contact endpoint (no signup required). A hidden "website"
    ///     field (honeypot) marks obvious bots as spam instead of persisting
    ///     them into the team's inbox queue.
    /// </summary>
    [HttpPost("contact")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitContact([FromBody] ContactFormRequest request, CancellationToken ct)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var message = request.Message?.Trim() ?? string.Empty;

        if (name.Length is < 2 or > 120)
            throw new ValuationException("نام را کامل وارد کنید.");
        if (!IsValidEmail(email))
            throw new ValuationException("ایمیل معتبر وارد کنید.");
        if (message.Length is < 10 or > 4000)
            throw new ValuationException("متن پیام باید بین ۱۰ تا ۴۰۰۰ نویسه باشد.");
        if (!request.Consent)
            throw new ValuationException("برای ارسال پیام باید با حفظ اطلاعات ملاحظه کنید.");

        // Honeypot: real users never render "Website" (it is visually hidden for AT
        // too); anything typed there is treated as spam but recorded for the filter.
        var isSpam = !string.IsNullOrWhiteSpace(request.Website);

        var entity = new ContactMessage
        {
            Name = name,
            Email = email,
            Subject = string.IsNullOrWhiteSpace(request.Subject) ? null : request.Subject.Trim(),
            Message = message,
            ConsentGiven = true,
            IsSpam = isSpam,
            Status = isSpam ? ContactMessageStatus.Spam : ContactMessageStatus.New,
        };

        contactCommands.Add(entity, saveNow: false);
        await contactCommands.SaveChangesAsync(ct);

        return Ok(new { id = entity.Id });
    }

    public record ContactFormRequest(string? Name, string? Email, string? Subject, string? Message, bool Consent, string? Website = null);

    public record SubscribeNewsletterRequest(string Email, bool Consent, string? Source = null);

    /// <summary>Public newsletter subscribe. Idempotent per address (lower-case).</summary>
    [HttpPost("newsletter")]
    [AllowAnonymous]
    public async Task<IActionResult> SubscribeNewsletter([FromBody] SubscribeNewsletterRequest request, CancellationToken ct)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        if (!IsValidEmail(email))
            throw new ValuationException("ایمیل معتبر وارد کنید.");
        if (!request.Consent)
            throw new ValuationException("برای عضویت در خبرنامه باید با حفظ اطلاعات ملاحظه کنید.");

        var emailKey = email.ToLowerInvariant();

        var existing = await subscribersCommands.Table
            .FirstOrDefaultAsync(s => s.EmailKey == emailKey, ct);

        if (existing is null)
        {
            subscribersCommands.Add(new NewsletterSubscriber
            {
                Email = email,
                EmailKey = emailKey,
                Source = string.IsNullOrWhiteSpace(request.Source) ? "footer" : request.Source.Trim(),
                ConsentGiven = true,
            }, saveNow: false);
        }
        else
        {
            existing.Resubscribe();
        }

        await subscribersCommands.SaveChangesAsync(ct);
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

    private static bool IsValidEmail(string email) =>
        // Minimal sanity — full validation is the sender's concern.
        email.Length is > 3 and <= 254 && email.Contains('@') && !email.Contains(' ');
}
