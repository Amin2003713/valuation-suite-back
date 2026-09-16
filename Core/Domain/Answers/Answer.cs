using Domain.Common;
using Domain.Attempts;

namespace Domain.Answers;

public enum AnswerValueType
{
    Text = 1,
    LongText = 2,
    Number = 3,
    Decimal = 4,
    Boolean = 5,
    SingleChoice = 6,
    MultipleChoice = 7,
    Dropdown = 8,
    Date = 9,
    Rating = 10,
    Mathematics = 20,
    Information = 30,
    Conditional = 31
}

public sealed class Answer : BaseEntity
{
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public AnswerValueType ValueType { get; set; }
    public string? TextValue { get; set; }
    public double? NumericValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateTime? DateValue { get; set; }
    public List<string>? ChoiceValues { get; set; }
    public string? JsonData { get; set; }
    public int ClientRevision { get; set; }
    public DateTime SynchronizedAt { get; set; }
    public AssessmentAttempt Attempt { get; set; } = null!;

    public static Answer Create(Guid attemptId, Guid questionId, AnswerValueType valueType)
    {
        return new Answer
        {
            AttemptId = attemptId,
            QuestionId = questionId,
            ValueType = valueType
        };
    }

    public void SetTextValue(string value) { TextValue = value; SynchronizedAt = DateTime.UtcNow; }
    public void SetNumericValue(double value) { NumericValue = value; SynchronizedAt = DateTime.UtcNow; }
    public void SetBooleanValue(bool value) { BooleanValue = value; SynchronizedAt = DateTime.UtcNow; }
    public void SetDateValue(DateTime value) { DateValue = value; SynchronizedAt = DateTime.UtcNow; }
    public void SetChoiceValues(List<string> values) { ChoiceValues = values; SynchronizedAt = DateTime.UtcNow; }
    public void SetJsonData(string json) { JsonData = json; SynchronizedAt = DateTime.UtcNow; }
    public void IncrementRevision() => ClientRevision++;
}
