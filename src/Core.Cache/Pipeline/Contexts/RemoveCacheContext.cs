namespace Core.Cache.Pipeline.Contexts;

/// <summary>
/// Context for the operation that removes a single entry.
/// </summary>
public sealed class RemoveCacheContext : CacheContext
{
    /// <inheritdoc />
    public override Task ExecuteAsync()
    {
        return Storage.RemoveAsync(
            Key,
            CancellationToken);
    }
}