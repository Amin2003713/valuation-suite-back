using Common.Base;

namespace Domain.Users;

/// <summary>
///     A purchase grant for one tool: unlocks the tool's *advanced* result sections.
///     Basic results of every tool are always free. A grant either has an expiry
///     (time-limited) or is perpetual — both may coexist per user/tool.
/// </summary>
public sealed class UserToolAccess : BaseEntity
{
    public Guid UserId { get; set; }
    public string ToolCode { get; set; } = string.Empty;

    /// <summary>Null = perpetual; otherwise the grant dies at this instant (UTC).</summary>
    public DateTime? ExpiresAt { get; set; }

    public Guid? PaymentId { get; set; }
    public Guid? GrantedByUserId { get; set; }

    public new bool IsActive(DateTime utcNow) =>
        ExpiresAt is null || ExpiresAt > utcNow;

    public static UserToolAccess Grant(
        Guid userId, string toolCode, DateTime? expiresAt, Guid? paymentId = null, Guid? grantedBy = null) =>
        new() { UserId = userId, ToolCode = toolCode, ExpiresAt = expiresAt, PaymentId = paymentId, GrantedByUserId = grantedBy };
}

/// <summary>
///     A purchasable bundle the user can compose freely: "pay for N tool unlocks,
///     any tools you like" (Pick-N) and/or "these exact tools" (curated).
///     Price is in Toman.
/// </summary>
public sealed class AccessPackage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>How many tool unlocks the buyer may spend on any tools they choose. Null = fixed list only.</summary>
    public int? PickCount { get; set; }

    /// <summary>Fixed tool codes always included in this bundle.</summary>
    public string ToolCodesJson { get; set; } = "[]";

    /// <summary>Days each unlock lasts; null = perpetual.</summary>
    public int? DurationDays { get; set; }

    public long PriceToman { get; set; }
    public new bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public List<string> ToolCodes()
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(ToolCodesJson) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static AccessPackage Create(
        string name, string? description, int? pickCount, List<string> toolCodes,
        int? durationDays, long priceToman, int sortOrder = 0) =>
        new()
        {
            Name = name,
            Description = description,
            PickCount = pickCount,
            ToolCodesJson = System.Text.Json.JsonSerializer.Serialize(toolCodes),
            DurationDays = durationDays,
            PriceToman = priceToman,
            SortOrder = sortOrder,
        };
}
