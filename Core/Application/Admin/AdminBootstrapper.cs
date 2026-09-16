using Application.Admin;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Admin;

/// <summary>
///     Startup bootstrap: ensures Admin/Support/Analyst roles exist and promotes the
///     account(s) configured in appsettings ("AdminBootstrap") into the Admin role.
///     This is the only sanctioned way to create the first admin — there is no UI
///     for creating admins, by design.
/// </summary>
public sealed class AdminBootstrapper(
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger<AdminBootstrapper> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        // 1. Roles
        foreach (var role in AdminRoles.All)
        {
            if (await roleManager.RoleExistsAsync(role))
                continue;

            var create = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            logger.LogInformation("Admin role '{Role}' created: {Succeeded}", role, create.Succeeded);
        }

        // 2. Bootstrap admin account(s) from configuration
        var emails = configuration.GetSection("AdminBootstrap:Emails").GetChildren()
            .Select(c => c.Value).Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Trim().ToLowerInvariant())
            .ToList();

        if (emails.Count == 0)
        {
            logger.LogWarning("AdminBootstrap:Emails is empty — no admin account was promoted.");
            return;
        }

        foreach (var email in emails)
        {
            ct.ThrowIfCancellationRequested();

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                logger.LogWarning("AdminBootstrap: user '{Email}' not found — register first, then restart.", email);
                continue;
            }

            if (await userManager.IsInRoleAsync(user, AdminRoles.Admin))
                continue;

            var add = await userManager.AddToRoleAsync(user, AdminRoles.Admin);
            logger.LogInformation("AdminBootstrap: '{Email}' promoted to Admin: {Succeeded}", email, add.Succeeded);
        }
    }
}
