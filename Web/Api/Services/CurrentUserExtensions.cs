using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Web.Api.Services;

/// <summary>Extension helpers to read the authenticated user's identity from claims.</summary>
public static class CurrentUserExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? principal.FindFirstValue("sub");

        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    public static string GetEmail(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Email)
           ?? principal.FindFirstValue(JwtRegisteredClaimNames.Email)
           ?? string.Empty;
}
