namespace Core.Cache.Attributes;

/// <summary>
/// Indicates that the result of an HTTP endpoint should be cached.
/// </summary>
/// <remarks>
/// When applied to an endpoint, the middleware automatically stores
/// successful responses and serves subsequent requests directly from the cache
/// until the configured expiration period elapses.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public class CacheableAttribute(int expirationSeconds = 0, string tag = "") : Attribute
{
    /// <summary>
    /// Gets the tag associated with the cached response, or
    /// <see langword="null"/> when none was supplied.
    /// </summary>
    public string? Tag { get; } = !string.IsNullOrEmpty(tag) ? tag : null;

    /// <summary>
    /// Gets the lifetime of the cached response in seconds, or
    /// <see langword="null"/> when the value was not positive, in which case
    /// <see cref="Core.Cache.Options.CacheOptions.DefaultExpiration"/> applies.
    /// </summary>
    public int? ExpirationSeconds { get; } = expirationSeconds > 0 ? expirationSeconds : null;
}