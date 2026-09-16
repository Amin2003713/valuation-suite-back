using Domain.Users;

namespace Persistence.Repositories.Users;

public class UserCommandRepository(
    WriteOnlyDbContext dbContext,
    IdentityService identityService,
    ILogger<CommandRepository<ApplicationUser>> logger
)
    : CommandRepository<ApplicationUser>(dbContext, identityService, logger),
        IUserCommandRepository;
