using Core.Cache.Pipeline.Abstractions;

namespace Core.Cache.Pipeline.Contexts;

/// <summary>
/// Context for the operation that reads a single value.
/// </summary>
/// <typeparam name="T">The type of the cached value.</typeparam>
public sealed class GetCacheContext<T> : CacheContext, ICacheMetricContext
{
    /// <summary>
    /// Gets or sets the resolved value, set once the operation has run.
    /// </summary>
    public T? Result { get; set; }

    /// <summary>
    /// Gets whether the read was a hit or a miss, derived from whether a value
    /// was resolved.
    /// </summary>
    public CacheMetricKind MetricKind =>
        Result is null
            ? CacheMetricKind.Miss
            : CacheMetricKind.Hit;

    /// <inheritdoc />
    public override async Task ExecuteAsync()
    {
        Result = await Storage.GetAsync<T>(
            Key,
            CancellationToken);
    }
}