using System.Globalization;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace RequestHandlers.Tools;

/// <summary>
///     Downloads one stored run as CSV: metadata, every submitted input flattened
///     to dotted paths, and every result value. Plain CSV on purpose — it is a
///     faithful export of what the engine computed, not a formatted valuation
///     report, so no standard-compliance claim is attached to it.
/// </summary>
public sealed record ExportToolSubmissionQuery(Guid SubmissionId) : IRequest<ToolSubmissionExport>;

public sealed record ToolSubmissionExport(string FileName, string Content, string ContentType);

public sealed class ExportToolSubmissionQueryHandler(
    IToolSubmissionQueries submissions,
    ICurrentUserAccessor currentUser) : IRequestHandler<ExportToolSubmissionQuery, ToolSubmissionExport>
{
    public async Task<ToolSubmissionExport> Handle(ExportToolSubmissionQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw ValuationException.Forbidden("برای دریافت خروجی ابتدا وارد حساب خود شوید.");

        var row = await submissions.All
            .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, ct)
            ?? throw ValuationException.NotFound("نتیجه یافت نشد.");

        // Owners only; staff may export for support purposes.
        if (row.UserId != userId && !currentUser.IsStaff)
            throw ValuationException.Forbidden("این ارزیابی متعلق به شما نیست.");

        var csv = new StringBuilder();
        csv.AppendLine("section,path,value");

        void Line(string section, string path, string value) =>
            csv.Append(section).Append(',')
               .Append('"').Append(path.Replace("\"", "\"\"")).Append("\",")
               .Append('"').Append(value.Replace("\"", "\"\"")).Append("\"")
               .AppendLine();

        Line("meta", "toolCode", row.ToolCode);
        Line("meta", "name", row.Name ?? string.Empty);
        Line("meta", "overallScore", row.OverallScore?.ToString("0.###", CultureInfo.InvariantCulture) ?? string.Empty);
        Line("meta", "createdAt", row.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        Line("meta", "language", row.Language);
        Line("meta", "company", row.CompanyName ?? string.Empty);
        Line("meta", "industry", row.Industry ?? string.Empty);

        Flatten(row.InputJson, "input", Line);
        Flatten(row.ResultJson, "result", Line);

        return new ToolSubmissionExport(
            $"{row.ToolCode}-{row.Id:N}.csv",
            csv.ToString(),
            "text/csv; charset=utf-8");
    }

    /// <summary>Walks a stored JSON document and emits every leaf as a dotted path.</summary>
    private static void Flatten(string json, string section, Action<string, string, string> line)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            using var doc = JsonDocument.Parse(json);
            Walk(doc.RootElement, section, string.Empty, line);
        }
        catch (JsonException)
        {
            // Legacy/corrupt payload: export it verbatim rather than failing the download.
            line(section, "(raw)", json);
        }
    }

    private static void Walk(
        JsonElement el, string section, string path, Action<string, string, string> line)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in el.EnumerateObject())
                    Walk(prop.Value, section, path.Length == 0 ? prop.Name : $"{path}.{prop.Name}", line);
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in el.EnumerateArray())
                    Walk(item, section, $"{path}[{index++}]", line);
                break;

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                line(section, path, string.Empty);
                break;

            default:
                line(section, path, el.ToString());
                break;
        }
    }
}
