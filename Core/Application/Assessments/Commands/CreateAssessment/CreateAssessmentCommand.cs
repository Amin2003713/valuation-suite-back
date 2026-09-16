using MediatR;

namespace Application.Assessments.Commands.CreateAssessment;

public class CreateAssessmentCommand : IRequest<Guid>
{
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? Description { get; set; }
    public Guid CompanyId { get; set; }
}
