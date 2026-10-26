using MediatR;

namespace Application.Adviser;

/// <summary>One adviser-chat message as returned by history/ask.</summary>
public record AdviserMessageRow(
    Guid Id,
    string Author,
    string Text,
    string CreatedAt);

public record AdviserAskResponse(IReadOnlyList<AdviserMessageRow> Messages);

/// <summary>
///     Asks the adviser one question in the context of a tool. The handler
///     builds context (latest submission result, company/area of work),
///     generates an answer, persists BOTH messages server-side and returns the
///     full thread for the tool.
/// </summary>
public sealed record AskAdviserCommand(string ToolCode, string Question) : IRequest<AdviserAskResponse>;

/// <summary>The caller's chat history for one tool (oldest first).</summary>
public sealed record GetAdviserHistoryQuery(string ToolCode) : IRequest<AdviserAskResponse>;

/// <summary>"New conversation" — clears the caller's history for one tool.</summary>
public sealed record ClearAdviserHistoryCommand(string ToolCode) : IRequest;
