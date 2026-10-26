using Common.Base;

namespace Domain.Adviser;

/// <summary>Author of an adviser-chat message.</summary>
public enum AdviserAuthor
{
    /// <summary>The user asked the question.</summary>
    User = 1,
    /// <summary>The (automated) adviser produced the answer.</summary>
    Adviser = 2,
}

/// <summary>
///     One message of the per-user, per-tool "Talk to the Adviser" chat.
///     Messages are persisted server-side so the conversation survives reloads
///     and is available on any device. The context snapshot (tool inputs/result
///     summaries at answer time) is stored with the answer for auditability.
/// </summary>
public sealed class AdviserMessage : BaseEntity
{
    public Guid UserId { get; set; }
    public string ToolCode { get; set; } = string.Empty;
    public AdviserAuthor Author { get; set; }
    public string Text { get; set; } = string.Empty;

    /// <summary>
    ///     Compact JSON context the answer was generated from (tool name,
    ///     latest submission summary, company/industry). Diagnostic only.
    /// </summary>
    public string? ContextJson { get; set; }
}
