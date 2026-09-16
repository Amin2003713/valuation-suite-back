using MediatR;

namespace Application.Results.Commands.CalculateResult;

public class CalculateResultCommand : IRequest<Guid>
{
    public Guid AttemptId { get; set; }
}
