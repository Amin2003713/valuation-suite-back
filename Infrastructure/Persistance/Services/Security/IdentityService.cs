using Microsoft.AspNetCore.Http;

namespace Persistence.Services.Security;

/// <summary>
///     Provides information about the current user for auditing and authorization,
///     same role as <c>Persistence.Services.Security.IdentityService</c> in the reference architecture.
/// </summary>
public class IdentityService(IHttpContextAccessor accessor)
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string? GetClaimValue(string claimType)
        => Principal?.FindFirst(claimType)?.Value;

    public string? GetClaimValue(params string[] claimTypes)
    {
        if (Principal == null || claimTypes.Length == 0)
            return null;

        return claimTypes
            .Select(t => Principal.Claims
                .FirstOrDefault(c => string.Equals(c.Type, t, StringComparison.OrdinalIgnoreCase))?.Value)
            .FirstOrDefault(val => !string.IsNullOrWhiteSpace(val));
    }

    public Guid? GetUserId()
    {
        var raw = GetClaimValue("id", ClaimTypes.NameIdentifier, "sub");
        return Guid.TryParse(raw, out var id) && id != Guid.Empty ? id : null;
    }

    public Guid? GetCompanyId()
    {
        var raw = GetClaimValue("companyId", "company");
        return Guid.TryParse(raw, out var id) && id != Guid.Empty ? id : null;
    }

    public string GetUserName()
        => GetClaimValue("username", ClaimTypes.Name) ?? string.Empty;

    public string GetEmail()
        => GetClaimValue(ClaimTypes.Email, "email") ?? string.Empty;

    public IReadOnlyList<string> GetRoles()
    {
        if (Principal == null) return Array.Empty<string>();

        return Principal.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type.Equals("role", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool HasRole(string roleName)
        => !string.IsNullOrWhiteSpace(roleName) &&
           GetRoles().Any(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
}
