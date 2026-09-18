using Common.Base;

namespace Domain.Payments;

public sealed class Payment : BaseEntity
{
    public Guid UserId { get; set; }
    /// <summary>Amount in Toman (IRT).</summary>
    public long Amount { get; set; }
    public string Gateway { get; set; } = "Zarinpal";
    public string Authority { get; set; } = string.Empty;
    public string? RefId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? Description { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? PaidAt { get; set; }

    /// <summary>What this payment buys when verified.</summary>
    public PaymentKind Kind { get; set; } = PaymentKind.ProPlan;

    /// <summary>Subscription length for Kind=ProPlan (months); null = default from config.</summary>
    public int? Months { get; set; }

    /// <summary>Tool code for Kind=ToolAdvanced, null otherwise.</summary>
    public string? ToolCode { get; set; }

    /// <summary>Package id for Kind=Package, null otherwise.</summary>
    public Guid? PackageId { get; set; }

    /// <summary>Submission id for Kind=Advice, null otherwise.</summary>
    public Guid? SubmissionId { get; set; }

    public static Payment Create(Guid userId, long amount, string description) => new()
    {
        UserId = userId,
        Amount = amount,
        Description = description,
    };

    public void MarkAwaiting(string authority)
    {
        Authority = authority;
        Status = PaymentStatus.AwaitingGateway;
    }

    public void MarkPaid(string? refId)
    {
        Status = PaymentStatus.Paid;
        RefId = refId;
        PaidAt = DateTime.UtcNow;
    }

    public void MarkFailed(string? error)
    {
        Status = PaymentStatus.Failed;
        ErrorMessage = error;
    }
}

public enum PaymentStatus
{
    Pending = 1,
    AwaitingGateway = 2,
    Paid = 3,
    Failed = 4,
    Cancelled = 5,
}

/// <summary>What a payment purchases. The callback grants entitlements by kind.</summary>
public enum PaymentKind
{
    /// <summary>Pro subscription (existing behaviour).</summary>
    ProPlan = 1,
    /// <summary>Unlock one tool's advanced results.</summary>
    ToolAdvanced = 2,
    /// <summary>A bundle of tool unlocks (fixed list and/or pick-N credits).</summary>
    Package = 3,
    /// <summary>Paid adviser review of one tool submission.</summary>
    Advice = 4,
}
