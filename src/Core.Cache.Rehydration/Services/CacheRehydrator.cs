using Core.Cache.Rehydration.Abstractions;
using Microsoft.Extensions.Logging;

namespace Core.Cache.Rehydration.Services;

internal sealed class CacheRehydrator(
    IRehydrationSource source,
    IRehydrationTarget target,
    ILogger<CacheRehydrator> logger)
    : ICacheRehydrator
{
    private const int BatchSize = 100;

    private static readonly Action<ILogger, string, Exception?> LogEntryFailed =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1, nameof(LogEntryFailed)),
            "Unable to rehydrate cache key '{Key}'. It will be retried later.");

    public async Task RehydrateAsync(
        CancellationToken cancellationToken)
    {
        var entries = source
            .GetEntries()
            .ToList();

        foreach (var batch in entries.Chunk(BatchSize))
        {
            foreach (var entry in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await target.StoreAsync(
                        entry,
                        cancellationToken);

                    await source.RemoveForRehydrationAsync(
                        entry.Key,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    LogEntryFailed(logger, entry.Key, ex);
                }
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(100),
                cancellationToken);
        }
    }
}