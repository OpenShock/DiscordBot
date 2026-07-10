using StackExchange.Redis;

namespace OpenShock.Activity.Api.Config;

/// <summary>
/// Optional Redis configuration. When a connection string is present it enables horizontal scaling:
/// a SignalR backplane (so group broadcasts fan out across replicas) plus a shared room-presence
/// store. When absent the API falls back to single-instance in-memory presence.
///
/// Bound from the top-level "Redis" section (env: <c>REDIS__CONN</c>), mirroring the top-level "Db"
/// section this repo uses. The Redis is shared with the wider OpenShock stack, so add
/// <c>,defaultDatabase=N</c> to the connection string to keep Activity presence keys isolated.
/// </summary>
public sealed class RedisConfig
{
    /// <summary>StackExchange.Redis connection string, e.g. <c>host:6379,password=…,defaultDatabase=3</c>.</summary>
    public string? Conn { get; init; }

    /// <summary>True when a connection string is set (i.e. distributed mode is enabled).</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Conn);

    /// <summary>Builds the StackExchange.Redis options used by both the backplane and the presence store.</summary>
    public ConfigurationOptions ToConfigurationOptions()
    {
        if (string.IsNullOrWhiteSpace(Conn))
            throw new InvalidOperationException("Redis:Conn is required to build Redis options.");

        var options = ConfigurationOptions.Parse(Conn);
        options.AbortOnConnectFail = false;
        return options;
    }
}
