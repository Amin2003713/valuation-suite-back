namespace Web.Api.Dto;

public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);

public record AuthResponse(
    Guid Id,
    string Name,
    string Email,
    string Plan,
    string Token,
    DateTime TokenExpiresAt);

public record UserDto(Guid Id, string Name, string Email, string Plan, bool IsPro, Guid? CompanyId);
