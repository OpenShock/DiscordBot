using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.DiscordBot.Services.UserRepository;

public interface IUserRepository
{
    /// <summary>
    /// Get a user by their Discord ID
    /// </summary>
    /// <param name="discordId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<User?> GetUser(ulong discordId, CancellationToken cancellationToken = default);
}