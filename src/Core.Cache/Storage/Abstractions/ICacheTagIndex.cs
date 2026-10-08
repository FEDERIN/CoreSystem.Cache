namespace Core.Cache.Storage.Abstractions;

/// <summary>
/// Maintains the association between tags and the keys stored under them, so
/// entries can be invalidated by tag.
/// </summary>
/// <typeparam name="TProvider">The storage provider the index belongs to.</typeparam>
public interface ICacheTagIndex<TProvider>
{
    /// <summary>
    /// Associates a key with the supplied tags.
    /// </summary>
    /// <param name="key">The cache key that was stored.</param>
    /// <param name="tags">The tags to associate with the key.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the tags have been recorded.</returns>
    Task AddAsync(
        string key,
        IReadOnlyCollection<string> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes every tag association for a key, used when the entry is deleted.
    /// </summary>
    /// <param name="key">The cache key that was removed.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the associations have been removed.</returns>
    Task RemoveKeyAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes every entry carrying a tag, invoking the callback per key.
    /// </summary>
    /// <param name="tag">The tag being invalidated.</param>
    /// <param name="removeEntry">Callback that removes a single entry.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the tag has been invalidated.</returns>
    Task InvalidateTagAsync(
        string tag,
        Func<string, CancellationToken, Task> removeEntry,
        CancellationToken cancellationToken = default);
}