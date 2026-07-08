using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Utils;
using OpenShock.SDK.CSharp.Errors;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.DiscordBot.Services;

/// <summary>How a control command spreads across a user's registered shockers.</summary>
public enum ShockMode
{
    Random = 0,
    All = 1
}

public interface IOpenShockBackendService
{
    /// <summary>Fire-and-forget control of every shocker a user owns (used by the profanity feature).</summary>
    public Task ControlAllShockers(ulong discordUserId, byte intensity, ushort duration, ControlType controlType);

    /// <summary>
    /// Control the given target user's shockers and surface the OpenShock result so callers can report
    /// the same error cases the bot does. Does not touch the database — pass already-loaded entities.
    /// </summary>
    public Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission, UnauthenticatedError>>
        ControlShockers(User targetUser, IReadOnlyList<UsersShocker> shockers, ControlType type, byte intensity,
            ushort durationMs, ShockMode mode, string customName);
}

public sealed class OpenShockBackendService : IOpenShockBackendService
{
    private readonly OpenShockDiscordContext _db;

    public OpenShockBackendService(OpenShockDiscordContext db)
    {
        _db = db;
    }

    public async Task ControlAllShockers(ulong discordUserId, byte intensity, ushort duration, ControlType controlType)
    {
        var user = await _db.Users.Where(x => x.DiscordId == discordUserId).Select(x => new
        {
            User = x,
            Shockers = x.UsersShockers
        }).FirstOrDefaultAsync();
        if (user == null) return;

        // Shock the user
        var apiClient = user.User.GetApiClient();

        var shocks = user.Shockers.Select(x => new Control
        {
            Id = x.ShockerId,
            Duration = duration,
            Intensity = intensity,
            Type = controlType
        });

        var controlResponse = await apiClient.ControlShocker(new ControlRequest
        {
            Shocks = shocks,
            CustomName = "Discord"
        });
    }

    public Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission, UnauthenticatedError>>
        ControlShockers(User targetUser, IReadOnlyList<UsersShocker> shockers, ControlType type, byte intensity,
            ushort durationMs, ShockMode mode, string customName)
    {
        var client = targetUser.GetApiClient();

        IEnumerable<Control> shocks;
        if (mode == ShockMode.Random)
        {
            var randomShocker = shockers[Random.Shared.Next(shockers.Count)];
            shocks =
            [
                new Control
                {
                    Id = randomShocker.ShockerId,
                    Duration = durationMs,
                    Intensity = intensity,
                    Type = type
                }
            ];
        }
        else
        {
            shocks = shockers.Select(x => new Control
            {
                Id = x.ShockerId,
                Duration = durationMs,
                Intensity = intensity,
                Type = type
            });
        }

        return client.ControlShocker(new ControlRequest
        {
            CustomName = customName,
            Shocks = shocks
        });
    }
}
