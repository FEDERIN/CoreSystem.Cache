namespace Core.Cache.Storage;

/// <summary>
/// Options applied to a cache entry when it is stored.
/// </summary>
public sealed class CacheEntryOptions
{
    /// <summary>
    /// Gets the default options, which do not track the entry for rehydration.
    /// </summary>
    public static readonly CacheEntryOptions Default = new();

    /// <summary>
    /// Gets options that mark the entry as recoverable, used by the fallback
    /// pipeline so the memory copy can later be restored to the primary storage.
    /// </summary>
    public static readonly CacheEntryOptions Rehydrate =
        new()
        {
            TrackForRehydration = true
        };

    /// <summary>
    /// Gets a value indicating whether the entry is tracked so it can be
    /// restored to the primary storage after a recovery.
    /// </summary>
    public bool TrackForRehydration { get; init; }
}