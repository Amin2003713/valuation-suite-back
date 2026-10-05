namespace Web.Api.Dto;

public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string? CompanyName = null,
    string? Industry = null,
    string? UseCase = null);
public record LoginRequest(string Email, string Password);

public record AuthResponse(
    Guid Id,
    string Name,
    string Email,
    string Plan,
    bool IsPro,
    Guid? CompanyId,
    string? CompanyName,
    List<string> Roles,
    string? CompanySlug,
    string? CompanyLogoUrl,
    string? CompanyIndustry,
    string? UseCase,
    string Token,
    DateTime TokenExpiresAt);

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string Plan,
    bool IsPro,
    Guid? CompanyId,
    string? CompanyName,
    List<string> Roles,
    string? UseCase);
