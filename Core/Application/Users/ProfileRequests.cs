using MediatR;

namespace Application.Users;

/// <summary>Updates the current user's own profile (display name and/or phone number).</summary>
public sealed record UpdateMyProfileCommand(string? DisplayName, string? PhoneNumber) : IRequest<ProfileResponse>;

/// <summary>Current user's profile snapshot (also used as the response of profile reads).</summary>
public record ProfileResponse(
    Guid Id,
    string Name,
    string Email,
    string PhoneNumber,
    string Plan,
    bool IsPro,
    DateTime? PlanExpiresAt,
    Guid? CompanyId,
    string? CompanyName,
    List<string> Roles);

/// <summary>Reads the current user's profile.</summary>
public sealed record GetMyProfileQuery : IRequest<ProfileResponse>;
