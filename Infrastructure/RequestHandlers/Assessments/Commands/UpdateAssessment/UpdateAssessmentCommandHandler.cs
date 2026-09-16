using Application.Assessments.Commands.UpdateAssessment;

namespace RequestHandlers.Assessments.Commands.UpdateAssessment;

public sealed class UpdateAssessmentCommandHandler(
    IAssessmentCommandRepository assessmentRepository
) : IRequestHandler<UpdateAssessmentCommand>
{
    public async Task Handle(UpdateAssessmentCommand request, CancellationToken ct)
    {
        var assessment = await assessmentRepository.GetTrackedAsync(request.Id, ct)
            ?? throw ValuationException.NotFound("Assessment not found.");

        if (!string.IsNullOrWhiteSpace(request.Name))
            assessment.Name = request.Name;
        if (request.Description is not null)
            assessment.Description = request.Description;

        await assessmentRepository.UpdateAsync(assessment, ct, saveNow: true);
    }
}

public sealed class DeleteAssessmentCommandHandler(
    IAssessmentCommandRepository assessmentRepository
) : IRequestHandler<DeleteAssessmentCommand>
{
    public async Task Handle(DeleteAssessmentCommand request, CancellationToken ct)
    {
        var assessment = await assessmentRepository.GetTrackedAsync(request.Id, ct)
            ?? throw ValuationException.NotFound("Assessment not found.");

        assessment.Archive();

        await assessmentRepository.SaveChangesAsync(ct);
    }
}
