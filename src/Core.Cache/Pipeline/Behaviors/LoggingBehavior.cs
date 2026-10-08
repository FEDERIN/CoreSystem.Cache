using Core.Cache.Pipeline.Abstractions;
using Core.Cache.Pipeline.Contexts;
using Core.Cache.Pipeline.Delegates;
using Microsoft.Extensions.Logging;

namespace Core.Cache.Pipeline.Behaviors;

internal sealed class LoggingBehavior(
    ILogger<LoggingBehavior> logger)
    : ICacheBehavior
{
    private static readonly Action<ILogger, string, string?, Exception?> LogExecuting =
        LoggerMessage.Define<string, string?>(
            LogLevel.Debug,
            new EventId(1, nameof(LogExecuting)),
            "Executing cache operation for key {Key} on {Storage}");

    private static readonly Action<ILogger, string, Exception?> LogCompleted =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(2, nameof(LogCompleted)),
            "Cache operation completed for key {Key}");

    private static readonly Action<ILogger, string, Exception?> LogFailed =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3, nameof(LogFailed)),
            "Cache operation failed for key {Key}");

    public int Order =>
        (int)CacheBehaviorOrder.Logging;

    public async Task InvokeAsync(CacheContext context, CacheDelegate next)
    {
        var sanitizedKey = SanitizeForLog(context.Key);

        LogExecuting(
            logger,
            sanitizedKey,
            context.Storage?.GetType().Name,
            null);

        try
        {
            await next(context);

            LogCompleted(logger, sanitizedKey, null);
        }
        catch (Exception ex)
        {
            LogFailed(logger, sanitizedKey, ex);

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