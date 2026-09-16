using MediatR;

namespace Application.Assessments.Commands.UpdateAssessment;

public class UpdateAssessmentCommand : IRequest
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class DeleteAssessmentCommand : IRequest
{
    public Guid Id { get; set; }
}
