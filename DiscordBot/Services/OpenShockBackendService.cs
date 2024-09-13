using Discord;
using Microsoft.EntityFrameworkCore;
using OpenShock.DiscordBot.OpenShockDiscordDb;
using OpenShock.DiscordBot.Utils;
using OpenShock.SDK.CSharp;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.DiscordBot.Services;

public interface IOpenShockBackendService
{
    public Task ControlAllShockers(ulong discordUserId, byte intensity, ushort duration, ControlType controlType);
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
}