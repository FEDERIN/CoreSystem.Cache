using Core.Cache.Pipeline.Contexts;
using Core.Cache.Pipeline.Delegates;

namespace Core.Cache.Pipeline.Abstractions;

/// <summary>
/// Runs a cache context through the configured behaviors.
/// </summary>
public interface ICachePipeline
{
    /// <summary>
    /// Executes the context through the pipeline.
    /// </summary>
    /// <param name="context">The operation to run.</param>
    /// <param name="terminal">The terminal step that performs the storage call.</param>
    /// <returns>A task that completes when the pipeline has run.</returns>
    Task ExecuteAsync(
        CacheContext context,
        CacheDelegate terminal);
}