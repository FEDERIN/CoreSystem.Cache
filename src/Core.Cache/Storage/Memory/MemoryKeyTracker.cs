using Core.Cache.Storage.Abstractions;
using System.Collections.Concurrent;

namespace Core.Cache.Storage.Memory;

/// <summary>
/// Keeps track of the keys held by the Memory storage.
/// </summary>
/// <remarks>
/// The implementation is public but the contract it satisfies,
/// <c>ICacheKeyTracker</c>, is internal, so it cannot currently be substituted
/// from outside the assembly.
/// </remarks>
public class MemoryKeyTracker : ICacheKeyTracker
{
    private readonly ConcurrentDictionary<string, byte> _trackedKeys = new();

    /// <inheritdoc />
    public void Track(string key) => _trackedKeys.TryAdd(key, 1);

    /// <inheritdoc />
    public void Untrack(string key) => _trackedKeys.TryRemove(key, out _);

    /// <inheritdoc />
    public IEnumerable<string> GetAllTrackedKeys() => _trackedKeys.Keys;
}