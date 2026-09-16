using Domain.Answers;

namespace Persistence.Repositories.Attempts;

public class AnswerCommandRepository(
    WriteOnlyDbContext dbContext,
    IdentityService identityService,
    ILogger<CommandRepository<Answer>> logger
)
    : CommandRepository<Answer>(dbContext, identityService, logger),
        IAnswerCommandRepository;
