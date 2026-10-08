using System.Diagnostics.Metrics;

namespace Core.Cache.Diagnostics;

/// <summary>
/// Publishes the OpenTelemetry counters describing cache read outcomes.
/// </summary>
/// <remarks>
/// Creating this type registers the <c>cache.distributed.hits</c> and
/// <c>cache.distributed.misses</c> counters. Exporting them requires an
/// exporter, supplied by <c>CoreSystem.Observability</c>.
/// </remarks>
public class CacheMetrics
{
    private readonly Counter<long>? _cacheHitCounter;
    private readonly Counter<long>? _cacheMissCounter;

    /// <summary>
    /// Initializes a new instance and creates the underlying counters.
    /// </summary>
    /// <param name="meterFactory">Factory used to create the cache meter.</param>
    public CacheMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(CacheDiagnosticsConstants.MeterName!, "1.0.0");

        _cacheHitCounter = meter.CreateCounter<long>(
            name: "cache.distributed.hits",
            unit: "{hits}",
            description: "Total number of successful cache reads.");

        _cacheMissCounter = meter.CreateCounter<long>(
            name: "cache.distributed.misses",
            unit: "{misses}",
            description: "Total number of cache read requests that did not find the requested item.");
    }

    /// <summary>
    /// Records one successful cache read.
    /// </summary>
    public void RecordHit() => _cacheHitCounter?.Add(1);

    /// <summary>
    /// Records one cache read that did not find the requested entry.
    /// </summary>
    public void RecordMiss() => _cacheMissCounter?.Add(1);
}