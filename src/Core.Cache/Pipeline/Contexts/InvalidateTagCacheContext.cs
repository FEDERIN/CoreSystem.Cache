namespace Core.Cache.Pipeline.Contexts;

/// <summary>
/// Context for the operation that removes every entry carrying a tag.
/// </summary>
public sealed class InvalidateTagCacheContext : CacheContext
{
    /// <summary>
    /// Gets the tag whose entries are invalidated.
    /// </summary>
    public required string Tag { get; init; }

    /// <inheritdoc />
    public override Task ExecuteAsync()
    {
        return Storage.InvalidateByTagAsync(
            Tag,
            CancellationToken);
    }
}