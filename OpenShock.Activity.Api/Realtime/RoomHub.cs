using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Activity.Api.Auth;
using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.Activity.Api.Realtime;

/// <summary>
/// Client→server / server→client event names for the room hub. Kept as constants so the frontend and
/// backend stay in sync.
/// </summary>
public static class RoomEvents
{
    public const string Roster = "roster";
    public const string ParticipantJoined = "participantJoined";
    public const string ParticipantLeft = "participantLeft";
    public const string ConsentChanged = "consentChanged";
    public const string ShockDelivered = "shockDelivered";
}

public sealed record ShockDeliveredEvent(ulong FromDiscordId, string FromName, ulong ToDiscordId, byte Intensity, float DurationSeconds, string Type);
public sealed record ConsentChangedEvent(ulong DiscordId, bool AllowRoomShocks);

/// <summary>
/// Realtime room. Clients connect with <c>?instanceId=&lt;discord activity instance id&gt;</c> and a JWT;
/// everyone in the same instance shares a SignalR group named by the instance id.
/// </summary>
[Authorize]
public sealed class RoomHub : Hub
{
    private readonly RoomRegistry _registry;
    private readonly OpenShockDiscordContext _db;

    public RoomHub(RoomRegistry registry, OpenShockDiscordContext db)
    {
        _registry = registry;
        _db = db;
    }

    public override async Task OnConnectedAsync()
    {
        var instanceId = Context.GetHttpContext()?.Request.Query["instanceId"].ToString();
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            Context.Abort();
            return;
        }

        var discordId = Context.User!.GetDiscordId();
        var name = Context.User!.GetDisplayName();

        var allowRoomShocks = await _db.Users
            .Where(u => u.DiscordId == discordId)
            .Select(u => u.AllowRoomShocks)
            .FirstOrDefaultAsync();

        var participant = new Participant(discordId, name, allowRoomShocks);
        _registry.Add(Context.ConnectionId, instanceId, participant);
        await Groups.AddToGroupAsync(Context.ConnectionId, instanceId);

        await Clients.Caller.SendAsync(RoomEvents.Roster, _registry.GetRoom(instanceId));
        await Clients.OthersInGroup(instanceId).SendAsync(RoomEvents.ParticipantJoined, participant);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var removed = _registry.Remove(Context.ConnectionId);
        if (removed is { } r)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, r.InstanceId);
            if (r.UserLeft)
                await Clients.Group(r.InstanceId).SendAsync(RoomEvents.ParticipantLeft, r.DiscordId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
