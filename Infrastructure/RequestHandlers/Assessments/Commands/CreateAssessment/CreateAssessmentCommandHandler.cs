using Application.Assessments.Commands.CreateAssessment;

namespace RequestHandlers.Assessments.Commands.CreateAssessment;

public sealed class CreateAssessmentCommandHandler(
    IAssessmentCommandRepository assessmentRepository
) : IRequestHandler<CreateAssessmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateAssessmentCommand request, CancellationToken ct)
    {
        var assessment = Domain.Assessments.Assessment.Create(
            request.Name, request.Code, request.Description, request.CompanyId);

        await assessmentRepository.AddAsync(assessment, ct, saveNow: true);
        return assessment.Id;
    }
}
