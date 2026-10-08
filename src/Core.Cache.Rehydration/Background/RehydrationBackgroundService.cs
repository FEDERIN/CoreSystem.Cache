using Core.Cache.Rehydration.Abstractions;
using Core.Cache.Rehydration.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Core.Cache.Rehydration.Background;

internal sealed class RehydrationBackgroundService(
    IRehydrationService rehydrationService,
    ILogger<RehydrationBackgroundService> logger,
    RehydrationOptions options)
    : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogStarted =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1, nameof(LogStarted)),
            "Cache rehydration background service started.");

    private static readonly Action<ILogger, Exception?> LogCycleFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(2, nameof(LogCycleFailed)),
            "An error occurred during cache rehydration.");

    private static readonly Action<ILogger, Exception?> LogStopped =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(3, nameof(LogStopped)),
            "Cache rehydration background service stopped.");

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        LogStarted(logger, null);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await rehydrationService.ExecuteCycleAsync(
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogCycleFailed(logger, ex);
            }

            await Task.Delay(
                options.Interval,
                stoppingToken);
        }

        LogStopped(logger, null);
    }
}