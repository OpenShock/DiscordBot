using System.Collections.Concurrent;

namespace OpenShock.Activity.Api.Realtime;

/// <summary>
/// In-memory presence tracking. Single-replica only — every instance keeps its own view, so running
/// more than one API replica requires <see cref="RedisRoomRegistry"/> plus the SignalR Redis backplane.
/// </summary>
public sealed class InMemoryRoomRegistry : IRoomRegistry
{
    // instanceId -> (discordId -> member)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<ulong, RoomMember>> _rooms = new();

    // connectionId -> (instanceId, discordId)
    private readonly ConcurrentDictionary<string, (string InstanceId, ulong DiscordId)> _connections = new();

    public Task<bool> AddAsync(string connectionId, string instanceId, ulong discordId, string name, CancellationToken ct = default)
    {
        var room = _rooms.GetOrAdd(instanceId, _ => new ConcurrentDictionary<ulong, RoomMember>());
        var isNewMember = !room.ContainsKey(discordId);
        _connections[connectionId] = (instanceId, discordId);
        room[discordId] = new RoomMember(discordId, name);
        return Task.FromResult(isNewMember);
    }

    public Task<RemovedConnection?> RemoveAsync(string connectionId, CancellationToken ct = default)
    {
        if (!_connections.TryRemove(connectionId, out var info)) return Task.FromResult<RemovedConnection?>(null);

        var userLeft = !_connections.Values.Any(c => c.InstanceId == info.InstanceId && c.DiscordId == info.DiscordId);
        if (userLeft && _rooms.TryGetValue(info.InstanceId, out var room))
        {
            room.TryRemove(info.DiscordId, out _);
            if (room.IsEmpty) _rooms.TryRemove(info.InstanceId, out _);
        }

        return Task.FromResult<RemovedConnection?>(new RemovedConnection(info.InstanceId, info.DiscordId, userLeft));
    }

    public Task<IReadOnlyCollection<RoomMember>> GetRoomAsync(string instanceId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyCollection<RoomMember>>(
            _rooms.TryGetValue(instanceId, out var room) ? room.Values.ToArray() : []);

    public Task<bool> IsInRoomAsync(string instanceId, ulong discordId, CancellationToken ct = default)
        => Task.FromResult(_rooms.TryGetValue(instanceId, out var room) && room.ContainsKey(discordId));

    public async Task<bool> AreInSameRoomAsync(string instanceId, ulong a, ulong b, CancellationToken ct = default)
        => await IsInRoomAsync(instanceId, a, ct) && await IsInRoomAsync(instanceId, b, ct);
}
