using MediatR;
using Domain.Assessments;
using Application.Common;

namespace Application.Assessments.Commands;

public class ArchiveAssessmentHandler : IRequestHandler<ArchiveAssessmentCommand>
{
    private readonly IAssessmentRepository _repo;
    private readonly IUnitOfWork _uow;

    public ArchiveAssessmentHandler(IAssessmentRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(ArchiveAssessmentCommand request, CancellationToken ct)
    {
        var assessment = await _repo.GetByIdAsync(request.AssessmentId, ct)
            ?? throw new InvalidOperationException("Assessment not found");
        assessment.Archive();
        await _uow.SaveChangesAsync(ct);
    }
}
