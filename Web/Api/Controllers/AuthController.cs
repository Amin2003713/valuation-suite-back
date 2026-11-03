using Application.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Dto;
using Web.Api.Handlers;
using Web.Api.Services;

namespace Web.Api.Controllers;

/// <summary>
///     Authentication endpoints. Every action is a thin translation between HTTP
///     and a command/query: validation, duplicate detection, company creation,
///     token issuance and DTO shaping live in <see cref="RegisterUserCommandHandler" />
///     and <see cref="LoginUserCommandHandler" />.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
        => Ok(await mediator.Send(
            new RegisterUserCommand(request.Name, request.Email, request.Password, request.CompanyName, request.Industry, request.UseCase),
            ct));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
        => Ok(await mediator.Send(new LoginUserCommand(request.Email, request.Password), ct));

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
        => Ok(await mediator.Send(new GetCurrentUserQuery(User.GetUserId()), ct));

    /// <summary>Reads the current user's profile (account page).</summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> Profile(CancellationToken ct)
        => Ok(await mediator.Send(new GetMyProfileQuery(), ct));

    /// <summary>Updates the current user's own profile (name / phone).</summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
        => Ok(await mediator.Send(new UpdateMyProfileCommand(request.DisplayName, request.PhoneNumber, request.CompanyName, request.Industry), ct));

    public record UpdateProfileRequest(
        string? DisplayName,
        string? PhoneNumber,
        string? CompanyName,
        string? Industry
    );
}
