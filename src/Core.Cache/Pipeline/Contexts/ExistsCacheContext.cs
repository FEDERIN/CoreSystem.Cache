namespace Core.Cache.Pipeline.Contexts;

/// <summary>
/// Context for the operation that checks whether a key is present.
/// </summary>
public sealed class ExistsCacheContext : CacheContext
{
    /// <summary>
    /// Gets or sets the result, set once the operation has run.
    /// </summary>
    public bool Exists { get; set; }

    /// <inheritdoc />
    public override async Task ExecuteAsync()
    {
        Exists = await Storage.ExistsAsync(
            Key,
            CancellationToken);
    }
}