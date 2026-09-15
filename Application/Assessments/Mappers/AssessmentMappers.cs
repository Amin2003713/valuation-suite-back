using Application.Assessments.Responses;
using Domain.Assessments;
using Domain.Results;
using Domain.Attempts;
using Domain.Answers;
using Domain.Companies;

namespace Application.Assessments.Mappers;

public static class AssessmentMappers
{
    public static AssessmentResponse ToResponse(this Assessment a) => new(
        a.Id, a.Name, a.Code, a.Description, a.CompanyId,
        a.IsPublished, a.IsArchived, a.PublishedVersion,
        a.Versions.Count, a.CreatedAt
    );

    public static AssessmentVersionResponse ToVersionResponse(this AssessmentVersion v) => new(
        v.Id, v.AssessmentId, v.VersionNumber, v.Title,
        v.IsDraft, v.IsPublished, v.PublishedAt,
        v.Steps.Count, v.Steps.Sum(s => s.Questions.Count)
    );

    public static StepResponse ToStepResponse(this Step s) => new(
        s.Id, s.VersionId, s.Title, s.Description, s.Order, s.Questions.Count
    );

    public static QuestionResponse ToQuestionResponse(this Question q) => new(
        q.Id, q.StepId, q.Text, q.Key, q.Type, q.Order, q.IsRequired,
        q.HelpText, q.Options.Select(o => new OptionResponse(o.Id, o.Label, o.Value, o.Order, o.IsCorrect, o.Score)).ToList(),
        q.Validations.Select(v => new ValidationRuleResponse(v.Field, v.Type, v.MinValue, v.MaxValue, v.MinLength, v.MaxLength, v.Pattern, v.ErrorMessage)).ToList(),
        q.VisibilityConditions.Select(c => new VisibilityConditionResponse(c.TargetQuestionId, c.Type, c.Value)).ToList(),
        q.Calculation == null ? null : new CalculationConfigResponse(q.Calculation.Expression, q.Calculation.InputQuestionIds, q.Calculation.OutputKey, q.Calculation.Formula, q.Calculation.Weights, q.Calculation.ResultLabel),
        q.MathExpression == null ? null : new MathExpressionResponse(q.MathExpression.Expression, q.MathExpression.Variables.Select(v => new MathVariableResponse(v.Name, v.Label, v.MinValue, v.MaxValue, v.DefaultValue, v.IsRequired)).ToList(), q.MathExpression.Operations.Select(o => new MathOperationResponse(o.Type, o.Operand, o.Constant)).ToList(), q.MathExpression.PostfixNotation)
    );

    public static AttemptResponse ToAttemptResponse(this AssessmentAttempt a) => new(
        a.Id, a.VersionId, a.UserId, a.CompanyId, a.Status,
        a.TotalSteps, a.CompletedSteps, a.StartedAt, a.CompletedAt,
        a.ClientRevision, a.Answers.Count
    );

    public static AnswerResponse ToAnswerResponse(this Answer a) => new(
        a.Id, a.AttemptId, a.QuestionId, a.ValueType,
        a.TextValue, a.NumericValue, a.BooleanValue, a.DateValue,
        a.ChoiceValues, a.ClientRevision, a.SynchronizedAt
    );

    public static AssessmentResultResponse ToResultResponse(this AssessmentResult r) => new(
        r.Id, r.AttemptId, r.AssessmentId, r.OverallScore,
        r.Level, r.LevelLabel, r.IsCalculated, r.CalculatedAt,
        r.StepScores, r.ScoredAnswers.Select(sa => new ScoredAnswerResponse(sa.QuestionId, sa.QuestionKey, sa.QuestionType, sa.Score, sa.MaxScore, sa.CalculatedValue)).ToList()
    );

    public static CompanyResponse ToCompanyResponse(this Company c) => new(
        c.Id, c.Name, c.Slug, c.LogoUrl, c.Industry, c.Plan, c.IsActive, c.Assessments.Count
    );
}
