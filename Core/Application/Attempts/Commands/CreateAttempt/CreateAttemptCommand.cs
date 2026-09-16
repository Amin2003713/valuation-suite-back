using MediatR;

namespace Application.Attempts.Commands.CreateAttempt;

public class CreateAttemptCommand : IRequest<Guid>
{
    public Guid VersionId { get; set; }
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
}
