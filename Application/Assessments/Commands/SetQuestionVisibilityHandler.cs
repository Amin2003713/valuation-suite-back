using MediatR;
using Domain.Assessments;
using Domain.Common;
using Application.Common;

namespace Application.Assessments.Commands;

public class SetQuestionVisibilityHandler : IRequestHandler<SetQuestionVisibilityCommand>
{
    private readonly IAssessmentVersionRepository _versionRepo;
    private readonly IUnitOfWork _uow;

    public SetQuestionVisibilityHandler(IAssessmentVersionRepository versionRepo, IUnitOfWork uow)
    {
        _versionRepo = versionRepo;
        _uow = uow;
    }

    public async Task Handle(SetQuestionVisibilityCommand request, CancellationToken ct)
    {
        var version = await _versionRepo.GetByIdAsync(request.QuestionId, ct)
            ?? throw new InvalidOperationException("Version not found");

        var question = version.Steps.SelectMany(s => s.Questions).FirstOrDefault(q => q.Id == request.QuestionId)
            ?? throw new InvalidOperationException("Question not found");

        question.SetVisibilityConditions(request.Conditions.Select(c => new VisibilityCondition
        {
            TargetQuestionId = c.TargetQuestionId,
            Type = c.Type,
            Value = c.Value
        }).ToList());
        await _uow.SaveChangesAsync(ct);
    }
}
