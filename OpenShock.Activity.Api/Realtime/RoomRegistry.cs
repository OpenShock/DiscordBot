using System.Collections.Concurrent;

namespace OpenShock.Activity.Api.Realtime;

/// <summary>A participant currently connected to a Discord Activity room instance.</summary>
public sealed record Participant(ulong DiscordId, string Name, bool AllowRoomShocks);

/// <summary>
/// In-memory tracking of who is connected to which Discord Activity instance. Source of truth for the
/// "same room" check in the control endpoint.
/// NOTE: single-instance only. Running more than one API replica requires a shared store + SignalR
/// Redis backplane (see plan).
/// </summary>
public sealed class RoomRegistry
{
    // instanceId -> (discordId -> participant)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<ulong, Participant>> _rooms = new();

    // connectionId -> (instanceId, discordId)
    private readonly ConcurrentDictionary<string, (string InstanceId, ulong DiscordId)> _connections = new();

    public void Add(string connectionId, string instanceId, Participant participant)
    {
        _connections[connectionId] = (instanceId, participant.DiscordId);
        var room = _rooms.GetOrAdd(instanceId, _ => new ConcurrentDictionary<ulong, Participant>());
        room[participant.DiscordId] = participant;
    }

    /// <summary>Removes a connection. Returns its (instanceId, discordId), and whether that user has no
    /// remaining connections in the instance (i.e. they truly left the room).</summary>
    public (string InstanceId, ulong DiscordId, bool UserLeft)? Remove(string connectionId)
    {
        if (!_connections.TryRemove(connectionId, out var info)) return null;

        var userLeft = !_connections.Values.Any(c => c.InstanceId == info.InstanceId && c.DiscordId == info.DiscordId);
        if (userLeft && _rooms.TryGetValue(info.InstanceId, out var room))
        {
            room.TryRemove(info.DiscordId, out _);
            if (room.IsEmpty) _rooms.TryRemove(info.InstanceId, out _);
        }

        return (info.InstanceId, info.DiscordId, userLeft);
    }

    public IReadOnlyCollection<Participant> GetRoom(string instanceId)
        => _rooms.TryGetValue(instanceId, out var room) ? room.Values.ToArray() : [];

    public bool IsInRoom(string instanceId, ulong discordId)
        => _rooms.TryGetValue(instanceId, out var room) && room.ContainsKey(discordId);

    public bool AreInSameRoom(string instanceId, ulong a, ulong b)
        => IsInRoom(instanceId, a) && IsInRoom(instanceId, b);

    public void UpdateConsent(ulong discordId, bool allowRoomShocks)
    {
        foreach (var room in _rooms.Values)
        {
            if (room.TryGetValue(discordId, out var existing))
                room[discordId] = existing with { AllowRoomShocks = allowRoomShocks };
        }
    }
}
