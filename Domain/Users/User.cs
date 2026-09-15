using Domain.Common;
using Domain.Companies;
using Domain.Attempts;
using Domain.Evaluation;

namespace Domain.Users;

public sealed class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Plan Plan { get; set; } = Plan.Free;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }
    public List<AssessmentAttempt> Attempts { get; set; } = [];

    public static User Create(string name, string email)
    {
        return new User
        {
            Name = name,
            Email = email
        };
    }

    public void SetPasswordHash(string hash) => PasswordHash = hash;
    public void SetCompany(Company company)
    {
        CompanyId = company.Id;
        Company = company;
    }
    public void UpgradeToPro() => Plan = Plan.Pro;
    public void UpdateLastLogin() => LastLoginAt = DateTime.UtcNow;
}
