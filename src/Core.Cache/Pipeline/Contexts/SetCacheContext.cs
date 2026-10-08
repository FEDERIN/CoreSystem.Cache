namespace Core.Cache.Pipeline.Contexts;

/// <summary>
/// Context for the operation that stores a value under a key.
/// </summary>
/// <typeparam name="T">The type of the value being stored.</typeparam>
public sealed class SetCacheContext<T> : CacheContext
{
    /// <summary>
    /// Gets the value to store.
    /// </summary>
    public required T Value { get; init; }

    /// <summary>
    /// Gets the lifetime applied to the entry, or <see langword="null"/> to
    /// store it with no expiration.
    /// </summary>
    public TimeSpan? Expiration { get; init; }

    /// <summary>
    /// Gets the tags associated with the entry, if any.
    /// </summary>
    public string[]? Tags { get; init; }

    /// <inheritdoc />
    public override Task ExecuteAsync()
    {
        return Storage.SetAsync(
            Key,
            Value,
            EntryOptions,
            Expiration,
            Tags,
            CancellationToken);
    }
}