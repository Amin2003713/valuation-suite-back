using Common.Base;

namespace Domain.Portfolio;

public enum IpAssetKind
{
    Patent = 1,
    Trademark = 2,
    Software = 3,
    KnowHow = 4,
    Data = 5,
    Brand = 6,
    Other = 9,
}

public enum IpAssetStatus
{
    Pending = 1,
    Granted = 2,
    Expired = 3,
    Abandoned = 4,
}

/// <summary>
///     One IP asset a company owns. The register is company-scoped (not user-scoped)
///     so every member of the company sees the same portfolio.
/// </summary>
public sealed class IpAsset : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid CreatedByUserId { get; set; }

    public IpAssetKind Kind { get; set; } = IpAssetKind.Other;
    public string Title { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? Jurisdiction { get; set; }
    public IpAssetStatus Status { get; set; } = IpAssetStatus.Granted;
    public DateTime? FilingDate { get; set; }
    public DateTime? GrantDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
///     One point on an asset's valuation history. <see cref="SubmissionId"/> links the
///     stored tool run the figure came from; the amount itself is what the user read
///     from that result and confirmed, because engines report different units ($mm, $k,
///     Toman, index) and silently converting them would invent numbers.
/// </summary>
public sealed class AssetValuation : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid AssetId { get; set; }
    public string ToolCode { get; set; } = string.Empty;
    public Guid? SubmissionId { get; set; }
    public decimal ValueAmount { get; set; }
    /// <summary>IRR | IRR_MILLION | USD | USD_MILLION | INDEX — displayed as stored.</summary>
    public string Unit { get; set; } = "IRR";
    public DateTime AsOfDate { get; set; } = DateTime.UtcNow;
    public string? Method { get; set; }
    public double? OverallScore { get; set; }
    public bool IsPrimary { get; set; }
    public string? Note { get; set; }
}
