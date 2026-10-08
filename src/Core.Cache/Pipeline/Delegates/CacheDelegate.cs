using Core.Cache.Pipeline.Contexts;
using System.Diagnostics.CodeAnalysis;

namespace Core.Cache.Pipeline.Delegates;

/// <summary>
/// Defines a delegate that represents the next step in the cache pipeline.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1711",
    Justification = "The Delegate suffix is the established convention for pipeline " +
        "step delegates and names the type accurately. Renaming it would break the " +
        "public ICacheBehavior contract for no functional gain.")]
public delegate Task CacheDelegate(CacheContext context);