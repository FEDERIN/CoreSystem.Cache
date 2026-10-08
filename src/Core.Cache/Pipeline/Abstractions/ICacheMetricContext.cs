namespace Core.Cache.Pipeline.Abstractions;

/// <summary>
/// Implemented by cache contexts that produce hit or miss metrics.
/// </summary>
public interface ICacheMetricContext
{
    /// <summary>
    /// Gets the kind of metric this operation contributes.
    /// </summary>
    CacheMetricKind MetricKind { get; }
}

/// <summary>
/// Identifies how a cache operation affected the hit or miss counters.
/// </summary>
public enum CacheMetricKind
{
    /// <summary>
    /// The operation does not affect the counters.
    /// </summary>
    None,

    /// <summary>
    /// The operation found a cached entry.
    /// </summary>
    Hit,

    /// <summary>
    /// The operation did not find a cached entry.
    /// </summary>
    Miss
}