using Application.Interfaces;
using Common.Exceptions;
using Domain.Portfolio;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence.DbContexts;

namespace RequestHandlers.Portfolio;

public sealed record PortfolioAssetRow(
    Guid Id,
    string Kind,
    string Title,
    string? ReferenceNumber,
    string? Jurisdiction,
    string Status,
    DateTime? FilingDate,
    DateTime? ExpiryDate,
    decimal? LatestValue,
    string? Unit,
    DateTime? AsOf,
    double? ChangePercent,
    int ValuationCount);

public sealed record PortfolioTotals(string Unit, decimal Total, int Assets);

public sealed record PortfolioResponse(PortfolioAssetRow[] Assets, PortfolioTotals[] Totals);

public sealed record ValuationRow(
    Guid Id, string ToolCode, decimal ValueAmount, string Unit, DateTime AsOfDate,
    string? Method, bool IsPrimary, Guid? SubmissionId, string? Note);

public sealed record AssetHistoryResponse(Guid AssetId, string Title, ValuationRow[] Valuations);

public sealed record CreateAssetRequest(
    string Kind, string Title, string? ReferenceNumber, string? Jurisdiction, string? Status,
    DateTime? FilingDate, DateTime? GrantDate, DateTime? ExpiryDate, string? Notes);

public sealed record AttachValuationRequest(
    string ToolCode, Guid? SubmissionId, decimal ValueAmount, string Unit,
    DateTime? AsOfDate, string? Method, double? OverallScore, bool IsPrimary, string? Note);

public sealed record GetPortfolioQuery : IRequest<PortfolioResponse>;

public sealed record GetAssetHistoryQuery(Guid AssetId) : IRequest<AssetHistoryResponse>;

public sealed record CreateAssetCommand(CreateAssetRequest Body) : IRequest<Guid>;

public sealed record DeleteAssetCommand(Guid AssetId) : IRequest<bool>;

public sealed record AttachValuationCommand(Guid AssetId, AttachValuationRequest Body) : IRequest<Guid>;

public sealed record DeleteValuationCommand(Guid ValuationId) : IRequest<bool>;

internal static class PortfolioGuard
{
    public static Guid RequireCompany(ICurrentUserAccessor user) =>
        user.CompanyId ?? throw ValuationException.BadRequest(
            "برای استفاده از پرتفوی ابتدا نام شرکت و حوزهٔ فعالیت را در پروفایل تکمیل کنید.");

    public static T Parse<T>(string value, T fallback) where T : struct, Enum =>
        Enum.TryParse<T>(value, true, out var parsed) ? parsed : fallback;
}

public sealed class GetPortfolioQueryHandler(ValuationDbContext db, ICurrentUserAccessor currentUser)
    : IRequestHandler<GetPortfolioQuery, PortfolioResponse>
{
    public async Task<PortfolioResponse> Handle(GetPortfolioQuery request, CancellationToken ct)
    {
        if (currentUser.CompanyId is not { } companyId)
            return new PortfolioResponse([], []);

        var assets = await db.Set<IpAsset>().AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .OrderBy(a => a.Title)
            .ToListAsync(ct);

        var valuations = await db.Set<AssetValuation>().AsNoTracking()
            .Where(v => v.CompanyId == companyId)
            .OrderByDescending(v => v.AsOfDate)
            .ToListAsync(ct);

        var rows = new List<PortfolioAssetRow>(assets.Count);
        var latestByAsset = new List<AssetValuation>();

        foreach (var asset in assets)
        {
            var history = valuations.Where(v => v.AssetId == asset.Id).ToList();
            var latest = history.FirstOrDefault();
            var previous = history.Skip(1).FirstOrDefault(v => latest is null || v.Unit == latest.Unit);
            if (latest is not null) latestByAsset.Add(latest);

            double? change = null;
            if (latest is not null && previous is not null && previous.ValueAmount != 0)
                change = Math.Round((double)((latest.ValueAmount - previous.ValueAmount) / previous.ValueAmount) * 100, 1);

            rows.Add(new PortfolioAssetRow(
                asset.Id, asset.Kind.ToString(), asset.Title, asset.ReferenceNumber,
                asset.Jurisdiction, asset.Status.ToString(), asset.FilingDate, asset.ExpiryDate,
                latest?.ValueAmount, latest?.Unit, latest?.AsOfDate, change, history.Count));
        }

        // Totals use each asset's latest figure once (never the sum of all history).
        var totals = latestByAsset
            .GroupBy(v => v.Unit)
            .Select(g => new PortfolioTotals(g.Key, g.Sum(x => x.ValueAmount), g.Count()))
            .OrderByDescending(t => t.Total)
            .ToArray();

        return new PortfolioResponse(rows.ToArray(), totals);
    }
}

