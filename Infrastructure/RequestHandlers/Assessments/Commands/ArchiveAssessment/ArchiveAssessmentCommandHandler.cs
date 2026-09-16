using Application.Assessments.Commands.ArchiveAssessment;

namespace RequestHandlers.Assessments.Commands.ArchiveAssessment;

public sealed class ArchiveAssessmentCommandHandler(
    IAssessmentCommandRepository assessmentRepository
) : IRequestHandler<ArchiveAssessmentCommand>
{
    public async Task Handle(ArchiveAssessmentCommand request, CancellationToken ct)
    {
        var assessment = await assessmentRepository.GetTrackedAsync(request.AssessmentId, ct)
            ?? throw ValuationException.NotFound("Assessment not found.");

        assessment.Archive();
        await assessmentRepository.SaveChangesAsync(ct);
    }
}
