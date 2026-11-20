using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Application.Tools;
using Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace RequestHandlers.Tools;

/// <summary>
///     Printable report for one stored run — the paid artefact. It is gated
///     server-side to Pro subscribers (or a purchased advanced section of that
///     tool); everyone else gets 403, so the client can never unlock it.
///     The HTML is self-contained and print-styled, so "Print → Save as PDF" in
///     any browser produces the PDF without adding a PDF dependency. It states the
///     standard/methods the engine follows and the limitations, because ISO 10668
///     and IVS 210 both require the basis and assumptions to be declared.
/// </summary>
public sealed record ExportToolSubmissionReportQuery(Guid SubmissionId) : IRequest<ToolSubmissionReport>;

public sealed record ToolSubmissionReport(string FileName, string Html);

public sealed class ExportToolSubmissionReportQueryHandler(
    IToolSubmissionQueries submissions,
    ICurrentUserAccessor currentUser,
    IEntitlementService entitlements) : IRequestHandler<ExportToolSubmissionReportQuery, ToolSubmissionReport>
{
    public async Task<ToolSubmissionReport> Handle(
        ExportToolSubmissionReportQuery request, CancellationToken ct)
    {
        if (currentUser.UserId == Guid.Empty)
            throw ValuationException.Forbidden("برای دریافت گزارش ابتدا وارد حساب خود شوید.");

        var row = await submissions.All
            .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, ct)
            ?? throw ValuationException.NotFound("نتیجه یافت نشد.");

        if (row.UserId != currentUser.UserId && !currentUser.IsStaff)
            throw ValuationException.Forbidden("این ارزیابی متعلق به شما نیست.");

        var isPro = await entitlements.IsProAsync(currentUser.UserId, ct);
        if (!currentUser.IsStaff &&
            !await entitlements.CanViewAdvancedAsync(currentUser.UserId, row.ToolCode, isPro, ct))
            throw ValuationException.Forbidden(
                "گزارش کامل مخصوص اشتراک Pro یا خریداران بخش پیشرفتهٔ همین ابزار است.");

        var standard = ToolStandards.For(row.ToolCode);
        var html = Build(row, standard);
        return new ToolSubmissionReport($"{row.ToolCode}-{row.Id:N}.html", html);
    }

    private static string Build(ToolSubmissionRow row, ToolStandardRow? standard)
    {
        var sb = new StringBuilder();
        string E(string? v) => WebUtility.HtmlEncode(v ?? string.Empty);

        sb.Append("<!doctype html><html lang=\"fa\" dir=\"rtl\"><head><meta charset=\"utf-8\">");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        sb.Append($"<title>{E(row.ToolCode)} — گزارش ارزش‌گذاری</title><style>");
        sb.Append("body{font-family:Tahoma,system-ui,sans-serif;margin:32px auto;max-width:820px;color:#1c1c1c;line-height:1.85}");
        sb.Append("h1{font-size:20px;margin:0 0 4px}h2{font-size:14px;margin:22px 0 8px;border-bottom:1px solid #ddd;padding-bottom:4px}");
        sb.Append("table{width:100%;border-collapse:collapse;font-size:12px}td,th{border:1px solid #e2e2e2;padding:5px 8px;text-align:right;vertical-align:top}");
        sb.Append("th{background:#f7f7f7;font-weight:700}.meta{font-size:12px;color:#555}.warn{background:#fff8e6;border:1px solid #f0d9a0;padding:10px;font-size:12px;border-radius:6px}");
        sb.Append("ol,ul{font-size:12px;padding-inline-start:18px}@media print{body{margin:0}button{display:none}}");
        sb.Append("</style></head><body>");

        sb.Append("<h1>گزارش ارزش‌گذاری</h1>");
        sb.Append($"<div class=\"meta\">{E(row.ToolCode)} · {E(row.Name)} · {row.CreatedAt:yyyy-MM-dd}</div>");
        sb.Append($"<div class=\"meta\">شرکت: {E(row.CompanyName)} · حوزه: {E(row.Industry)} · زبان: {E(row.Language)}</div>");
        sb.Append($"<div class=\"meta\">امتیاز کل: {(row.OverallScore?.ToString("0.##", CultureInfo.InvariantCulture) ?? "—")}</div>");

        if (standard is not null)
        {
            sb.Append("<h2>استاندارد و روش</h2>");
            sb.Append($"<p class=\"meta\">{E(standard.Standard)}{(standard.Version is null ? "" : $" ({E(standard.Version)})")}</p>");
            if (standard.Methods.Length > 0)
                sb.Append($"<p class=\"meta\">روش‌های موتور: {E(string.Join(" · ", standard.Methods))}</p>");
            sb.Append("<table><thead><tr><th>عنصر استاندارد</th><th>وضعیت</th><th>توضیح</th></tr></thead><tbody>");
            foreach (var el in standard.Elements)
                sb.Append($"<tr><td>{E(el.Name)}</td><td>{E(el.Status)}</td><td>{E(el.Note)}</td></tr>");
            sb.Append("</tbody></table>");
        }

        AppendSection(sb, "ورودی‌های ثبت‌شده", row.InputJson, "input", E);
        AppendSection(sb, "خروجی محاسبه‌شده", row.ResultJson, "result", E);

        sb.Append("<h2>محدودیت‌ها</h2><div class=\"warn\">");
        sb.Append("این گزارش خروجی خودکار موتور است. مفروضات از ورودی‌هایی است که کاربر ثبت کرده و ");
        sb.Append("جایگزین گزارش کارشناسی امضاشده یا نظر رسمی ارزیاب دارای مجوز نیست. ");
        sb.Append("برای استفاده به‌عنوان وثیقه یا سند رسمی، تأیید کارشناس مجاز لازم است.");
        sb.Append("</div>");

        sb.Append("<p class=\"meta\">تولیدشده توسط سامانه ارزیابی و ارزش‌گذاری — ");
        sb.Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture));
        sb.Append("</p>");
        sb.Append("<button onclick=\"window.print()\">چاپ / ذخیره به‌صورت PDF</button>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static void AppendSection(
        StringBuilder sb, string title, string json, string prefix, Func<string?, string> encode)
    {
        var pairs = new List<(string Path, string Value)>();
        Collect(json, prefix, pairs);
        if (pairs.Count == 0) return;

        sb.Append($"<h2>{encode(title)}</h2>");
        sb.Append("<table><thead><tr><th>مسیر</th><th>مقدار</th></tr></thead><tbody>");
        foreach (var (path, value) in pairs)
            sb.Append($"<tr><td>{encode(path)}</td><td>{encode(value)}</td></tr>");
        sb.Append("</tbody></table>");
    }

    private static void Collect(string json, string section, List<(string, string)> into)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            using var doc = JsonDocument.Parse(json);
            Walk(doc.RootElement, section, string.Empty, into);
        }
        catch (JsonException)
        {
            into.Add((section, json));
        }
    }

    private static void Walk(JsonElement el, string section, string path, List<(string, string)> into)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in el.EnumerateObject())
                    Walk(prop.Value, section, path.Length == 0 ? prop.Name : $"{path}.{prop.Name}", into);
                break;
            case JsonValueKind.Array:
                var i = 0;
                foreach (var item in el.EnumerateArray())
                    Walk(item, section, $"{path}[{i++}]", into);
                break;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                into.Add((path, string.Empty));
                break;
            default:
                into.Add((path, el.ToString()));
                break;
        }
    }
}
