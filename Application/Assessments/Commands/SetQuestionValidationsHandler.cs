using MediatR;
using Domain.Assessments;
using Domain.Common;
using Application.Common;

namespace Application.Assessments.Commands;

public class SetQuestionValidationsHandler : IRequestHandler<SetQuestionValidationsCommand>
{
    private readonly IAssessmentVersionRepository _versionRepo;
    private readonly IUnitOfWork _uow;

    public SetQuestionValidationsHandler(IAssessmentVersionRepository versionRepo, IUnitOfWork uow)
    {
        _versionRepo = versionRepo;
        _uow = uow;
    }

    public async Task Handle(SetQuestionValidationsCommand request, CancellationToken ct)
    {
        var version = await _versionRepo.GetByIdAsync(request.QuestionId, ct)
            ?? throw new InvalidOperationException("Version not found");

        var question = version.Steps.SelectMany(s => s.Questions).FirstOrDefault(q => q.Id == request.QuestionId)
            ?? throw new InvalidOperationException("Question not found");

        question.SetValidations(request.Validations.Select(v => new ValidationRule
        {
            Field = v.Field,
            Type = v.Type,
            MinValue = v.MinValue,
            MaxValue = v.MaxValue,
            MinLength = v.MinLength,
            MaxLength = v.MaxLength,
            Pattern = v.Pattern,
            ErrorMessage = v.ErrorMessage
        }).ToList());
        await _uow.SaveChangesAsync(ct);
    }
}
