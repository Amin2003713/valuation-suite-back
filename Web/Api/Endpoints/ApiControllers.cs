using Microsoft.AspNetCore.Mvc;
using MediatR;
using Application.Assessments.Commands;
using Application.Assessments.Queries;
using Application.Assessments.Responses;
using Application.Attempts.Commands;
using Application.Results.Commands;
using Application.Companies.Commands;
using Application.Companies.Queries;
using Application.Common;
using Domain.Common;

namespace Web.Api;

[ApiController]
[Route("api/[controller]")]
public class AssessmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AssessmentsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateAssessment([FromBody] CreateAssessmentCommand command)
        => Ok(await _mediator.Send(command));

    [HttpGet]
    public async Task<ActionResult<PagedResult<AssessmentResponse>>> GetAssessments([FromQuery] Guid companyId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _mediator.Send(new GetAssessmentsQuery(companyId, page, pageSize)));

    [HttpGet("{id}")]
    public async Task<ActionResult<AssessmentResponse?>> GetAssessment(Guid id)
        => Ok(await _mediator.Send(new GetAssessmentQuery(id)));

    [HttpPost("{id}/versions")]
    public async Task<ActionResult<Guid>> CreateVersion(Guid id, [FromBody] CreateVersionCommand command)
    {
        command = command with { AssessmentId = id };
        return Ok(await _mediator.Send(command));
    }

    [HttpGet("{id}/versions")]
    public async Task<ActionResult<List<AssessmentVersionResponse>>> GetVersions(Guid id)
        => Ok(await _mediator.Send(new GetAssessmentVersionsQuery(id)));

    [HttpPost("{id}/publish")]
    public async Task<ActionResult> Publish(Guid id, [FromBody] PublishAssessmentCommand command)
    {
        command = command with { AssessmentId = id };
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{id}/archive")]
    public async Task<ActionResult> Archive(Guid id, [FromBody] ArchiveAssessmentCommand command)
    {
        command = command with { AssessmentId = id };
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpGet("assessment-versions/{id}")]
    public async Task<ActionResult<AssessmentVersionResponse?>> GetVersion(Guid id)
        => Ok(await _mediator.Send(new GetAssessmentVersionQuery(id)));

    [HttpGet("assessments/{id}/client")]
    public async Task<ActionResult<AssessmentVersionResponse?>> GetForClient(Guid id)
        => Ok(await _mediator.Send(new GetAssessmentForClientQuery(id)));

    [HttpGet("assessments/{id}/preview")]
    public async Task<ActionResult<AssessmentVersionResponse?>> GetPreview(Guid id, [FromQuery] Guid versionId)
        => Ok(await _mediator.Send(new GetAssessmentPreviewQuery(id, versionId)));
}

[ApiController]
[Route("api/[controller]")]
public class StepsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StepsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("assessment-versions/{versionId}/steps")]
    public async Task<ActionResult<Guid>> AddStep(Guid versionId, [FromBody] AddStepCommand command)
    {
        command = command with { VersionId = versionId };
        return Ok(await _mediator.Send(command));
    }
}

[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuestionsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("steps/{stepId}/questions")]
    public async Task<ActionResult<Guid>> AddQuestion(Guid stepId, [FromBody] AddQuestionCommand command)
    {
        command = command with { StepId = stepId };
        return Ok(await _mediator.Send(command));
    }

    [HttpPut("questions/{id}")]
    public async Task<ActionResult> UpdateQuestion(Guid id, [FromBody] UpdateQuestionCommand command)
    {
        command = command with { Id = id };
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("questions/{id}/options")]
    public async Task<ActionResult> SetOptions(Guid id, [FromBody] SetQuestionOptionsCommand command)
    {
        command = command with { QuestionId = id };
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("questions/{id}/validations")]
    public async Task<ActionResult> SetValidations(Guid id, [FromBody] SetQuestionValidationsCommand command)
    {
        command = command with { QuestionId = id };
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("questions/{id}/visibility")]
    public async Task<ActionResult> SetVisibility(Guid id, [FromBody] SetQuestionVisibilityCommand command)
    {
        command = command with { QuestionId = id };
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("questions/{id}/calculation")]
    public async Task<ActionResult> SetCalculation(Guid id, [FromBody] SetQuestionCalculationCommand command)
    {
        command = command with { QuestionId = id };
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("questions/{id}/math")]
    public async Task<ActionResult> SetMathExpression(Guid id, [FromBody] SetQuestionMathExpressionCommand command)
    {
        command = command with { QuestionId = id };
        await _mediator.Send(command);
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class AttemptsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AttemptsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateAttempt([FromBody] CreateAttemptCommand command)
        => Ok(await _mediator.Send(command));

    [HttpGet("{id}")]
    public async Task<ActionResult<AttemptResponse?>> GetAttempt(Guid id)
        => Ok(await _mediator.Send(new GetAssessmentAttemptQuery(id)));

    [HttpGet("{id}/answers")]
    public async Task<ActionResult<List<AnswerResponse>>> GetAnswers(Guid id)
        => Ok(await _mediator.Send(new GetAttemptAnswersQuery(id)));

    [HttpPost("{id}/answers/batch")]
    public async Task<ActionResult> SyncAnswers(Guid id, [FromBody] SyncAnswersCommand command)
    {
        command = command with { AttemptId = id };
        await _mediator.Send(command);
        return Ok();
    }

    [HttpPost("{id}/complete")]
    public async Task<ActionResult> Complete(Guid id, [FromBody] CompleteAttemptCommand command)
    {
        command = command with { Id = id };
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{id}/abandon")]
    public async Task<ActionResult> Abandon(Guid id, [FromBody] AbandonAttemptCommand command)
    {
        command = command with { Id = id };
        await _mediator.Send(command);
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class ResultsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ResultsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("calculate")]
    public async Task<ActionResult<Guid>> Calculate([FromBody] CalculateResultCommand command)
        => Ok(await _mediator.Send(command));

    [HttpGet("{attemptId}")]
    public async Task<ActionResult<AssessmentResultResponse?>> GetResult(Guid attemptId)
        => Ok(await _mediator.Send(new GetAssessmentResultQuery(attemptId)));

    [HttpGet("assessment-results")]
    public async Task<ActionResult<PagedResult<AssessmentResultResponse>>> GetResults([FromQuery] Guid assessmentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _mediator.Send(new GetAssessmentResultsQuery(assessmentId, page, pageSize)));
}

[ApiController]
[Route("api/admin/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CompaniesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateCompany([FromBody] CreateCompanyCommand command)
        => Ok(await _mediator.Send(command));

    [HttpGet("{id}")]
    public async Task<ActionResult<CompanyResponse?>> GetCompany(Guid id)
        => Ok(await _mediator.Send(new GetCompanyQuery(id)));

    [HttpPost("{id}/upgrade")]
    public async Task<ActionResult> Upgrade(Guid id, [FromBody] UpgradeCompanyToProCommand command)
    {
        command = command with { Id = id };
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{id}/downgrade")]
    public async Task<ActionResult> Downgrade(Guid id, [FromBody] DowngradeCompanyToFreeCommand command)
    {
        command = command with { Id = id };
        await _mediator.Send(command);
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class QuestionTypesController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuestionTypesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<QuestionType>>> GetTypes()
        => Ok(await _mediator.Send(new GetQuestionTypesQuery()));
}
