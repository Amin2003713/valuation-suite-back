using Domain.Results;

namespace Persistence.Repositories.Results;

public class AssessmentResultCommandRepository(
    WriteOnlyDbContext dbContext,
    IdentityService identityService,
    ILogger<CommandRepository<AssessmentResult>> logger
)
    : CommandRepository<AssessmentResult>(dbContext, identityService, logger),
        IAssessmentResultCommandRepository;
