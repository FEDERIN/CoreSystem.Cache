using Core.Cache.Abstractions;
using Core.Cache.Storage;

namespace Core.Cache.Pipeline.Contexts;

/// <summary>
/// Represents a single cache operation travelling through the pipeline.
/// </summary>
/// <remarks>
/// One context instance is created per operation and mutated by the behaviors as
/// it moves down the pipeline and, on fallback, back up it.
/// </remarks>
public abstract class CacheContext
{
    /// <summary>
    /// Gets the logical cache key for this operation.
    /// </summary>
    public required string Key { get; init; }

    internal ICacheStorage Storage { get; set; } = default!;

    /// <summary>
    /// Gets the token used to cancel this operation.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// Gets or sets the exception that caused the operation to fall back, or
    /// <see langword="null"/> when it completed without one.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets the options applied to the entry once it is stored.
    /// </summary>
    public CacheEntryOptions EntryOptions { get; set; } = CacheEntryOptions.Default;

    /// <summary>
    /// Executes the operation against the currently selected storage.
    /// </summary>
    /// <returns>A task that completes when the operation has run.</returns>
    public abstract Task ExecuteAsync();
}