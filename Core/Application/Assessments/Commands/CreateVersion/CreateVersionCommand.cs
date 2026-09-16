using MediatR;

namespace Application.Assessments.Commands.CreateVersion;

public class CreateVersionCommand : IRequest<Guid>
{
    public Guid AssessmentId { get; set; }
}

public class PublishAssessmentCommand : IRequest
{
    public Guid AssessmentId { get; set; }
    public Guid VersionId { get; set; }
}
