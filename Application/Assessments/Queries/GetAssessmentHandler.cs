using MediatR;
using Domain.Assessments;
using Application.Common;
using Application.Assessments.Mappers;
using Application.Assessments.Responses;

namespace Application.Assessments.Queries;

public class GetAssessmentHandler : IRequestHandler<GetAssessmentQuery, AssessmentResponse?>
{
    private readonly IAssessmentRepository _repo;

    public GetAssessmentHandler(IAssessmentRepository repo) => _repo = repo;

    public async Task<AssessmentResponse?> Handle(GetAssessmentQuery request, CancellationToken ct)
        => (await _repo.GetByIdAsync(request.Id, ct))?.ToResponse();
}
