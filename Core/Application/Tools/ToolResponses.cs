namespace Application.Tools;

public record ToolFormSummary(
    string ToolCode,
    string Title,
    string? Description,
    string Kind,
    int SortOrder,
    string Route,
    long AdvancedPriceToman,
    long AdvicePriceToman);

public record ToolFormResponse(
    string ToolCode,
    string Title,
    string? Description,
    string Kind,
    JsonElement Schema,
    string Route,
    long AdvancedPriceToman,
    long AdvicePriceToman);

public record ToolRunResponse(
    Guid SubmissionId,
    string ToolCode,
    JsonElement Result,
    double? OverallScore,
    bool AdvancedIncluded);

public record ToolSubmissionResponse(
    Guid Id,
    string ToolCode,
    string? Name,
    JsonElement Input,
    JsonElement Result,
    double? OverallScore,
    DateTime CreatedAt);
