using MediatR;
using Domain.Assessments;
using Application.Common;

namespace Application.Assessments.Commands;

public class AddQuestionHandler : IRequestHandler<AddQuestionCommand, Guid>
{
    private readonly IAssessmentVersionRepository _versionRepo;
    private readonly IUnitOfWork _uow;

    public AddQuestionHandler(IAssessmentVersionRepository versionRepo, IUnitOfWork uow)
    {
        _versionRepo = versionRepo;
        _uow = uow;
    }

    public async Task<Guid> Handle(AddQuestionCommand request, CancellationToken ct)
    {
        var version = await _versionRepo.GetByIdAsync(request.StepId, ct)
            ?? throw new InvalidOperationException("Version not found");

        var step = version.Steps.FirstOrDefault(s => s.Order == request.Order)
            ?? throw new InvalidOperationException("Step not found");

        var question = new Domain.Assessments.Question
        {
            StepId = step.Id,
            Text = request.Text,
            Type = request.Type,
            Order = request.Order
        };
        step.AddQuestion(question);
        await _uow.SaveChangesAsync(ct);
        return question.Id;
    }
}
