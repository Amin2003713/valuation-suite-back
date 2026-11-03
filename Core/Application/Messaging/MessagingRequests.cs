using MediatR;

namespace Application.Messaging;

/* Public messaging commands. The controller used to validate length, consent,
 * e-mail shape and the honeypot inline; those rules — plus phishing detection —
 * now run here so every entry point shares one implementation. */

public sealed record SubmitContactMessageCommand(
    string? Name,
    string? Email,
    string? Subject,
    string? Message,
    bool Consent,
    string? Website = null) : IRequest<Guid>;

public sealed record SubscribeNewsletterCommand(
    string? Email,
    bool Consent,
    string? Source = null) : IRequest<bool>;
