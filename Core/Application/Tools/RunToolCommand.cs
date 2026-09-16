using MediatR;

namespace Application.Tools;

/// <summary>Runs a tool with user input; returns the backend-computed result and stores a submission.</summary>
public class RunToolCommand : IRequest<ToolRunResponse>
{
    public string ToolCode { get; set; } = string.Empty;
    public JsonElement Input { get; set; }
}

public class GetToolFormQuery : IRequest<ToolFormResponse?>
{
    public string ToolCode { get; set; } = string.Empty;
}

public class GetToolFormsQuery : IRequest<List<ToolFormSummary>>
{
}

public class GetToolSubmissionsQuery : IRequest<List<ToolSubmissionResponse>>
{
    public string? ToolCode { get; set; }
}
