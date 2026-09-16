using MediatR;

namespace Application.Assessments.Commands.UpdateStep;

public class UpdateStepCommand : IRequest
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
}

public class ReorderStepsCommand : IRequest
{
    public Guid VersionId { get; set; }
    public List<Guid> StepIdsInOrder { get; set; } = [];
}

public class RemoveStepCommand : IRequest
{
    public Guid StepId { get; set; }
}
