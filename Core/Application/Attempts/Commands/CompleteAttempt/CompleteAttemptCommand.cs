using MediatR;

namespace Application.Attempts.Commands.CompleteAttempt;

public class CompleteAttemptCommand : IRequest
{
    public Guid Id { get; set; }
}
