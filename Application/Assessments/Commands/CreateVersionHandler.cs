using MediatR;
using Domain.Assessments;
using Application.Common;

namespace Application.Assessments.Commands;

public class CreateVersionHandler : IRequestHandler<CreateVersionCommand, Guid>
{
    private readonly IAssessmentRepository _assessmentRepo;
    private readonly IAssessmentVersionRepository _versionRepo;
    private readonly IUnitOfWork _uow;

    public CreateVersionHandler(IAssessmentRepository assessmentRepo, IAssessmentVersionRepository versionRepo, IUnitOfWork uow)
    {
        _assessmentRepo = assessmentRepo;
        _versionRepo = versionRepo;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateVersionCommand request, CancellationToken ct)
    {
        var assessment = await _assessmentRepo.GetByIdAsync(request.AssessmentId, ct)
            ?? throw new InvalidOperationException("Assessment not found");

        assessment.CreateDraftVersion();
        await _uow.SaveChangesAsync(ct);

        var draft = await _versionRepo.GetCurrentDraftAsync(request.AssessmentId, ct);
        return draft!.Id;
    }
}
