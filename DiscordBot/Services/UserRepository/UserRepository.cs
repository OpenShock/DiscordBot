using Microsoft.EntityFrameworkCore;
using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.DiscordBot.Services.UserRepository;

public sealed class UserRepository : IUserRepository
{
    private readonly OpenShockDiscordContext _dbContext;

    public UserRepository(OpenShockDiscordContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public Task<User?> GetUser(ulong discordId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.FirstOrDefaultAsync(x => x.DiscordId == discordId, cancellationToken: cancellationToken);
    }
}