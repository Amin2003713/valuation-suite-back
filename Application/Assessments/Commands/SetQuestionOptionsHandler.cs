using MediatR;
using Domain.Assessments;
using Application.Common;

namespace Application.Assessments.Commands;

public class SetQuestionOptionsHandler : IRequestHandler<SetQuestionOptionsCommand>
{
    private readonly IAssessmentVersionRepository _versionRepo;
    private readonly IUnitOfWork _uow;

    public SetQuestionOptionsHandler(IAssessmentVersionRepository versionRepo, IUnitOfWork uow)
    {
        _versionRepo = versionRepo;
        _uow = uow;
    }

    public async Task Handle(SetQuestionOptionsCommand request, CancellationToken ct)
    {
        var version = await _versionRepo.GetByIdAsync(request.QuestionId, ct)
            ?? throw new InvalidOperationException("Version not found");

        var question = version.Steps.SelectMany(s => s.Questions).FirstOrDefault(q => q.Id == request.QuestionId)
            ?? throw new InvalidOperationException("Question not found");

        question.Options.Clear();
        foreach (var opt in request.Options)
        {
            question.AddOption(new Option
            {
                Label = opt.Label,
                Value = opt.Value,
                Order = opt.Order,
                IsCorrect = opt.IsCorrect,
                Score = opt.Score
            });
        }
        await _uow.SaveChangesAsync(ct);
    }
}