public sealed class CreateAssetCommandHandler(ValuationDbContext db, ICurrentUserAccessor currentUser)
    : IRequestHandler<CreateAssetCommand, Guid>
{
    public async Task<Guid> Handle(CreateAssetCommand request, CancellationToken ct)
    {
        var companyId = PortfolioGuard.RequireCompany(currentUser);
        var body = request.Body;
        if (string.IsNullOrWhiteSpace(body.Title))
            throw ValuationException.BadRequest("نام دارایی الزامی است.");

        var asset = new IpAsset
        {
            CompanyId = companyId,
            CreatedByUserId = currentUser.UserId,
            Kind = PortfolioGuard.Parse(body.Kind, IpAssetKind.Other),
            Title = body.Title.Trim(),
            ReferenceNumber = body.ReferenceNumber,
            Jurisdiction = body.Jurisdiction,
            Status = PortfolioGuard.Parse(body.Status, IpAssetStatus.Granted),
            FilingDate = body.FilingDate,
            GrantDate = body.GrantDate,
            ExpiryDate = body.ExpiryDate,
            Notes = body.Notes,
        };

        db.Set<IpAsset>().Add(asset);
        await db.SaveChangesAsync(ct);
        return asset.Id;
    }
}

public sealed class DeleteAssetCommandHandler(ValuationDbContext db, ICurrentUserAccessor currentUser)
    : IRequestHandler<DeleteAssetCommand, bool>
{
    public async Task<bool> Handle(DeleteAssetCommand request, CancellationToken ct)
    {
        var companyId = PortfolioGuard.RequireCompany(currentUser);
        var asset = await db.Set<IpAsset>()
            .FirstOrDefaultAsync(a => a.Id == request.AssetId && a.CompanyId == companyId, ct)
            ?? throw ValuationException.NotFound("دارایی یافت نشد.");

        // Soft delete the asset and archive its history in the same transaction.
        var valuations = await db.Set<AssetValuation>()
            .Where(v => v.AssetId == asset.Id).ToListAsync(ct);
        foreach (var v in valuations) db.Set<AssetValuation>().Remove(v);
        db.Set<IpAsset>().Remove(asset);

        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed class AttachValuationCommandHandler(ValuationDbContext db, ICurrentUserAccessor currentUser)
    : IRequestHandler<AttachValuationCommand, Guid>
{
    public async Task<Guid> Handle(AttachValuationCommand request, CancellationToken ct)
    {
        var companyId = PortfolioGuard.RequireCompany(currentUser);
        var body = request.Body;
        if (body.ValueAmount <= 0)
            throw ValuationException.BadRequest("مبلغ ارزش‌گذاری باید بزرگ‌تر از صفر باشد.");
        if (string.IsNullOrWhiteSpace(body.Unit))
            throw ValuationException.BadRequest("واحد پول ارزش‌گذاری الزامی است.");

        var asset = await db.Set<IpAsset>()
            .FirstOrDefaultAsync(a => a.Id == request.AssetId && a.CompanyId == companyId, ct)
            ?? throw ValuationException.NotFound("دارایی یافت نشد.");

        var valuation = new AssetValuation
        {
            CompanyId = companyId,
            AssetId = asset.Id,
            ToolCode = (body.ToolCode ?? string.Empty).Trim(),
            SubmissionId = body.SubmissionId,
            ValueAmount = body.ValueAmount,
            Unit = body.Unit.Trim(),
            AsOfDate = body.AsOfDate ?? DateTime.UtcNow,
            Method = body.Method,
            OverallScore = body.OverallScore,
            IsPrimary = body.IsPrimary,
            Note = body.Note,
        };

        if (valuation.IsPrimary)
        {
            var others = await db.Set<AssetValuation>()
                .Where(v => v.AssetId == asset.Id && v.IsPrimary).ToListAsync(ct);
            foreach (var o in others) o.IsPrimary = false;
        }

        db.Set<AssetValuation>().Add(valuation);
        await db.SaveChangesAsync(ct);
        return valuation.Id;
    }
}

public sealed class DeleteValuationCommandHandler(ValuationDbContext db, ICurrentUserAccessor currentUser)
    : IRequestHandler<DeleteValuationCommand, bool>
{
    public async Task<bool> Handle(DeleteValuationCommand request, CancellationToken ct)
    {
        var companyId = PortfolioGuard.RequireCompany(currentUser);
        var valuation = await db.Set<AssetValuation>()
            .FirstOrDefaultAsync(v => v.Id == request.ValuationId && v.CompanyId == companyId, ct)
            ?? throw ValuationException.NotFound("ارزش‌گذاری یافت نشد.");

        db.Set<AssetValuation>().Remove(valuation);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed class GetAssetHistoryQueryHandler(ValuationDbContext db, ICurrentUserAccessor currentUser)
    : IRequestHandler<GetAssetHistoryQuery, AssetHistoryResponse>
{
    public async Task<AssetHistoryResponse> Handle(GetAssetHistoryQuery request, CancellationToken ct)
    {
        var companyId = PortfolioGuard.RequireCompany(currentUser);
        var asset = await db.Set<IpAsset>().AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AssetId && a.CompanyId == companyId, ct)
            ?? throw ValuationException.NotFound("دارایی یافت نشد.");

        var rows = await db.Set<AssetValuation>().AsNoTracking()
            .Where(v => v.AssetId == asset.Id)
            .OrderByDescending(v => v.AsOfDate)
            .Select(v => new ValuationRow(
                v.Id, v.ToolCode, v.ValueAmount, v.Unit, v.AsOfDate,
                v.Method, v.IsPrimary, v.SubmissionId, v.Note))
            .ToListAsync(ct);

        return new AssetHistoryResponse(asset.Id, asset.Title, rows.ToArray());
    }
}
