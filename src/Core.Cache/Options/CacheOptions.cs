using Core.Serialization;

namespace Core.Cache.Options;

/// <summary>
/// Represents the configuration options for the cache.
/// </summary>
/// <remarks>
/// These options control cache behavior shared across the core cache
/// orchestration, including key naming, serialization, expiration,
/// entry size limits, and rehydration settings.
/// </remarks>
public class CacheOptions
{
    /// <summary>
    /// Master switch to enable or disable the cache logic.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional prefix applied to generated cache keys.
    /// </summary>
    /// <remarks>
    /// Using an instance name allows multiple applications or environments
    /// to safely share the same cache infrastructure without key collisions.
    /// </remarks>
    public string? InstanceName { get; set; }

    /// <summary>
    /// Gets or sets the expiration applied to HTTP response cache entries
    /// when the <c>CacheableAttribute</c> omits <c>expirationSeconds</c>.
    /// </summary>
    /// <remarks>
    /// This value is only used by the HTTP response caching path. It is not a
    /// fallback for <c>SetAsync</c> or <c>GetOrAddAsync</c>: when those
    /// operations are called without an expiration, the entry is stored with
    /// no expiration at all.
    /// </remarks>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets the maximum allowed size, in bytes, for a cache entry.
    /// </summary>
    /// <remarks>
    /// Reserved for a future entry-size limit. No cache storage or HTTP
    /// handler currently reads this value, so entries of any size are cached.
    /// Setting it has no effect today.
    /// </remarks>
    public long MaxCacheableSize { get; set; } = 1024 * 1024;

    /// <summary>
    /// Gets or sets the serializer used to store cache entries.
    /// </summary>
    /// <remarks>
    /// Supported serializers include JSON, MessagePack, and Protocol Buffers.
    /// </remarks>
    public SerializerType SerializerType { get; set; } = SerializerType.Json;

    /// <summary>
    /// Copies the configuration values from another <see cref="CacheOptions"/> instance.
    /// </summary>
    /// <param name="source"></param>
    public void CopyFrom(CacheOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Enabled = source.Enabled;
        InstanceName = source.InstanceName;
        DefaultExpiration = source.DefaultExpiration;
        MaxCacheableSize = source.MaxCacheableSize;
        SerializerType = source.SerializerType;
    }
}