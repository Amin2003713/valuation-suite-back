using Application.Admin;
using Application.Interfaces;
using Common.Exceptions;
using MediatR;

namespace RequestHandlers.Admin;

/// <summary>Dashboard KPIs. Gated by dashboard.view at the controller.</summary>
public sealed class GetAdminDashboardQueryHandler(IAdminQueryRepository repo)
    : IRequestHandler<GetAdminDashboardQuery, AdminDashboardResponse>
{
    public Task<AdminDashboardResponse> Handle(GetAdminDashboardQuery request, CancellationToken ct)
        => repo.GetDashboardAsync(ct);
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
