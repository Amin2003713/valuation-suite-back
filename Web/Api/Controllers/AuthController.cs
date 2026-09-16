using Application.Interfaces;
using Common.Exceptions;
using Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Dto;
using Web.Api.Services;

namespace Web.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    TokenService tokenService) : ControllerBase
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
        var (token, expiresAt) = tokenService.CreateToken(user);

        return Ok(new AuthResponse(user.Id, user.DisplayName, user.Email!, user.Plan.ToString(), token, expiresAt));
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

        return Ok(new AuthResponse(user.Id, user.DisplayName, user.Email!, user.Plan.ToString(), token, expiresAt));
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

        return Ok(new UserDto(
            user.Id, user.DisplayName, user.Email ?? string.Empty,
            user.Plan.ToString(), user.IsPro, user.CompanyId));
    }
}
