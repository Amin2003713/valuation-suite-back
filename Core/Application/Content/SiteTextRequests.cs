using MediatR;

namespace Application.Content;

/* ── Read models ─────────────────────────────────────────────────────────── */

/// <summary>Public override map consumed by the client provider.</summary>
public sealed record SiteTextDto(string Key, string Group, string Fa, string En, int SortOrder);

/// <summary>Row shown in the admin editor (includes id/label/active flag).</summary>
public sealed record AdminSiteTextDto(
    Guid Id, string Key, string Group, string Label, string Fa, string En, int SortOrder, bool IsActive);

/* ── Requests ────────────────────────────────────────────────────────────── */

/// <summary>Active texts only — anonymous, cached briefly at the edge.</summary>
public sealed record GetSiteTextsQuery : IRequest<List<SiteTextDto>>;

/// <summary>All texts (active or not) for the admin editor.</summary>
public sealed record GetAdminSiteTextsQuery : IRequest<List<AdminSiteTextDto>>;

/// <summary>
///     Creates (<paramref name="Id" /> null) or updates one text. Texts may be
///     blank in either language; the client then keeps its built-in default.
/// </summary>
public sealed record UpsertSiteTextCommand(
    Guid? Id,
    string? Key,
    string? Group,
    string? Label,
    string? Fa,
    string? En,
    int SortOrder,
    bool IsActive = true) : IRequest<Guid>;

public sealed record DeleteSiteTextCommand(Guid Id) : IRequest<bool>;
