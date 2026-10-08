using Core.Cache.Abstractions;
using Core.Cache.Pipeline.Abstractions;
using Core.Cache.Pipeline.Contexts;
using Core.Cache.Pipeline.Delegates;
using Core.Cache.Storage;
using Microsoft.Extensions.Logging;

namespace Core.Cache.Pipeline.Behaviors;

internal sealed class FallbackBehavior(
    ICacheStorageResolver resolver,
    IPrimaryHealthStateWriter primaryHealthState,
    ILogger<FallbackBehavior> logger) : ICacheBehavior
{
    private readonly ICacheStorageResolver _resolver = resolver;
    private readonly ILogger<FallbackBehavior> _logger = logger;

    private static readonly Action<ILogger, Exception?> LogPrimaryFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1, nameof(LogPrimaryFailed)),
            "Primary storage failed. Switching to fallback.");

    public int Order =>

    (int)CacheBehaviorOrder.Fallback;
    public async Task InvokeAsync(
        CacheContext context,
        CacheDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var fallback = _resolver.Fallback;

            if (fallback is null)
            {
                throw;
            }

            primaryHealthState.MarkUnavailable();

            LogPrimaryFailed(_logger, ex);

            context.Exception = ex;
            context.Storage = fallback;
            context.EntryOptions = CacheEntryOptions.Rehydrate;

            await context.ExecuteAsync();
        }
    }
}