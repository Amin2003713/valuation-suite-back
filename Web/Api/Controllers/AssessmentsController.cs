using ApiFramework.Controller;
using Application.Assessments.Commands.AddQuestion;
using Application.Assessments.Commands.AddStep;
using Application.Assessments.Commands.ArchiveAssessment;
using Application.Assessments.Commands.CreateAssessment;
using Application.Assessments.Commands.CreateVersion;
using Application.Assessments.Commands.SetQuestionCalculation;
using Application.Assessments.Commands.SetQuestionMathExpression;
using Application.Assessments.Commands.SetQuestionOptions;
using Application.Assessments.Commands.SetQuestionValidations;
using Application.Assessments.Commands.SetQuestionVisibility;
using Application.Assessments.Commands.UpdateAssessment;
using Application.Assessments.Commands.UpdateQuestion;
using Application.Assessments.Commands.UpdateStep;
using Application.Assessments.Queries.GetAssessment;
using Application.Assessments.Queries.GetAssessmentVersion;
using Application.Questions.Queries.GetQuestionTypes;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Controllers;

[ApiController]
[Route("api/assessments")]
public class AssessmentsController : ApiBaseController
{
    [HttpGet]
    public Task<IActionResult> GetAssessments([FromQuery] GetAssessmentsQuery query, CancellationToken ct)
        => ExecuteAsync(query, ct);

    [HttpGet("{id:guid}", Name = "GetAssessmentById")]
    public Task<IActionResult> GetAssessment(Guid id, CancellationToken ct)
        => ExecuteAsync(new GetAssessmentQuery { Id = id }, ct);

    [HttpPost]
    public Task<IActionResult> CreateAssessment([FromBody] CreateAssessmentCommand command, CancellationToken ct)
        => ExecuteCreateAsync(command, nameof(GetAssessment), ct);

    [HttpPut("{id:guid}")]
    public Task<IActionResult> UpdateAssessment(Guid id, [FromBody] UpdateAssessmentCommand command, CancellationToken ct)
    {
        command.Id = id;
        return ExecuteAsync(command, ct);
    }

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> DeleteAssessment(Guid id, CancellationToken ct)
        => ExecuteAsync(new DeleteAssessmentCommand { Id = id }, ct);

    [HttpPost("{id:guid}/archive")]
    public Task<IActionResult> ArchiveAssessment(Guid id, CancellationToken ct)
        => ExecuteAsync(new ArchiveAssessmentCommand { AssessmentId = id }, ct);

    [HttpPost("{id:guid}/versions")]
    public Task<IActionResult> CreateVersion(Guid id, CancellationToken ct)
        => ExecuteCreateAsync(new CreateVersionCommand { AssessmentId = id }, nameof(GetAssessmentVersion), ct);

    [HttpPost("{assessmentId:guid}/versions/{versionId:guid}/publish")]
    public Task<IActionResult> PublishVersion(Guid assessmentId, Guid versionId, CancellationToken ct)
        => ExecuteAsync(new PublishAssessmentCommand { AssessmentId = assessmentId, VersionId = versionId }, ct);

    [HttpGet("{id:guid}/versions")]
    public Task<IActionResult> GetVersions(Guid id, CancellationToken ct)
        => ExecuteAsync(new GetAssessmentVersionsQuery { AssessmentId = id }, ct);

    [HttpGet("versions/{id:guid}", Name = "GetAssessmentVersion")]
    public Task<IActionResult> GetAssessmentVersion(Guid id, CancellationToken ct)
        => ExecuteAsync(new GetAssessmentVersionQuery { Id = id }, ct);

    [HttpGet("{assessmentId:guid}/client")]
    public Task<IActionResult> GetForClient(Guid assessmentId, CancellationToken ct)
        => ExecuteAsync(new GetAssessmentForClientQuery { AssessmentId = assessmentId }, ct);

