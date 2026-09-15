using MediatR;
using Domain.Assessments;
using Domain.Attempts;
using Domain.Answers;
using Domain.Companies;
using Domain.Users;
using Application.Common;

namespace Application.Assessments.Commands;

public class CreateAssessmentHandler : IRequestHandler<CreateAssessmentCommand, Guid>
{
    private readonly IAssessmentRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateAssessmentHandler(IAssessmentRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateAssessmentCommand request, CancellationToken ct)
    {
        var assessment = Assessment.Create(request.Name, request.Code, request.Description, request.CompanyId);
        await _repo.AddAsync(assessment, ct);
        await _uow.SaveChangesAsync(ct);
        return assessment.Id;
    }
}
