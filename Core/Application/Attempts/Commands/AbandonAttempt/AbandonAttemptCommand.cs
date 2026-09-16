using MediatR;

namespace Application.Attempts.Commands.AbandonAttempt;

public class AbandonAttemptCommand : IRequest
{
    public Guid Id { get; set; }
}