    [HttpGet("{assessmentId:guid}/preview")]
    public Task<IActionResult> GetPreview(Guid assessmentId, [FromQuery] Guid? versionId, CancellationToken ct)
        => ExecuteAsync(new GetAssessmentPreviewQuery { AssessmentId = assessmentId, VersionId = versionId ?? Guid.Empty }, ct);

    // ------------------------------------------------------------------
    // Steps
    // ------------------------------------------------------------------

    [HttpPost("versions/{versionId:guid}/steps")]
    public Task<IActionResult> AddStep(Guid versionId, [FromBody] AddStepCommand command, CancellationToken ct)
    {
        command.VersionId = versionId;
        return ExecuteCreateAsync(command, nameof(GetAssessmentVersion), ct);
    }

    [HttpPut("steps/{id:guid}")]
    public Task<IActionResult> UpdateStep(Guid id, [FromBody] UpdateStepCommand command, CancellationToken ct)
    {
        command.Id = id;
        return ExecuteAsync(command, ct);
    }

    [HttpPut("versions/{versionId:guid}/steps/reorder")]
    public Task<IActionResult> ReorderSteps(Guid versionId, [FromBody] ReorderStepsCommand command, CancellationToken ct)
    {
        command.VersionId = versionId;
        return ExecuteAsync(command, ct);
    }

    [HttpDelete("steps/{id:guid}")]
    public Task<IActionResult> RemoveStep(Guid id, CancellationToken ct)
        => ExecuteAsync(new RemoveStepCommand { StepId = id }, ct);

    // ------------------------------------------------------------------
    // Questions
    // ------------------------------------------------------------------

    [HttpPost("steps/{stepId:guid}/questions")]
    public Task<IActionResult> AddQuestion(Guid stepId, [FromBody] AddQuestionCommand command, CancellationToken ct)
    {
        command.StepId = stepId;
        return ExecuteCreateAsync(command, nameof(GetAssessmentVersion), ct);
    }

    [HttpPut("questions/{id:guid}")]
    public Task<IActionResult> UpdateQuestion(Guid id, [FromBody] UpdateQuestionCommand command, CancellationToken ct)
    {
        command.Id = id;
        return ExecuteAsync(command, ct);
    }

    [HttpDelete("questions/{id:guid}")]
    public Task<IActionResult> RemoveQuestion(Guid id, CancellationToken ct)
        => ExecuteAsync(new RemoveQuestionCommand { QuestionId = id }, ct);

    [HttpPut("questions/{id:guid}/options")]
    public Task<IActionResult> SetOptions(Guid id, [FromBody] SetQuestionOptionsCommand command, CancellationToken ct)
    {
        command.QuestionId = id;
        return ExecuteAsync(command, ct);
    }

    [HttpPut("questions/{id:guid}/validations")]
    public Task<IActionResult> SetValidations(Guid id, [FromBody] SetQuestionValidationsCommand command, CancellationToken ct)
    {
        command.QuestionId = id;
        return ExecuteAsync(command, ct);
    }

    [HttpPut("questions/{id:guid}/visibility")]
    public Task<IActionResult> SetVisibility(Guid id, [FromBody] SetQuestionVisibilityCommand command, CancellationToken ct)
    {
        command.QuestionId = id;
        return ExecuteAsync(command, ct);
    }

    [HttpPut("questions/{id:guid}/calculation")]
    public Task<IActionResult> SetCalculation(Guid id, [FromBody] SetQuestionCalculationCommand command, CancellationToken ct)
    {
        command.QuestionId = id;
        return ExecuteAsync(command, ct);
    }

    [HttpPut("questions/{id:guid}/math-expression")]
    public Task<IActionResult> SetMathExpression(Guid id, [FromBody] SetQuestionMathExpressionCommand command, CancellationToken ct)
    {
        command.QuestionId = id;
        return ExecuteAsync(command, ct);
    }

    // ------------------------------------------------------------------
    // Question catalogue
    // ------------------------------------------------------------------

    [HttpGet("question-types")]
    public Task<IActionResult> GetQuestionTypes(CancellationToken ct)
        => ExecuteAsync(new GetQuestionTypeDetailsQuery(), ct);
}
