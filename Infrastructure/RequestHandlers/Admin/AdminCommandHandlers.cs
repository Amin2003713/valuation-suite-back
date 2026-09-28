using Application.Admin;
using Application.Interfaces;
using Common.Exceptions;
using Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace RequestHandler.Admin;

/// <summary>
///     Modifies a customer through the domain methods — plan transitions go through
///     UpgradeToPro/DowngradeToFree so invariants stay in the domain (Uncle Bob).
/// </summary>
public sealed class UpdateAdminCustomerCommandHandler(
    ICommandRepository<ApplicationUser> users,
    IQueryRepository<ApplicationUser> queryUsers)
    : IRequestHandler<UpdateAdminCustomerCommand, AdminCustomerRow>
{
    public async Task<AdminCustomerRow> Handle(UpdateAdminCustomerCommand request, CancellationToken ct)
    {
        var user = await users.Table
            .FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw ValuationException.NotFound("مشتری یافت نشد.");

        if (request.IsActive is { } active)
            user.IsActive = active;

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            user.DisplayName = request.DisplayName.Trim();

        if (request.Plan is { } planName)
        {
            if (!Enum.TryParse<Domain.Companies.Plan>(planName, ignoreCase: true, out var plan))
                throw ValuationException.BadRequest($"پلن «{planName}» معتبر نیست.");

            if (plan == Domain.Companies.Plan.Pro && user.Plan != Domain.Companies.Plan.Pro)
                user.UpgradeToPro(request.PlanExpiresAt);
            else if (plan == Domain.Companies.Plan.Free && user.Plan != Domain.Companies.Plan.Free)
                user.DowngradeToFree();
            else if (plan == Domain.Companies.Plan.Pro)
                user.PlanExpiresAt = request.PlanExpiresAt;
        }
        else if (request.PlanExpiresAt is { } && user.Plan == Domain.Companies.Plan.Pro)
        {
            user.PlanExpiresAt = request.PlanExpiresAt;
        }

        await users.SaveChangesAsync(ct);

        var name = user.DisplayName;
        var email = user.Email ?? string.Empty;
        var isPro = user.IsPro;
        var planStr = user.Plan.ToString();
        var expiry = user.PlanExpiresAt;
        var isActive = user.IsActive;
        var companyId = user.CompanyId;
        var companyName = user.Company?.Name;
        var createdAt = user.CreatedAt;
        var lastLogin = user.LastLoginAt;

        return new AdminCustomerRow(
            user.Id, name, email, planStr, isPro, expiry, isActive,
            companyId, companyName, createdAt, lastLogin,
            0, 0, []);
    }
}

/// <summary>
///     Assigns/removes admin roles (Admin/Support/Analyst) via RoleManager.
///     Permission claims in existing tokens stay until re-login (short-lived JWTs recommended).
/// </summary>
public sealed class SetAdminRolesCommandHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager)
    : IRequestHandler<SetAdminRolesCommand, List<string>>
{
    public async Task<List<string>> Handle(SetAdminRolesCommand request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(request.Id.ToString())
            ?? throw ValuationException.NotFound("مشتری یافت نشد.");

        var valid = request.Roles
            .Where(r => AdminRoles.All.Contains(r, StringComparer.Ordinal))
            .Distinct()
            .ToList();

        foreach (var role in AdminRoles.All)
        {
            ct.ThrowIfCancellationRequested();

            if (await roleManager.RoleExistsAsync(role))
                continue;

            var create = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            if (!create.Succeeded)
                throw ValuationException.BadRequest($"خطا در ایجاد نقش {role}.");
        }

        var current = await userManager.GetRolesAsync(user);

        var toAdd = valid.Except(current).ToList();
        var toRemove = current.Except(valid).Where(r => AdminRoles.All.Contains(r)).ToList();

        if (toAdd.Count > 0)
        {
            var add = await userManager.AddToRolesAsync(user, toAdd);
            if (!add.Succeeded)
                throw ValuationException.BadRequest(string.Join(" | ", add.Errors.Select(e => e.Description)));
        }

        if (toRemove.Count > 0)
        {
            var rem = await userManager.RemoveFromRolesAsync(user, toRemove);
            if (!rem.Succeeded)
                throw ValuationException.BadRequest(string.Join(" | ", rem.Errors.Select(e => e.Description)));
        }

        return (await userManager.GetRolesAsync(user)).ToList();
    }
}

/// <summary>
///     Admin-initiated password reset. Bypasses the old-password check by design
///     (support scenario); gated server-side by the customers.manage permission.
/// </summary>
public sealed class ResetAdminPasswordCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<ResetAdminPasswordCommand>
{
    public async Task Handle(ResetAdminPasswordCommand request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(request.Id.ToString())
            ?? throw ValuationException.NotFound("مشتری یافت نشد.");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
            throw ValuationException.BadRequest(string.Join(" | ", result.Errors.Select(e => e.Description)));
    }
}

/// <summary>
///     Creates a staff user (Admin/Support/Analyst/Adviser) with an initial password.
///     The first admin still comes from AdminBootstrap; this serves the user manager UI.
/// </summary>
public sealed class CreateAdminUserCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<CreateAdminUserCommand, AdminUserRow>
{
    public async Task<AdminUserRow> Handle(CreateAdminUserCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await userManager.FindByEmailAsync(email) is not null)
            throw ValuationException.Conflict("این ایمیل قبلاً ثبت شده است.");

        if (request.Roles.Count == 0)
            throw ValuationException.BadRequest("حداقل یک نقش انتخاب کنید.");

        var valid = request.Roles
            .Where(r => AdminRoles.All.Contains(r, StringComparer.Ordinal))
            .Distinct()
            .ToList();
        if (valid.Count == 0)
            throw ValuationException.BadRequest("هیچ نقش معتبری انتخاب نشده است.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = request.Name.Trim(),
            Plan = Domain.Companies.Plan.Free,
            IsActive = true,
        };

        var create = await userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
            throw ValuationException.BadRequest(string.Join(" | ", create.Errors.Select(e => e.Description)));

        var add = await userManager.AddToRolesAsync(user, valid);
        if (!add.Succeeded)
            throw ValuationException.BadRequest(string.Join(" | ", add.Errors.Select(e => e.Description)));

        return new AdminUserRow(
            user.Id, user.DisplayName, email, user.Plan.ToString(), user.IsActive,
            user.CreatedAt, user.LastLoginAt, valid,
            RolePermissions.For(valid).ToList(), 0);
    }
}

/// <summary>Activates/deactivates a user account (user manager quick action).</summary>
public sealed class SetAdminUserActiveCommandHandler(
    ICommandRepository<ApplicationUser> users)
    : IRequestHandler<SetAdminUserActiveCommand>
{
    public async Task Handle(SetAdminUserActiveCommand request, CancellationToken ct)
    {
        var user = await users.Table
            .FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw ValuationException.NotFound("کاربر یافت نشد.");

        user.IsActive = request.IsActive;
        await users.SaveChangesAsync(ct);
    }
}
