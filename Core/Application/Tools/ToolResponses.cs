namespace Application.Tools;

public record ToolFormSummary(
    string ToolCode,
    string Title,
    string? Description,
    string Kind,
    int SortOrder);

public record ToolFormResponse(
    string ToolCode,
    string Title,
    string? Description,
    string Kind,
    JsonElement Schema);

public record ToolRunResponse(
    Guid SubmissionId,
    string ToolCode,
    JsonElement Result,
    double? OverallScore);

public record ToolSubmissionResponse(
    Guid Id,
    string ToolCode,
    string? Name,
    JsonElement Input,
    JsonElement Result,
    double? OverallScore,
    DateTime CreatedAt);
