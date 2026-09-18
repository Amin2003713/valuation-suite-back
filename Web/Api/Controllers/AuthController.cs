using Application.Interfaces;
using Application.Users;
using Common.Exceptions;
using Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Web.Api.Dto;
using Web.Api.Services;

namespace Web.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    TokenService tokenService,
    IMediator mediator,
    Application.Interfaces.Base.ICommandRepository<Domain.Companies.Company> companyCommands) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            throw new ConflictException("این ایمیل قبلاً ثبت شده است");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = request.Name.Trim(),
            Plan = global::Domain.Companies.Plan.Free,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
            throw new ValuationException(errors);
        }

        await userManager.UpdateAsync(user);

        // Optional company registration at sign-up (users register their company too).
        if (!string.IsNullOrWhiteSpace(request.CompanyName))
        {
            var slug = Slugify(request.CompanyName);
            var company = Domain.Companies.Company.Create(request.CompanyName.Trim(), slug);
            await companyCommands.AddAsync(company, ct, saveNow: true);
            user.CompanyId = company.Id;
            await userManager.UpdateAsync(user);
        }

        var roles = await userManager.GetRolesAsync(user);
        var (token, expiresAt) = tokenService.CreateToken(user, roles);

        return Ok(BuildAuthResponse(user, roles, token, expiresAt));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null || !user.IsActive)
            throw new ForbiddenException("کاربری با این ایمیل وجود ندارد یا غیرفعال است");

        var valid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
            throw new ForbiddenException("رمز عبور اشتباه است");

        user.UpdateLastLogin();
        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var (token, expiresAt) = tokenService.CreateToken(user, roles);

        return Ok(BuildAuthResponse(user, roles, token, expiresAt));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            throw new ForbiddenException("دسترسی غیرمجاز");

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("کاربر یافت نشد");

        var roles = await userManager.GetRolesAsync(user);

        return Ok(new UserDto(
            user.Id,
            user.DisplayName,
            user.Email ?? string.Empty,
            user.Plan.ToString(),
            user.IsPro,
            user.CompanyId,
            user.Company != null ? user.Company.Name : null,
            roles.ToList()));
    }

    /// <summary>Reads the current user's profile (account page).</summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> Profile(CancellationToken ct)
        => Ok(await mediator.Send(new GetMyProfileQuery(), ct));

    /// <summary>Updates the current user's own profile (name / phone).</summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
        => Ok(await mediator.Send(new UpdateMyProfileCommand(request.DisplayName, request.PhoneNumber), ct));

    public record UpdateProfileRequest(string? DisplayName, string? PhoneNumber);

    private AuthResponse BuildAuthResponse(ApplicationUser user, IList<string> roles, string token, DateTime expiresAt)
    {
        return new AuthResponse(
            user.Id,
            user.DisplayName,
            user.Email ?? string.Empty,
            user.Plan.ToString(),
            user.IsPro,
            user.CompanyId,
            user.Company != null ? user.Company.Name : null,
            roles.ToList(),
            user.Company != null ? user.Company.Slug : null,
            user.Company != null ? user.Company.LogoUrl : null,
            user.Company != null ? user.Company.Industry : null,
            token,
            expiresAt);
    }

    private static string Slugify(string name)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var slug = Regex.Replace(normalized, @"[^a-z0-9\u0600-\u06FF]+", "-").Trim('-');
        if (string.IsNullOrEmpty(slug))
            slug = $"co-{Guid.NewGuid().ToString("N")[..8]}";
        return $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";
    }
}
