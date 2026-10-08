using Core.Cache.Redis.Storage.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Core.Cache.Redis.Diagnostics;

internal sealed class RedisHealthCheck(
    IConnectionMultiplexer redis,
    IHealthState healthState,
    ILogger<RedisHealthCheck> logger)
    : IHealthCheck
{
    private const string RedisRecoveredMessage =
    "Redis connection restored.";

    private const string RedisUnavailableMessage =
        "Redis became unavailable. Switching to memory fallback.";

    private const string RedisUnavailableHealthMessage =
        "Redis is not responding. Memory fallback active.";

    private static readonly Action<ILogger, Exception?> LogRecovered =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1, nameof(LogRecovered)),
            RedisRecoveredMessage);

    private static readonly Action<ILogger, Exception?> LogUnavailable =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(2, nameof(LogUnavailable)),
            RedisUnavailableMessage);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            await redis.GetDatabase().PingAsync();

            if (healthState.Update(true) == HealthTransition.BecameHealthy)
            {
                LogRecovered(logger, null);
            }

            return HealthCheckResult.Healthy(
                "Redis is connected successfully.");
        }
        catch (Exception ex)
        {
            if (healthState.Update(false) == HealthTransition.BecameUnhealthy)
            {
                LogUnavailable(logger, ex);
            }

            return HealthCheckResult.Degraded(
                RedisUnavailableHealthMessage,
                ex);
        }
    }
}