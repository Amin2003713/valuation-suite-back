using Application.Interfaces;
using Common.Exceptions;

namespace Application.Common;

/// <summary>
///     Ownership guards for user-scoped resources. Identity always comes from the
///     authenticated principal (<see cref="ICurrentUserAccessor" />), never from a
///     request body or query string — a caller therefore cannot act as another user.
/// </summary>
public static class CurrentUserGuard
{
    /// <summary>Returns the authenticated user id, or throws 403 when the caller is anonymous.</summary>
    public static Guid RequireUserId(this ICurrentUserAccessor currentUser)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw ValuationException.Forbidden("دسترسی غیرمجاز.");

        return userId;
    }

    /// <summary>
    ///     Returns the authenticated user id after checking that it owns the resource.
    ///     Staff roles are <b>not</b> exempt: records are scoped to their owner unless
    ///     the caller goes through an admin endpoint that is itself permission-gated.
    /// </summary>
    public static Guid RequireOwnership(this ICurrentUserAccessor currentUser, Guid ownerId, string? message = null)
    {
        var userId = currentUser.RequireUserId();
        if (ownerId != userId)
            throw ValuationException.Forbidden(message ?? "این مورد متعلق به شما نیست.");

        return userId;
    }
}
