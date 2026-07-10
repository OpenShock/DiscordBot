namespace OpenShock.Activity.Api.Realtime;

/// <summary>
/// Keeps this replica's presence entries alive by periodically refreshing the TTL on the hash fields
/// for the connections it owns. Dead connections need no cleanup here — a graceful disconnect deletes
/// its field via <see cref="RoomHub.OnDisconnectedAsync"/>, and a crashed replica's fields expire on
/// their own once their TTL lapses. Registered only when Redis is configured.
/// </summary>
public sealed class RoomPresenceMaintenanceService : BackgroundService
{
    // Must stay well under RedisRoomRegistry.ConnTtl so a live connection is never briefly evicted.
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private readonly RedisRoomRegistry _registry;
    private readonly ILogger<RoomPresenceMaintenanceService> _logger;

    public RoomPresenceMaintenanceService(RedisRoomRegistry registry, ILogger<RoomPresenceMaintenanceService> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await _registry.RefreshLocalTtlsAsync();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Room presence TTL refresh failed");
            }
        }
    }
}
