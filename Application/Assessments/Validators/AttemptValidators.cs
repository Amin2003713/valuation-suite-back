using FluentValidation;
using Application.Attempts.Commands;

namespace Application.Assessments.Validators;

public class CreateAttemptCommandValidator : AbstractValidator<CreateAttemptCommand>
{
    public CreateAttemptCommandValidator()
    {
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}

public class CompleteAttemptCommandValidator : AbstractValidator<CompleteAttemptCommand>
{
    public CompleteAttemptCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
