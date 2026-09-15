using MediatR;
using Domain.Assessments;
using Domain.Attempts;
using Application.Common;

namespace Application.Assessments.Commands;

public class PublishAssessmentHandler : IRequestHandler<PublishAssessmentCommand>
{
    private readonly IAssessmentRepository _assessmentRepo;
    private readonly IAssessmentVersionRepository _versionRepo;
    private readonly IUnitOfWork _uow;

    public PublishAssessmentHandler(IAssessmentRepository assessmentRepo, IAssessmentVersionRepository versionRepo, IUnitOfWork uow)
    {
        _assessmentRepo = assessmentRepo;
        _versionRepo = versionRepo;
        _uow = uow;
    }

    public async Task Handle(PublishAssessmentCommand request, CancellationToken ct)
    {
        var assessment = await _assessmentRepo.GetByIdAsync(request.AssessmentId, ct)
            ?? throw new InvalidOperationException("Assessment not found");

        var version = await _versionRepo.GetByIdAsync(request.VersionId, ct)
            ?? throw new InvalidOperationException("Version not found");

        if (version.AssessmentId != request.AssessmentId)
            throw new InvalidOperationException("Version does not belong to assessment");
        if (version.IsDraft == false)
            throw new InvalidOperationException("Only draft versions can be published");

        assessment.PublishVersion(request.VersionId);
        await _uow.SaveChangesAsync(ct);
    }
}
