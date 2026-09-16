using Application.Assessments.Responses;
using MediatR;

namespace Application.Assessments.Queries.GetAssessmentVersion;

public class GetAssessmentVersionQuery : IRequest<AssessmentVersionResponse?>
{
    public Guid Id { get; set; }
}

public class GetAssessmentVersionsQuery : IRequest<List<AssessmentVersionResponse>>
{
    public Guid AssessmentId { get; set; }
}

public class GetAssessmentForClientQuery : IRequest<AssessmentVersionResponse?>
{
    public Guid AssessmentId { get; set; }
}

public class GetAssessmentPreviewQuery : IRequest<AssessmentVersionResponse?>
{
    public Guid AssessmentId { get; set; }
    public Guid VersionId { get; set; }
}
