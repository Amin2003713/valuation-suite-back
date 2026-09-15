using MediatR;
using Domain.Evaluation;

namespace Application.Results.Commands;

public record CalculateResultCommand(Guid AttemptId) : IRequest<Guid>;
public record GetComputedResultCommand(Guid AttemptId) : IRequest<Application.Assessments.Responses.AssessmentResultResponse?>;
