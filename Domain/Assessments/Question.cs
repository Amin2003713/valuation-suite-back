using Domain.Common;
using Domain.Evaluation;

namespace Domain.Assessments;

public sealed class Question : BaseEntity
{
    public Guid StepId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Key { get; set; }
    public QuestionType Type { get; set; } = QuestionType.Text;
    public int Order { get; set; }
    public bool IsRequired { get; set; }
    public string? HelpText { get; set; }
    public List<Option> Options { get; set; } = [];
    public List<ValidationRule> Validations { get; set; } = [];
    public List<VisibilityCondition> VisibilityConditions { get; set; } = [];
    public CalculationConfig? Calculation { get; set; }
    public MathExpression? MathExpression { get; set; }
    public Step Step { get; set; } = null!;

    public void SetKey(string key) => Key = key;
    public void SetRequired(bool required) => IsRequired = required;
    public void SetHelpText(string helpText) => HelpText = helpText;
    public void AddOption(Option option) => Options.Add(option);
    public void RemoveOption(Guid optionId) => Options.RemoveAll(o => o.Id == optionId);
    public void AddValidation(ValidationRule validation) => Validations.Add(validation);
    public void SetValidations(List<ValidationRule> validations) => Validations = validations;
    public void SetVisibilityConditions(List<VisibilityCondition> conditions) => VisibilityConditions = conditions;
    public void SetCalculation(CalculationConfig calculation) => Calculation = calculation;
    public void SetMathExpression(MathExpression expression) => MathExpression = expression;
    public bool HasVisibilityConditions => VisibilityConditions.Any();
}
