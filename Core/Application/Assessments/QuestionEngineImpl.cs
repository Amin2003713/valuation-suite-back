using Domain.Answers;
using Domain.Assessments;
using Domain.Common;
using Domain.Results;

namespace Application.Assessments;

public class QuestionEngine : IQuestionEngine
{
    public (int OverallScore, string? Level, string? LevelLabel, Dictionary<string, int> StepScores, List<ScoredAnswer> ScoredAnswers) CalculateResults(
        AssessmentVersion version, List<Answer> answers)
    {
        var stepScores = new Dictionary<string, int>();
        var scoredAnswers = new List<ScoredAnswer>();

        foreach (var step in version.Steps)
        {
            int stepTotal = 0;
            int stepMax = 0;

            foreach (var question in step.Questions)
            {
                var answer = answers.FirstOrDefault(a => a.QuestionId == question.Id);
                var (score, max) = ScoreQuestion(question, answer);

                stepTotal += score;
                stepMax += max;

                scoredAnswers.Add(new ScoredAnswer
                {
                    QuestionId = question.Id,
                    QuestionKey = question.Key ?? question.Id.ToString(),
                    QuestionType = question.Type,
                    Score = score,
                    MaxScore = max,
                    CalculatedValue = answer?.NumericValue
                });
            }

            if (stepMax > 0)
                stepScores[step.Title] = (int)Math.Round((double)stepTotal / stepMax * 100);
        }

        var overallScore = stepScores.Count > 0 ? (int)Math.Round(stepScores.Values.Average()) : 0;
        var (level, levelLabel) = GetLevel(overallScore);

        return (overallScore, level, levelLabel, stepScores, scoredAnswers);
    }

    private (int Score, int MaxScore) ScoreQuestion(Question question, Answer? answer)
    {
        return question.Type switch
        {
            QuestionType.SingleChoice or QuestionType.Dropdown => ScoreChoice(question, answer),
            QuestionType.MultipleChoice => ScoreMultipleChoice(question, answer),
            QuestionType.Boolean => ScoreBoolean(question, answer),
            QuestionType.Number or QuestionType.Decimal => ScoreNumber(question, answer),
            QuestionType.Rating => ScoreRating(question, answer),
            QuestionType.Mathematics => ScoreMath(question, answer),
            QuestionType.Conditional => ScoreConditional(question, answer),
            _ => ScoreText(question, answer)
        };
    }

    private static (int, int) ScoreChoice(Question question, Answer? answer)
    {
        var max = question.Options.Count > 0 ? (int)(question.Options.Max(o => o.Score ?? 100)) : 100;
        if (answer?.ChoiceValues == null || !answer.ChoiceValues.Any())
            return (0, max);

        var selectedValue = answer.ChoiceValues.First();
        var option = question.Options.FirstOrDefault(o => o.Value == selectedValue || o.Label == selectedValue);
        return ((int)(option?.Score ?? 50), max);
    }

    private static (int, int) ScoreMultipleChoice(Question question, Answer? answer)
    {
        var max = question.Options.Count * 100;
        if (answer?.ChoiceValues == null || !answer.ChoiceValues.Any())
            return (0, max);

        var correctCount = answer.ChoiceValues.Count(v =>
            question.Options.Any(o => (o.Value == v || o.Label == v) && o.IsCorrect));
        var totalCorrect = question.Options.Count(o => o.IsCorrect);
        if (totalCorrect == 0) return (0, max);

        return ((int)Math.Round((double)correctCount / totalCorrect * max), max);
    }

    private static (int, int) ScoreBoolean(Question question, Answer? answer)
    {
        var max = 100;
        if (answer?.BooleanValue == null) return (0, max);
        var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
        var expected = correctOption?.Value?.ToLower() == "yes" || correctOption?.Label?.Contains("بله") == true
            ? true
            : correctOption?.Value?.ToLower() == "no" || correctOption?.Label?.Contains("خیر") == true
                ? false
                : (bool?)null;
        return (answer.BooleanValue == expected ? max : 0, max);
    }

    private static (int, int) ScoreNumber(Question question, Answer? answer)
    {
        var max = 100;
        if (answer?.NumericValue == null) return (0, max);
        var validations = question.Validations.FirstOrDefault(v => v.Type == ValidationType.Range);
        if (validations != null && answer.NumericValue >= validations.MinValue && answer.NumericValue <= validations.MaxValue)
            return (max, max);
        return (50, max);
    }

    private static (int, int) ScoreRating(Question question, Answer? answer)
    {
        var max = 100;
        if (answer?.NumericValue == null) return (0, max);
        var val = (int)answer.NumericValue;
        return (val * 10, max); // Rating 0-10 → 0-100
    }

    private static (int, int) ScoreMath(Question question, Answer? answer)
    {
        var max = 100;
        if (answer?.NumericValue == null) return (0, max);
        return (Math.Min((int)answer.NumericValue, max), max);
    }

    private static (int, int) ScoreConditional(Question question, Answer? answer)
    {
        var max = 100;
        if (answer?.BooleanValue == null) return (0, max);
        var visibleCondition = question.VisibilityConditions.FirstOrDefault();
        if (visibleCondition == null) return (answer.BooleanValue.Value ? max : 0, max);
        return (max, max);
    }

    private static (int, int) ScoreText(Question question, Answer? answer)
    {
        var max = 100;
        if (string.IsNullOrEmpty(answer?.TextValue)) return (0, max);
        return (max, max); // Text questions: presence = full marks
    }

    private static (string? Level, string? LevelLabel) GetLevel(int score)
    {
        return score switch
        {
            >= 80 => ("عالی", "عالی"),
            >= 60 => ("خوب", "خوب"),
            >= 40 => ("متوسط", "متوسط"),
            _ => ("ضعیف", "ضعیف")
        };
    }
}
