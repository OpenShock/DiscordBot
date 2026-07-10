using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Activity.Api.Auth;
using OpenShock.DiscordBot.OpenShockDiscordDb;

namespace OpenShock.Activity.Api.Realtime;

/// <summary>Roster/join wire shape sent to clients: presence plus the target's room-shock consent.</summary>
public sealed record Participant(ulong DiscordId, string Name, bool AllowRoomShocks);

public sealed record ShockDeliveredEvent(ulong FromDiscordId, string FromName, ulong ToDiscordId, byte Intensity, float DurationSeconds, string Type);
public sealed record ConsentChangedEvent(ulong DiscordId, bool AllowRoomShocks);

/// <summary>
/// Strongly-typed server→client contract for the room hub. Each method name is the SignalR event the
/// frontend subscribes to, and its parameter is the payload — so broadcasts are compile-time checked
/// instead of using magic strings. Keep the method names in sync with the frontend's listeners.
/// </summary>
public interface IRoomClient
{
    /// <summary>Full roster sent to a client immediately after it connects.</summary>
    Task Roster(IReadOnlyList<Participant> participants);

    /// <summary>A user's first connection joined the room.</summary>
    Task ParticipantJoined(Participant participant);

    /// <summary>A user's last connection left the room.</summary>
    Task ParticipantLeft(ulong discordId);

    /// <summary>A user changed their room-shock consent.</summary>
    Task ConsentChanged(ConsentChangedEvent consent);

    /// <summary>A shock/vibrate/sound was delivered to someone in the room.</summary>
    Task ShockDelivered(ShockDeliveredEvent shock);
}

/// <summary>
/// Realtime room. Clients connect with <c>?instanceId=&lt;discord activity instance id&gt;</c> and a JWT;
/// everyone in the same instance shares a SignalR group named by the instance id.
/// </summary>
[Authorize]
public sealed class RoomHub : Hub<IRoomClient>
{
    private readonly IRoomRegistry _registry;
    private readonly OpenShockDiscordContext _db;

    public RoomHub(IRoomRegistry registry, OpenShockDiscordContext db)
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

        var isNewMember = await _registry.AddAsync(Context.ConnectionId, instanceId, discordId, name);
        await Groups.AddToGroupAsync(Context.ConnectionId, instanceId);

        await Clients.Caller.Roster(await BuildRosterAsync(instanceId));

        // Only announce a join on the user's first connection (a second tab shouldn't re-broadcast).
        if (isNewMember)
        {
            var allowRoomShocks = await _db.Users
                .Where(u => u.DiscordId == discordId)
                .Select(u => u.AllowRoomShocks)
                .FirstOrDefaultAsync();
            await Clients.OthersInGroup(instanceId)
                .ParticipantJoined(new Participant(discordId, name, allowRoomShocks));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var removed = await _registry.RemoveAsync(Context.ConnectionId);
        if (removed is { } r)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, r.InstanceId);
            if (r.UserLeft)
                await Clients.Group(r.InstanceId).ParticipantLeft(r.DiscordId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Presence comes from the registry (id + name); consent is read fresh from the DB so the
    /// roster is never stale — the registry no longer caches it.</summary>
    private async Task<IReadOnlyList<Participant>> BuildRosterAsync(string instanceId)
    {
        var members = await _registry.GetRoomAsync(instanceId);
        if (members.Count == 0) return [];

        var ids = members.Select(m => m.DiscordId).ToArray();
        var consent = await _db.Users
            .Where(u => ids.Contains(u.DiscordId))
            .Select(u => new { u.DiscordId, u.AllowRoomShocks })
            .ToDictionaryAsync(u => u.DiscordId, u => u.AllowRoomShocks);

        return members
            .Select(m => new Participant(m.DiscordId, m.Name, consent.GetValueOrDefault(m.DiscordId)))
            .ToArray();
    }
}
