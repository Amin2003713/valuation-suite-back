using MediatR;

namespace Application.Attempts.Commands.CreateAttempt;

/// <summary>
///     Starts an attempt for the <b>acting</b> user. Owner and company are resolved
///     from the authenticated principal in the handler, so they are deliberately not
///     part of the request payload.
/// </summary>
public class CreateAttemptCommand : IRequest<Guid>
{
    public Guid VersionId { get; set; }
}
