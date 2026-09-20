using Core.Cache.Pipeline.Abstractions;
using Core.Cache.Pipeline.Contexts;
using Core.Cache.Pipeline.Delegates;
using Microsoft.Extensions.Logging;

namespace Core.Cache.Pipeline.Behaviors;

internal sealed class LoggingBehavior(
    ILogger<LoggingBehavior> logger)
    : ICacheBehavior
{
    public int Order =>
        (int)CacheBehaviorOrder.Logging;

    public async Task InvokeAsync(CacheContext context, CacheDelegate next)
    {
        var sanitizedKey = SanitizeForLog(context.Key);

        logger.LogDebug(
            "Executing cache operation for key {Key} on {Storage}",
            sanitizedKey,
            context.Storage?.GetType().Name);

        try
        {
            await next(context);

            logger.LogDebug(
                "Cache operation completed for key {Key}",
                sanitizedKey);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Cache operation failed for key {Key}",
                sanitizedKey);

            throw;
        }
    }

    private static string SanitizeForLog(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);
    }
}