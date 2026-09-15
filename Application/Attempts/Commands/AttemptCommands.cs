using MediatR;
using Domain.Attempts;

namespace Application.Attempts.Commands;

public record CreateAttemptCommand(Guid VersionId, Guid UserId, Guid CompanyId) : IRequest<Guid>;
public record CompleteAttemptCommand(Guid Id) : IRequest;
public record AbandonAttemptCommand(Guid Id) : IRequest;
public record SyncAnswersCommand(Guid AttemptId, List<AnswerData> Answers, int ClientRevision) : IRequest<SyncAnswersResponse>;

public record AnswerData(Guid QuestionId, string? TextValue, double? NumericValue, bool? BooleanValue, DateTime? DateValue, List<string>? ChoiceValues, string? JsonData);

public record SyncAnswersResponse(int SavedCount, int Conflicts, int NewRevision);
