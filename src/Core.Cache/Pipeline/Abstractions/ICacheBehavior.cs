using Core.Cache.Pipeline.Contexts;
using Core.Cache.Pipeline.Delegates;
using System.Diagnostics.CodeAnalysis;

namespace Core.Cache.Pipeline.Abstractions;

/// <summary>
/// Defines a behavior that can be used to intercept and process cache operations.
/// </summary>
public interface ICacheBehavior
{
    /// <summary>
    /// Gets the order of the behavior in the pipeline.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Invokes the behavior in the pipeline.
    /// </summary>
    /// <param name="context">The context of the current cache operation.</param>
    /// <param name="next">The next delegate in the pipeline to be invoked.</param>
    [SuppressMessage(
        "Naming",
        "CA1716",
        Justification = "'next' is the established ASP.NET Core middleware and pipeline " +
            "convention. Renaming it would break every existing implementer of this " +
            "public interface for no functional gain.")]
    Task InvokeAsync(
        CacheContext context,
        CacheDelegate next);
}