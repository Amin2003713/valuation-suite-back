using Application.Interfaces.Base;
using Application.Messaging;
using Application.Security;
using Common.Exceptions;
using Domain.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace RequestHandlers.Messaging;

/// <summary>
///     Public contact form. Validates the payload, runs the phishing detector over
///     the free-text fields and quarantines anything that looks like an attack
///     instead of putting it in the team's inbox.
/// </summary>
public sealed class SubmitContactMessageHandler(ICommandRepository<ContactMessage> contactCommands)
    : IRequestHandler<SubmitContactMessageCommand, Guid>
{
    public async Task<Guid> Handle(SubmitContactMessageCommand request, CancellationToken ct)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var message = request.Message?.Trim() ?? string.Empty;
        var subject = string.IsNullOrWhiteSpace(request.Subject) ? null : request.Subject.Trim();

        if (name.Length is < 2 or > 120)
            throw new ValuationException("نام را کامل وارد کنید.");
        if (!LooksLikeEmail(email))
            throw new ValuationException("ایمیل معتبر وارد کنید.");
        if (message.Length is < 10 or > 4000)
            throw new ValuationException("متن پیام باید بین ۱۰ تا ۴۰۰۰ نویسه باشد.");
        if (!request.Consent)
            throw new ValuationException("برای ارسال پیام باید با حفظ اطلاعات ملاحظه کنید.");

        // Honeypot: real users never render "Website" (it is visually hidden for
        // assistive tech too), so anything typed there is a bot.
        var isSpam = !string.IsNullOrWhiteSpace(request.Website);

        var nameReport = ThreatDetector.Inspect(name);
        var messageReport = ThreatDetector.Inspect(message);
        var subjectReport = ThreatDetector.Inspect(subject);

        if (nameReport.HasMarkup || messageReport.HasMarkup || subjectReport.HasMarkup)
            throw new ValuationException("متن پیام شامل کد یا نشانه‌گذاری غیرمجاز است.");

        // Phishing is recorded, not rejected: the team needs to see what arrived
        // to block the sender. It is filed as spam so it never reaches the queue.
        if (messageReport.IsPhishing || subjectReport.IsPhishing)
            isSpam = true;

        var entity = new ContactMessage
        {
            Name = name,
            Email = email,
            Subject = subject,
            Message = message,
            ConsentGiven = true,
            IsSpam = isSpam,
            Status = isSpam ? ContactMessageStatus.Spam : ContactMessageStatus.New,
        };

        contactCommands.Add(entity, saveNow: false);
        await contactCommands.SaveChangesAsync(ct);

        return entity.Id;
    }

    private static bool LooksLikeEmail(string email) =>
        email.Length is > 3 and <= 254 && email.Contains('@') && !email.Contains(' ');
}

/// <summary>Public newsletter subscribe. Idempotent per lower-cased address.</summary>
public sealed class SubscribeNewsletterHandler(ICommandRepository<NewsletterSubscriber> subscriberCommands)
    : IRequestHandler<SubscribeNewsletterCommand, bool>
{
    public async Task<bool> Handle(SubscribeNewsletterCommand request, CancellationToken ct)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        if (email.Length is not (> 3 and <= 254) || !email.Contains('@') || email.Contains(' '))
            throw new ValuationException("ایمیل معتبر وارد کنید.");
        if (!request.Consent)
            throw new ValuationException("برای عضویت در خبرنامه باید با حفظ اطلاعات ملاحظه کنید.");

        // Addresses are user input too: a newsletter row must never carry markup.
        if (ThreatDetector.Inspect(email).HasMarkup)
            throw new ValuationException("ایمیل معتبر وارد کنید.");

        var emailKey = email.ToLowerInvariant();
        var existing = await subscriberCommands.Table
            .FirstOrDefaultAsync(s => s.EmailKey == emailKey, ct);

        if (existing is null)
        {
            subscriberCommands.Add(new NewsletterSubscriber
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

        await subscriberCommands.SaveChangesAsync(ct);
        return true;
    }
}
