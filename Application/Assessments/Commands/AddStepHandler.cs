using MediatR;
using Domain.Assessments;
using Application.Common;

namespace Application.Assessments.Commands;

public class AddStepHandler : IRequestHandler<AddStepCommand, Guid>
{
    private readonly IAssessmentVersionRepository _versionRepo;
    private readonly IUnitOfWork _uow;

    public AddStepHandler(IAssessmentVersionRepository versionRepo, IUnitOfWork uow)
    {
        _versionRepo = versionRepo;
        _uow = uow;
    }

    public async Task<Guid> Handle(AddStepCommand request, CancellationToken ct)
    {
        var version = await _versionRepo.GetByIdAsync(request.VersionId, ct)
            ?? throw new InvalidOperationException("Version not found");

        var step = new Domain.Assessments.Step
        {
            VersionId = request.VersionId,
            Title = request.Title,
            Order = request.Order
        };
        version.AddStep(step);
        await _uow.SaveChangesAsync(ct);
        return step.Id;
    }
}
