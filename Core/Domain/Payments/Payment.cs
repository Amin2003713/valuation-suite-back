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
