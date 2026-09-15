using Domain.Assessments;
using Domain.Evaluation;
using Domain.Answers;
using Domain.Results;

namespace Application.Assessments;

public interface IQuestionEngine
{
    (int OverallScore, string? Level, string? LevelLabel, Dictionary<string, int> StepScores, List<ScoredAnswer> ScoredAnswers) CalculateResults(
        AssessmentVersion version, List<Answer> answers);
}

public interface IMathEngine
{
    double Evaluate(string expression, Dictionary<string, double> variables);
    bool ValidateExpression(string expression, out string? error);
}
