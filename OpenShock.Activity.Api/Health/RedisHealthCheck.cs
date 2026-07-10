using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace OpenShock.Activity.Api.Health;

/// <summary>
/// Readiness check for the Redis backplane/presence store: verifies the shared connection multiplexer is
/// connected and responds to a PING. Only registered when Redis is configured (see Program.cs) — without
/// Redis the API runs single-instance and this check is absent rather than failing.
/// </summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _multiplexer;

    public RedisHealthCheck(IConnectionMultiplexer multiplexer) => _multiplexer = multiplexer;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_multiplexer.IsConnected)
                return HealthCheckResult.Unhealthy("Redis connection is not established.");

            var latency = await _multiplexer.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Redis PING {latency.TotalMilliseconds:F1}ms");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis PING failed.", ex);
        }
    }
}