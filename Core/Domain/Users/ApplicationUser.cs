using Common.Base;
using Domain.Attempts;
using Domain.Companies;
using Domain.Evaluation;
using Microsoft.AspNetCore.Identity;

namespace Domain.Users;

/// <summary>
///     Application user backed by ASP.NET Core Identity.
///     Inherits IdentityUser (Id, UserName, Email, PasswordHash, ...) and adds valuation-specific
///     data. The Guid-keyed contract required by the audit pipeline is preserved.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>, IEntity
{
    public string DisplayName { get; set; } = string.Empty;
    public Plan Plan { get; set; } = Plan.Free;
    public DateTime? PlanExpiresAt { get; set; }
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    public List<AssessmentAttempt> Attempts { get; set; } = [];

    public bool IsPro =>
        Plan == Plan.Pro && (PlanExpiresAt is null || PlanExpiresAt > DateTime.UtcNow);

    public void UpgradeToPro(DateTime? expiresAt = null)
    {
        Plan = Plan.Pro;
        PlanExpiresAt = expiresAt;
    }

    public void DowngradeToFree()
    {
        Plan = Plan.Free;
        PlanExpiresAt = null;
    }

    public void UpdateLastLogin() => LastLoginAt = DateTime.UtcNow;
}
