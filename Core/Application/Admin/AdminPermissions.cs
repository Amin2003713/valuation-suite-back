namespace Application.Admin;

/// <summary>
///     Role catalog. Roles are ASP.NET Identity roles; each role maps to a set of
///     granular permissions that are stamped into the JWT as "perm" claims at login.
/// </summary>
public static class AdminRoles
{
    public const string Admin = "Admin";
    public const string Support = "Support";
    public const string Analyst = "Analyst";

    public static readonly string[] All = [Admin, Support, Analyst];
}

/// <summary>
///     Granular permission constants (feature gates). Policies are named "perm:{permission}".
/// </summary>
public static class Perms
{
    public const string DashboardView = "dashboard.view";
    public const string CustomersRead = "customers.read";
    public const string CustomersManage = "customers.manage";
    public const string PaymentsRead = "payments.read";
    public const string SubmissionsRead = "submissions.read";

    public static readonly string[] All =
    [
        DashboardView, CustomersRead, CustomersManage, PaymentsRead, SubmissionsRead
    ];
}

/// <summary>Role → permission map. The union of a user's role permissions goes into the token.</summary>
public static class RolePermissions
{
    private static readonly Dictionary<string, string[]> Map = new()
    {
        [AdminRoles.Admin] = Perms.All,
        [AdminRoles.Support] =
        [
            Perms.DashboardView, Perms.CustomersRead, Perms.PaymentsRead, Perms.SubmissionsRead
        ],
        [AdminRoles.Analyst] = [Perms.DashboardView, Perms.SubmissionsRead],
    };

    public static IReadOnlyList<string> For(IEnumerable<string> roles)
    {
        var set = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var role in roles)
            if (Map.TryGetValue(role, out var perms))
                foreach (var p in perms)
                    set.Add(p);

        return [.. set];
    }
}

/// <summary>Claim-based policies: one policy per permission ("perm:{permission}").</summary>
public static class PermissionPolicies
{
    public const string ClaimType = "perm";

    public static string Policy(string permission) => $"perm:{permission}";

    public static void AddPolicies(Microsoft.AspNetCore.Authorization.AuthorizationOptions options)
    {
        foreach (var permission in Perms.All)
        {
            options.AddPolicy(Policy(permission), policy =>
                policy.RequireAuthenticatedUser().RequireClaim(ClaimType, permission));
        }
    }
}
