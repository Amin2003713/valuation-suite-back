using Application.Interfaces.Base;
using Application.Users;
using Common.Exceptions;
using Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RequestHandlers.Tools;

namespace RequestHandlers.Users;

/// <summary>Reads the caller's profile.</summary>
public sealed class GetMyProfileQueryHandler(
    ICurrentUserAccessor currentUser,
    UserManager<ApplicationUser> userManager)
    : IRequestHandler<GetMyProfileQuery, ProfileResponse>
{
    public async Task<ProfileResponse> Handle(GetMyProfileQuery request, CancellationToken ct)
    {
        var user = await userManager.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct)
            ?? throw ValuationException.NotFound("کاربر یافت نشد.");

        var roles = await userManager.GetRolesAsync(user);

        return new ProfileResponse(
            user.Id,
            user.DisplayName,
            user.Email ?? string.Empty,
            user.PhoneNumber ?? string.Empty,
            user.Plan.ToString(),
            user.IsPro,
            user.PlanExpiresAt,
            user.CompanyId,
            user.Company?.Name,
            roles.ToList());
    }
}

/// <summary>Updates the caller's own profile — name and phone only; email/company are fixed.</summary>
public sealed class UpdateMyProfileCommandHandler(
    ICurrentUserAccessor currentUser,
    UserManager<ApplicationUser> userManager)
    : IRequestHandler<UpdateMyProfileCommand, ProfileResponse>
{
    public async Task<ProfileResponse> Handle(UpdateMyProfileCommand request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString())
            ?? throw ValuationException.NotFound("کاربر یافت نشد.");

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            user.DisplayName = request.DisplayName.Trim();

        if (request.PhoneNumber is { } phone)
        {
            var normalized = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            if (normalized is not null && !System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^09\d{9}$"))
                throw ValuationException.BadRequest("شماره موبایل معتبر نیست (قالب: 09xxxxxxxxx).");
            var setResult = await userManager.SetPhoneNumberAsync(user, normalized);
            if (!setResult.Succeeded)
                throw ValuationException.BadRequest(string.Join(" | ", setResult.Errors.Select(e => e.Description)));
        }

        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);

        return new ProfileResponse(
            user.Id,
            user.DisplayName,
            user.Email ?? string.Empty,
            user.PhoneNumber ?? string.Empty,
            user.Plan.ToString(),
            user.IsPro,
            user.PlanExpiresAt,
            user.CompanyId,
            user.Company?.Name,
            roles.ToList());
    }
}
