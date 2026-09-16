using MediatR;

namespace Application.Assessments.Commands.ArchiveAssessment;

public class ArchiveAssessmentCommand : IRequest
{
    public Guid AssessmentId { get; set; }
}
