using MediatR;

namespace Application.Attempts.Commands.SyncAnswers;

public class SyncAnswersCommand : IRequest<SyncAnswersResponse>
{
    public Guid AttemptId { get; set; }
    public List<AnswerData> Answers { get; set; } = [];
    public int ClientRevision { get; set; }
}

public record AnswerData(Guid QuestionId, string? TextValue, double? NumericValue, bool? BooleanValue, DateTime? DateValue, List<string>? ChoiceValues, string? JsonData);

public record SyncAnswersResponse(int SavedCount, int Conflicts, int NewRevision);
