# ⚡ CoreSystem.Cache.Redis

> **Redis distributed cache provider for CoreSystem.Cache on .NET 8**

`CoreSystem.Cache.Redis` registers Redis as the external primary cache storage
for `CoreSystem.Cache`. Memory caching becomes the automatic fallback, so cache
reads keep working while Redis is unreachable.

The package does not implement a cache API. Application services continue to use
`ICoreCache` exactly as they do with the core package.

![NuGet](https://img.shields.io/nuget/v/CoreSystem.Cache.Redis?style=for-the-badge)
![Downloads](https://img.shields.io/nuget/dt/CoreSystem.Cache.Redis?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8.0-blue?style=for-the-badge)

------------------------------------------------------------------------

## ✨ Features

-   ✅ Redis as the external primary cache storage
-   ✅ Automatic Memory fallback when Redis is unavailable
-   ✅ Tag-based invalidation through a Redis tag index
-   ✅ Distributed locking for concurrent cache population
-   ✅ Health check reported as `redis_cache`, tagged `cache` and `primary`
-   ✅ Optional resilience integration through `CoreSystem.Resilience`
-   ✅ Configurable serialization through `Core.Serialization`

------------------------------------------------------------------------

## 📦 Installation

Install the core package first, then the Redis provider:

```bash
dotnet add package CoreSystem.Cache
dotnet add package CoreSystem.Cache.Redis
```

------------------------------------------------------------------------

## 🚀 Quick Start

Register the core cache first. `AddCoreCacheRedis()` reads the `CacheOptions`
instance registered here, so the order matters:

```csharp
builder.Services.AddCoreCache(options =>
{
    options.InstanceName = "my-app";
});
```

Then register Redis:

```csharp
builder.Services.AddCoreCacheRedis(options =>
{
    options.Configuration = redis =>
    {
        redis.EndPoints.Add("localhost", 6379);
    };
});
```

The `Configuration` delegate receives the StackExchange.Redis
`ConfigurationOptions`, so any connection setting is available.

Add health checks so the `redis_cache` check is registered:

```csharp
builder.Services.AddHealthChecks();
```

Application services keep using `ICoreCache`:

```csharp
public sealed class ProductService(ICoreCache cache)
{
    public Task<Product?> GetAsync(
        string key,
        CancellationToken ct = default)
        => cache.GetAsync<Product>(key, ct);
}
```

Cache-Aside reads work unchanged:

```csharp
var product = await cache.GetOrAddAsync(
    $"products:{id}",
    async ct => await repository.GetByIdAsync(id, ct),
    expiration: TimeSpan.FromMinutes(10),
    tags: ["products"]);
```

------------------------------------------------------------------------

## ⚠️ Registration Requirements

`AddCoreCacheRedis()` throws `InvalidOperationException` when:

-   `AddCoreCache()` has not been called, because no `CacheOptions` instance is
    registered (`CacheRedisRegistration.cs:139`);
-   the `Configuration` delegate did not assign `Configuration`
    (`CacheRedisRegistration.cs:124`).

When the core cache is registered but disabled (`CacheOptions.Enabled` is
`false`), the Redis services are not registered and no exception is thrown.

------------------------------------------------------------------------

## 🔑 Key Prefixing

`CacheOptions.InstanceName` is used as a Redis key prefix. The provider appends
its own separator, so do not add a trailing colon:

```csharp
options.InstanceName = "my-app";
```

produces keys such as:

```text
my-app:products:1
```

If no instance name is configured, no prefix is added. The Memory storage does
not apply this prefix.

------------------------------------------------------------------------

## ❤️ Health Checks

The provider registers a health check named `redis_cache` with the tags `cache`
and `primary`. The `primary` tag is what
`CoreSystem.Cache.Rehydration` observes to detect a recovery.

The check reports:

| Result | Description |
|---|---|
| Healthy | `Redis is connected successfully.` |
| Degraded | `Redis is not responding. Memory fallback active.` |

The check is contributed through an `IHealthCheckContributor`, so
`AddHealthChecks()` must be called for it to be registered.

------------------------------------------------------------------------

## 🔁 Fallback and Locking

Registering the provider enables the fallback behavior automatically, so the
storage resolver resolves Redis as primary and Memory as fallback.

`GetOrAddAsync` acquires a distributed lock per key before populating the entry,
then re-reads the value inside the lock. This prevents several instances from
populating the same key concurrently.

------------------------------------------------------------------------

## 🛡 Resilience

`CoreSystem.Resilience` defines a dedicated `PipelineType.Redis`. When a Redis
resilience pipeline is configured, the provider adds the following exceptions to
the Retry and Circuit Breaker handling:

-   `RedisConnectionException`
-   `RedisTimeoutException`
-   `TimeoutException`

The strategies and their options belong to `CoreSystem.Resilience`, not to
`RedisOptions`, which exposes only `Configuration`.

------------------------------------------------------------------------

## 🏗 Architecture

```text
ICoreCache
      │
      ▼
 CachePipeline
      │
      ├── Logging
      ├── Metrics
      ├── Fallback (enabled by the Redis provider)
      └── Resilience (when a Redis pipeline is configured)
      │
      ▼
 Cache Storage Resolver
      │
      ├── RedisCacheStorage (Primary)
      │
      └── Memory Storage (Fallback)
```

------------------------------------------------------------------------

## 📚 Documentation

The full documentation covers:

-   Getting Started
-   Architecture
-   Configuration
-   Basic Usage
-   Health Checks
-   Extensibility
-   Roadmap

Visit [federin.github.io/CoreSystem.Cache/Redis](https://federin.github.io/CoreSystem.Cache/Redis)
for the complete reference.

------------------------------------------------------------------------

## 🤝 Contributing

Issues, discussions and pull requests are welcome.

------------------------------------------------------------------------

## 📄 License

Released under the MIT License.