using MediatR;

namespace Application.Assessments.Commands.AddStep;

public class AddStepCommand : IRequest<Guid>
{
    public Guid VersionId { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public int Order { get; set; }
}
