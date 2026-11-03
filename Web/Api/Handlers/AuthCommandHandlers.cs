using Application.Interfaces.Base;
using Application.Security;
using Common.Exceptions;
using Domain.Companies;
using Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;
using Web.Api.Dto;
using Web.Api.Services;

namespace Web.Api.Handlers;

/* ── Auth handlers ─────────────────────────────────────────────────────────
 * Registration and sign-in used to live in AuthController: normalising the
 * e-mail, checking duplicates, creating the company, slugging its name,
 * assembling the token response and shaping the DTO were all inline in the
 * action methods. They are commands now, so the controller only translates
 * HTTP ↔ message.
 *
 * Handlers live in the web assembly because they need UserManager and the JWT
 * TokenService; that assembly is registered with MediatR in Program.cs.
 * ------------------------------------------------------------------------- */

public sealed record RegisterUserCommand(
    string Name,
    string Email,
    string Password,
    string? CompanyName = null,
    string? Industry = null,
    string? UseCase = null) : IRequest<AuthResponse>;

public sealed record LoginUserCommand(string Email, string Password) : IRequest<AuthResponse>;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<UserDto>;

public sealed partial class RegisterUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    TokenService tokenService,
    ICommandRepository<Company> companyCommands)
    : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$")]
    private static partial Regex EmailPattern();

    public async Task<AuthResponse> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var name = (request.Name ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

        if (name.Length is < 2 or > 120)
            throw new ValuationException("نام را کامل وارد کنید.");

        if (!EmailPattern().IsMatch(email))
            throw new ValuationException("ایمیل معتبر وارد کنید.");

        if ((request.Password ?? string.Empty).Length < 6)
            throw new ValuationException("رمز عبور باید حداقل ۶ کاراکتر باشد.");

        // Free-text fields are user input: refuse markup outright, and refuse
        // links that impersonate the platform.
        foreach (var field in new[] { name, request.CompanyName, request.Industry, request.UseCase })
        {
            var report = ThreatDetector.Inspect(field);
            if (report.HasMarkup)
                throw new ValuationException("متن وارد‌شده شامل کد یا نشانه‌گذاری غیرمجاز است.");
            if (report.IsPhishing)
                throw new ValuationException("متن وارد‌شده شامل پیوند مشکوک است.");
        }

        if (await userManager.FindByEmailAsync(email) is not null)
            throw new ConflictException("این ایمیل قبلاً ثبت شده است");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = name,
            Plan = Plan.Free,
            UseCase = string.IsNullOrWhiteSpace(request.UseCase) ? null : request.UseCase.Trim(),
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            throw new ValuationException(string.Join(" | ", created.Errors.Select(e => e.Description)));

        Company? company = null;
        if (!string.IsNullOrWhiteSpace(request.CompanyName))
        {
            company = Company.Create(request.CompanyName.Trim(), Slugify(request.CompanyName));
            if (!string.IsNullOrWhiteSpace(request.Industry))
                company.SetIndustry(request.Industry.Trim());

            await companyCommands.AddAsync(company, ct, saveNow: true);
            user.CompanyId = company.Id;
            await userManager.UpdateAsync(user);
        }

        var roles = await userManager.GetRolesAsync(user);
        var (token, expiresAt) = tokenService.CreateToken(user, roles);

        return Build(user, roles, token, expiresAt, company);
    }

    private static string Slugify(string name)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var slug = Regex.Replace(normalized, @"[^a-z0-9\u0600-\u06FF]+", "-").Trim('-');
        if (string.IsNullOrEmpty(slug))
            slug = $"co-{Guid.NewGuid().ToString("N")[..8]}";

        return $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";
    }

    internal static AuthResponse Build(
        ApplicationUser user,
        IList<string> roles,
        string token,
        DateTime expiresAt,
        Company? company = null)
    {
        var co = company ?? user.Company;
        return new AuthResponse(
            user.Id,
            user.DisplayName,
            user.Email ?? string.Empty,
            user.Plan.ToString(),
            user.IsPro,
            co?.Id ?? user.CompanyId,
            co?.Name,
            roles.ToList(),
            co?.Slug,
            co?.LogoUrl,
            co?.Industry,
            user.UseCase,
            token,
            expiresAt);
    }
}

public sealed class LoginUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    TokenService tokenService)
    : IRequestHandler<LoginUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginUserCommand request, CancellationToken ct)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(email);

        // Same message for "unknown", "inactive" and "wrong password": telling a
        // caller which one it was turns login into an account-enumeration oracle.
        if (user is null || !user.IsActive)
            throw new ForbiddenException("ایمیل یا رمز عبور نادرست است");

        if (!await userManager.CheckPasswordAsync(user, request.Password ?? string.Empty))
            throw new ForbiddenException("ایمیل یا رمز عبور نادرست است");

        user.UpdateLastLogin();
        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var (token, expiresAt) = tokenService.CreateToken(user, roles);

        return RegisterUserCommandHandler.Build(user, roles, token, expiresAt);
    }
}

public sealed class GetCurrentUserQueryHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        if (request.UserId == Guid.Empty)
            throw new ForbiddenException("دسترسی غیرمجاز");

        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new NotFoundException("کاربر یافت نشد");

        var roles = await userManager.GetRolesAsync(user);

        return new UserDto(
            user.Id,
            user.DisplayName,
            user.Email ?? string.Empty,
            user.Plan.ToString(),
            user.IsPro,
            user.CompanyId,
            user.Company?.Name,
            roles.ToList(),
            user.UseCase);
    }
}
