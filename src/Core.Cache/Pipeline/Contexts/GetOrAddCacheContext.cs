using Core.Cache.Pipeline.Abstractions;

namespace Core.Cache.Pipeline.Contexts;

/// <summary>
/// Context for the Cache-Aside operation that reads a key and populates it
/// through a factory when it is missing.
/// </summary>
/// <typeparam name="T">The type of the cached value.</typeparam>
public sealed class GetOrAddCacheContext<T> : CacheContext, ICacheMetricContext
{
    /// <summary>
    /// Gets the factory invoked when the key is not already cached.
    /// </summary>
    public required Func<CancellationToken, Task<T>> Factory { get; init; }

    /// <summary>
    /// Gets the lifetime applied to the entry, or <see langword="null"/> to
    /// store it with no expiration.
    /// </summary>
    public TimeSpan? Expiration { get; init; }

    /// <summary>
    /// Gets the tags associated with the entry, if any.
    /// </summary>
    public string[]? Tags { get; init; }

    /// <summary>
    /// Gets or sets the resolved value, set once the operation has run.
    /// </summary>
    public T? Result { get; set; }

    /// <summary>
    /// Gets whether the operation was a hit or a miss, derived from whether a
    /// value was resolved.
    /// </summary>
    public CacheMetricKind MetricKind =>
        Result is null
            ? CacheMetricKind.Miss
            : CacheMetricKind.Hit;

    /// <inheritdoc />
    public override async Task ExecuteAsync()
    {
        Result = await Storage.GetOrAddAsync(
            Key,
            Factory,
            EntryOptions,
            Expiration,
            Tags,
            CancellationToken);
    }
}