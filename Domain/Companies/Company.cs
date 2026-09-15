using Domain.Common;
using Domain.Evaluation;
using Domain.Assessments;

namespace Domain.Companies;

public sealed class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Industry { get; set; }
    public Plan Plan { get; set; } = Plan.Free;
    public bool IsActive { get; set; } = true;
    public List<Assessment> Assessments { get; set; } = [];

    public static Company Create(string name, string slug)
    {
        return new Company
        {
            Name = name,
            Slug = slug
        };
    }

    public void SetIndustry(string industry) => Industry = industry;
    public void SetLogo(string url) => LogoUrl = url;
    public void UpgradeToPro() => Plan = Plan.Pro;
    public void DowngradeToFree() => Plan = Plan.Free;
}

public enum Plan
{
    Free = 1,
    Pro = 2
}
