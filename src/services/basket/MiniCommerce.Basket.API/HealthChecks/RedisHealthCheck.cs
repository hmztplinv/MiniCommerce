using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace MiniCommerce.Basket.API.HealthChecks;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_connectionMultiplexer.IsConnected)
            {
                return HealthCheckResult.Unhealthy("Redis connection is not active.");
            }

            var database = _connectionMultiplexer.GetDatabase();
            var ping = await database.PingAsync();

            return HealthCheckResult.Healthy($"Redis ping successful. Response time: {ping.TotalMilliseconds} ms.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis health check failed.", exception);
        }
    }
}
