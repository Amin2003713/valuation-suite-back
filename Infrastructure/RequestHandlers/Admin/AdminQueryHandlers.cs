using Application.Admin;
using Application.Interfaces;
using Application.Tools;
using Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace RequestHandlers.Admin;

/// <summary>Dashboard KPIs. Gated by dashboard.view at the controller.</summary>
public sealed class GetAdminDashboardQueryHandler(IAdminQueryRepository repo)
    : IRequestHandler<GetAdminDashboardQuery, AdminDashboardResponse>
{
    public Task<AdminDashboardResponse> Handle(GetAdminDashboardQuery request, CancellationToken ct)
        => repo.GetDashboardAsync(ct);
}

public sealed class GetAdminPackagesQueryHandler(ICommandRepository<AccessPackage> packages)
    : IRequestHandler<GetAdminPackagesQuery, AdminPackageListResponse>
{
    public async Task<AdminPackageListResponse> Handle(GetAdminPackagesQuery request, CancellationToken ct)
    {
        var all = await packages.TableNoTracking.OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync(ct);
        var total = all.Count;
        var items = all.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();

        var list = items.Select(p => new AdminPackageRow(
            p.Id, p.Name, p.Description, p.PickCount,
            p.ToolCodes(), p.DurationDays, p.PriceToman, p.IsActive, p.SortOrder)).ToList();

        return new AdminPackageListResponse(request.Page, request.PageSize, total, list);
    }
}

public sealed class DeleteAdminPackageCommandHandler(ICommandRepository<AccessPackage> packages)
    : IRequestHandler<DeleteAdminPackageCommand, Guid>
{
    public async Task<Guid> Handle(DeleteAdminPackageCommand request, CancellationToken ct)
    {
        var pkg = await packages.Table.FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw ValuationException.NotFound("بسته یافت نشد.");
        pkg.IsActive = false;
        await packages.SaveChangesAsync(ct);
        return pkg.Id;
    }
}

public sealed class GetAdminCustomersQueryHandler(IAdminQueryRepository repo)
    : IRequestHandler<GetAdminCustomersQuery, AdminCustomerListResponse>
{
    public async Task<AdminCustomerListResponse> Handle(GetAdminCustomersQuery request, CancellationToken ct)
    {
        var (items, total) = await repo.GetCustomersAsync(
            Math.Max(1, request.Page), Math.Clamp(request.PageSize, 1, 100),
            request.Search, request.Plan, request.IsActive, ct);

        return new AdminCustomerListResponse(request.Page, Math.Clamp(request.PageSize, 1, 100), total, items);
    }
}

public sealed class GetAdminSubmissionsQueryHandler(IAdminQueryRepository repo)
    : IRequestHandler<GetAdminSubmissionsQuery, AdminSubmissionListResponse>
{
    public async Task<AdminSubmissionListResponse> Handle(GetAdminSubmissionsQuery request, CancellationToken ct)
    {
        var (items, total) = await repo.SearchSubmissionsAsync(
            Math.Max(1, request.Page), Math.Clamp(request.PageSize, 1, 100),
            request.ToolCode, request.Search, request.UserId, ct);

        return new AdminSubmissionListResponse(request.Page, Math.Clamp(request.PageSize, 1, 100), total, items);
    }
}

public sealed class GetAdminToolsQueryHandler(IAdminQueryRepository repo)
    : IRequestHandler<GetAdminToolsQuery, List<AdminToolRow>>
{
    public Task<List<AdminToolRow>> Handle(GetAdminToolsQuery request, CancellationToken ct)
        => repo.GetToolsAsync(ct);
}

public sealed class GetAdminPaymentsQueryHandler(IAdminQueryRepository repo)
    : IRequestHandler<GetAdminPaymentsQuery, AdminPaymentListResponse>
{
    public async Task<AdminPaymentListResponse> Handle(GetAdminPaymentsQuery request, CancellationToken ct)
    {
        var (items, total) = await repo.GetPaymentsAsync(
            Math.Max(1, request.Page), Math.Clamp(request.PageSize, 1, 100),
            request.UserId, request.Status, ct);

        return new AdminPaymentListResponse(request.Page, Math.Clamp(request.PageSize, 1, 100), total, items);
    }
}

public sealed class GetAdminCompaniesQueryHandler(IAdminQueryRepository repo)
    : IRequestHandler<GetAdminCompaniesQuery, AdminCompanyListResponse>
{
    public async Task<AdminCompanyListResponse> Handle(GetAdminCompaniesQuery request, CancellationToken ct)
    {
        var (items, total) = await repo.GetCompaniesAsync(
            Math.Max(1, request.Page), Math.Clamp(request.PageSize, 1, 100), request.Search, ct);

        return new AdminCompanyListResponse(request.Page, Math.Clamp(request.PageSize, 1, 100), total, items);
    }
}

public sealed class GetAdminUsersQueryHandler(IAdminQueryRepository repo)
    : IRequestHandler<GetAdminUsersQuery, AdminUserListResponse>
{
    public async Task<AdminUserListResponse> Handle(GetAdminUsersQuery request, CancellationToken ct)
    {
        var (items, total) = await repo.GetUsersAsync(
            Math.Max(1, request.Page), Math.Clamp(request.PageSize, 1, 100),
            request.Search, request.Role, ct);

        return new AdminUserListResponse(request.Page, Math.Clamp(request.PageSize, 1, 100), total, items);
    }
}

public sealed class GetAdminCustomerQueryHandler(
    IAdminQueryRepository repo,
    Microsoft.AspNetCore.Identity.UserManager<Domain.Users.ApplicationUser> userManager)
    : IRequestHandler<GetAdminCustomerQuery, AdminCustomerDetail>
{
    public async Task<AdminCustomerDetail> Handle(GetAdminCustomerQuery request, CancellationToken ct)
    {
        var customer = await repo.GetCustomerAsync(request.Id, ct)
            ?? throw ValuationException.NotFound("مشتری یافت نشد.");

        var user = await userManager.FindByIdAsync(request.Id.ToString());
        List<string> roles = user is null
            ? new List<string>()
            : (await userManager.GetRolesAsync(user)).ToList();

        var payments = await repo.GetPaymentsAsync(1, 50, request.Id, null, ct);
        var submissions = await repo.GetSubmissionsAsync(request.Id, take: 20, ct);

        return new AdminCustomerDetail(customer with { Roles = roles }, payments.Items, submissions);
    }
}
