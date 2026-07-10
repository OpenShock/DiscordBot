using System.Collections.Concurrent;
using StackExchange.Redis;

namespace OpenShock.Activity.Api.Realtime;

/// <summary>
/// Redis-backed presence store, safe to run across multiple API replicas (paired with the SignalR
/// Redis backplane so group broadcasts fan out). Keys are namespaced under <c>activity:</c> and live
/// in the connection string's default database so they don't collide with other consumers of the
/// shared Redis (Dragonfly).
///
/// Data model — one hash per Discord Activity instance:
/// <code>
///   activity:room:{instanceId}:conns   HASH   connectionId -> "{discordId}:{name}"
/// </code>
/// Each field carries a TTL (Dragonfly/Redis 7.4 hash-field expiry). While the owning replica is
/// alive it refreshes its fields' TTLs; if a replica crashes its fields simply expire, so the members
/// drop out on their own — no separate liveness keys and no reaper needed. A user is "present" iff at
/// least one of their (unexpired) connections is in the hash.
/// </summary>
public sealed class RedisRoomRegistry : IRoomRegistry
{
    private const string Prefix = "activity";

    /// <summary>Per-connection field TTL. Comfortably larger than the refresh interval so a live
    /// connection is never evicted between refreshes.
    /// Keep this a whole number of seconds: StackExchange.Redis only emits the seconds-precision
    /// HEXPIRE for whole-second spans; a fractional value flips it to HPEXPIRE, which Dragonfly does
    /// not implement (it supports HEXPIRE/HTTL but not the millisecond variants).</summary>
    public static readonly TimeSpan ConnTtl = TimeSpan.FromSeconds(90);

    private readonly IDatabase _db;

    // Connections owned by THIS replica: connectionId -> (instanceId, discordId, name). Drives TTL
    // refresh (which re-asserts the field, so name is needed) and lets graceful disconnects locate
    // their room without an extra round-trip.
    private readonly ConcurrentDictionary<string, (string InstanceId, ulong DiscordId, string Name)> _local = new();

    public RedisRoomRegistry(IConnectionMultiplexer mux) => _db = mux.GetDatabase();

    private static string RoomConnsKey(string instanceId) => $"{Prefix}:room:{instanceId}:conns";
    private static string Encode(ulong discordId, string name) => $"{discordId}:{name}";

    private static bool TryDecode(string? value, out ulong discordId, out string name)
    {
        discordId = 0;
        name = string.Empty;
        if (string.IsNullOrEmpty(value)) return false;
        var sep = value.IndexOf(':');
        if (sep <= 0) return false;
        if (!ulong.TryParse(value.AsSpan(0, sep), out discordId)) return false;
        name = value[(sep + 1)..];
        return true;
    }

    public async Task<bool> AddAsync(string connectionId, string instanceId, ulong discordId, string name, CancellationToken ct = default)
    {
        var connsKey = RoomConnsKey(instanceId);
        await _db.HashSetAsync(connsKey, connectionId, Encode(discordId, name));
        await _db.HashFieldExpireAsync(connsKey, [connectionId], ConnTtl);
        _local[connectionId] = (instanceId, discordId, name);

        // First connection for this user in the room? (Count this user's entries after the add.)
        var values = await _db.HashValuesAsync(connsKey);
        var count = values.Count(v => TryDecode(v, out var id, out _) && id == discordId);
        return count <= 1;
    }

    public async Task<RemovedConnection?> RemoveAsync(string connectionId, CancellationToken ct = default)
    {
        // Only local connections are removed explicitly; a crashed replica's fields expire on their own.
        if (!_local.TryRemove(connectionId, out var info)) return null;

        var connsKey = RoomConnsKey(info.InstanceId);
        await _db.HashDeleteAsync(connsKey, connectionId);

        var remaining = await _db.HashValuesAsync(connsKey);
        var userLeft = !remaining.Any(v => TryDecode(v, out var id, out _) && id == info.DiscordId);
        return new RemovedConnection(info.InstanceId, info.DiscordId, userLeft);
    }

    public async Task<IReadOnlyCollection<RoomMember>> GetRoomAsync(string instanceId, CancellationToken ct = default)
    {
        var values = await _db.HashValuesAsync(RoomConnsKey(instanceId));
        var members = new Dictionary<ulong, RoomMember>();
        foreach (var v in values)
            if (TryDecode(v, out var id, out var name))
                members[id] = new RoomMember(id, name);
        return members.Values;
    }

    private async Task<HashSet<ulong>> PresentIdsAsync(string instanceId)
    {
        var values = await _db.HashValuesAsync(RoomConnsKey(instanceId));
        var ids = new HashSet<ulong>();
        foreach (var v in values)
            if (TryDecode(v, out var id, out _))
                ids.Add(id);
        return ids;
    }

    public async Task<bool> IsInRoomAsync(string instanceId, ulong discordId, CancellationToken ct = default)
        => (await PresentIdsAsync(instanceId)).Contains(discordId);

    public async Task<bool> AreInSameRoomAsync(string instanceId, ulong a, ulong b, CancellationToken ct = default)
    {
        var ids = await PresentIdsAsync(instanceId);
        return ids.Contains(a) && ids.Contains(b);
    }

    /// <summary>Re-asserts every connection this replica owns and refreshes its field TTL. Re-writing
    /// the field (HSET) rather than only bumping the TTL is deliberate: if a field expired while its
    /// client is still connected — a Redis blip or a stalled refresh outrunning the TTL — HEXPIRE alone
    /// cannot resurrect a missing field, so the still-connected user would stay absent (and fail the
    /// same-room check) until they reconnected. HSET recreates it, bounding any such gap to one cycle.
    /// Called on a timer by <see cref="RoomPresenceMaintenanceService"/>.</summary>
    internal async Task RefreshLocalTtlsAsync()
    {
        if (_local.IsEmpty) return;
        foreach (var group in _local.GroupBy(kv => kv.Value.InstanceId))
        {
            var connsKey = RoomConnsKey(group.Key);
            var entries = group.Select(kv => new HashEntry(kv.Key, Encode(kv.Value.DiscordId, kv.Value.Name))).ToArray();
            await _db.HashSetAsync(connsKey, entries);
            var fields = group.Select(kv => (RedisValue)kv.Key).ToArray();
            await _db.HashFieldExpireAsync(connsKey, fields, ConnTtl);
        }
    }
}
