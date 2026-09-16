using Application.Attempts.Commands.SyncAnswers;
using Application.Assessments.Commands.AddQuestion;
using Application.Assessments.Commands.AddStep;
using Application.Assessments.Commands.CreateAssessment;
using Application.Assessments.Commands.CreateVersion;
using Application.Companies.Commands.CreateCompany;
using FluentValidation;

namespace Application.Assessments.Validators;

public class CreateAssessmentCommandValidator : AbstractValidator<CreateAssessmentCommand>
{
    public CreateAssessmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}

public class PublishAssessmentCommandValidator : AbstractValidator<PublishAssessmentCommand>
{
    public PublishAssessmentCommandValidator()
    {
        RuleFor(x => x.AssessmentId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
    }
}

public class AddStepCommandValidator : AbstractValidator<AddStepCommand>
{
    public AddStepCommandValidator()
    {
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}

public class AddQuestionCommandValidator : AbstractValidator<AddQuestionCommand>
{
    public AddQuestionCommandValidator()
    {
        RuleFor(x => x.StepId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100);
    }
}

public class SyncAnswersCommandValidator : AbstractValidator<SyncAnswersCommand>
{
    public SyncAnswersCommandValidator()
    {
        RuleFor(x => x.AttemptId).NotEmpty();
        RuleFor(x => x.Answers).NotEmpty();
        RuleFor(x => x.ClientRevision).GreaterThanOrEqualTo(0);
    }
}
