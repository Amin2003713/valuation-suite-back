using MediatR;

namespace Application.Tools;

/// <summary>Runs a tool with user input; returns the backend-computed result and stores a submission.</summary>
public class RunToolCommand : IRequest<ToolRunResponse>
{
    public string ToolCode { get; set; } = string.Empty;
    public JsonElement Input { get; set; }

    /// <summary>
    ///     Run-on-behalf context (admin flow): attribute the submission to a
    ///     client/company instead of the authenticated staff member. Null = normal run.
    /// </summary>
    public RunForContext? RunFor { get; set; }
}

/// <summary>Who a run is performed for (admin "run for client" flow).</summary>
public sealed class RunForContext
{
    public Guid? CompanyId { get; set; }
    public Guid? UserId { get; set; }
    public string? Note { get; set; }
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
