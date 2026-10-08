using Application.Interfaces.Base;
using Application.Users;
using Common.Exceptions;
using Domain.Companies;
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
            .Include(u => u.Company)
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
            user.Company?.Industry,
            roles.ToList());
    }
}

/// <summary>
///     Updates the caller's own profile — display name, phone number and the
///     company (name + industry). Email is fixed. Passing a company name
///     creates (or renames) the user's own company; industry is stored on it.
/// </summary>
public sealed class UpdateMyProfileCommandHandler(
    ICurrentUserAccessor currentUser,
    UserManager<ApplicationUser> userManager,
    ICompanyCommandRepository companyCommands,
    ICompanyQueryRepository companyQueries)
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

        // Company name / industry — optional; a name creates the company on
        // first save (same as registration) and updates it afterwards.
        Domain.Companies.Company? company = null;
        if (user.CompanyId is { } companyId)
            company = await companyQueries
                .FirstOrDefaultAsync(c => c.Id == companyId, ct);

        if (company is null && !string.IsNullOrWhiteSpace(request.CompanyName))
        {
            var slug = Slugify(request.CompanyName);
            company = Company.Create(request.CompanyName.Trim(), slug);
            await companyCommands.AddAsync(company, ct, saveNow: true);
            user.CompanyId = company.Id;
        }

        if (company is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.CompanyName))
                company.Name = request.CompanyName.Trim();
            if (!string.IsNullOrWhiteSpace(request.Industry))
                company.SetIndustry(request.Industry.Trim());
            await companyCommands.UpdateAsync(company, ct, saveNow: true);
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
            company?.Name,
            company?.Industry,
            roles.ToList());
    }

    private static string Slugify(string name)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var slug = System.Text.RegularExpressions.Regex
            .Replace(normalized, @"[^a-z0-9\u0600-\u06FF]+", "-").Trim('-');
        if (string.IsNullOrEmpty(slug))
            slug = $"co-{Guid.NewGuid().ToString("N")[..8]}";
        return $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";
    }
}
