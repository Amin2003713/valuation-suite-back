using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Web.Api.Services;

public class TokenService(IConfiguration configuration, ILogger<TokenService> logger)
{
    public (string Token, DateTime ExpiresAt) CreateToken(ApplicationUser user, IEnumerable<string>? roles = null)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured")));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var minutes = configuration.GetValue("Jwt:ExpiryMinutes", 60 * 24 * 14);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("name", user.DisplayName ?? user.UserName ?? string.Empty),
            new("plan", user.Plan.ToString()),
        };

        if (user.CompanyId is { } companyId)
            claims.Add(new Claim("companyId", companyId.ToString()));

        if (roles != null)
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: credentials);

        var serialized = new JwtSecurityTokenHandler().WriteToken(token);
        logger.LogDebug("Issued JWT for user {UserId} expiring {ExpiresAt}", user.Id, token.ValidTo);
        return (serialized, token.ValidTo);
    }
}
