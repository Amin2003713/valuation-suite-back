using MediatR;

namespace Application.Tools;

/// <summary>Runs a tool with user input; returns the backend-computed result and (optionally) stores a submission.</summary>
public class RunToolCommand : IRequest<ToolRunResponse>
{
    public string ToolCode { get; set; } = string.Empty;
    public JsonElement Input { get; set; }

    /// <summary>
    ///     When false the tool is computed but no submission row is written — a
    ///     "preview" run. The client persists on explicit save or when leaving the
    ///     tool page, so typing does not spam the database. Defaults to true so
    ///     existing callers keep storing their runs.
    /// </summary>
    public bool Persist { get; set; } = true;

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
