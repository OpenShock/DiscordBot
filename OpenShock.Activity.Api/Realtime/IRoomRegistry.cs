namespace OpenShock.Activity.Api.Realtime;

/// <summary>A user present in a Discord Activity instance (presence only — consent lives in the DB).</summary>
public sealed record RoomMember(ulong DiscordId, string Name);

/// <summary>Result of removing a connection: which room/user it belonged to, and whether that was the
/// user's last connection in the room (i.e. they truly left).</summary>
public sealed record RemovedConnection(string InstanceId, ulong DiscordId, bool UserLeft);

/// <summary>
/// Tracks who is connected to which Discord Activity instance. Source of truth for the "same room"
/// check in the control endpoint and the roster shown to clients.
///
/// Two implementations: <see cref="InMemoryRoomRegistry"/> (single replica) and
/// <see cref="RedisRoomRegistry"/> (shared store, required when running more than one API replica
/// alongside the SignalR Redis backplane).
/// </summary>
public interface IRoomRegistry
{
    /// <summary>Records a connection. Returns true if this is the user's first connection in the
    /// instance (so callers know whether to broadcast a join).</summary>
    Task<bool> AddAsync(string connectionId, string instanceId, ulong discordId, string name, CancellationToken ct = default);

    /// <summary>Removes a connection. Returns its room/user and whether the user has no connections
    /// left in the instance; null if the connection was unknown.</summary>
    Task<RemovedConnection?> RemoveAsync(string connectionId, CancellationToken ct = default);

    /// <summary>All users currently present in the instance.</summary>
    Task<IReadOnlyCollection<RoomMember>> GetRoomAsync(string instanceId, CancellationToken ct = default);

    Task<bool> IsInRoomAsync(string instanceId, ulong discordId, CancellationToken ct = default);

    Task<bool> AreInSameRoomAsync(string instanceId, ulong a, ulong b, CancellationToken ct = default);
}
