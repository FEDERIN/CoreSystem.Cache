# ⚡ CoreSystem.Cache

> **Production-ready caching framework for .NET 8, 9 and 10 with a single cache abstraction.**

CoreSystem.Cache provides one cache abstraction over an execution pipeline, Memory
storage, optional external storage with Memory fallback, HTTP response caching,
OpenTelemetry metrics and tag-based invalidation. It exists so applications can add
caching without wiring each concern by hand.

![NuGet](https://img.shields.io/nuget/v/CoreSystem.Cache?style=for-the-badge)
![Downloads](https://img.shields.io/nuget/dt/CoreSystem.Cache?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-blue?style=for-the-badge)

---

## ✨ Features

- ✅ Memory cache provider
- ✅ Cache-Aside (`GetOrAddAsync`)
- ✅ Tag-based invalidation
- ✅ Optional external cache with Memory fallback
- ✅ HTTP response caching
- ✅ OpenTelemetry metrics for cache hits and misses
- ✅ Configurable execution pipeline
- ✅ Resilience behavior when a provider registers it, such as `CoreSystem.Cache.Redis`
- ✅ Configurable serialization through `CoreSystem.Serialization`

Redis support is provided by the `CoreSystem.Cache.Redis` package, which
registers Redis as the external primary storage and uses Memory as the
fallback storage.

---

## 📦 Installation

```bash
dotnet add package CoreSystem.Cache
```

For Redis support, add the Redis provider package separately:

```bash
dotnet add package CoreSystem.Cache.Redis
```

| Package | Responsibility |
|----------|----------------|
| [**CoreSystem.Cache**](https://www.nuget.org/packages/CoreSystem.Cache) | Cache orchestration, storage resolution and fallback support |
| [**CoreSystem.Cache.Redis**](https://www.nuget.org/packages/CoreSystem.Cache.Redis) | Redis as the external primary storage, with Memory as the fallback |
| [**CoreSystem.Cache.Rehydration**](https://www.nuget.org/packages/CoreSystem.Cache.Rehydration) | Recovery of tracked fallback entries into the primary storage |

---

## 🚀 Quick Start

Register the framework:

```csharp
using Core.Cache.DependencyInjection;

builder.Services.AddCoreCache(options =>
{
    options.InstanceName = "my-app";
});
```

Inject the cache service:

```csharp
using Core.Cache.Abstractions;

public sealed class ProductService(ICoreCache cache)
{
}
```

Store data:

```csharp
await cache.SetAsync(
    "products:1",
    product,
    TimeSpan.FromMinutes(10));
```

Retrieve data:

```csharp
var product = await cache.GetAsync<Product>("products:1");
```

Recommended Cache-Aside pattern:

```csharp
var product = await cache.GetOrAddAsync(
    $"products:{id}",
    async ct => await repository.GetByIdAsync(id, ct),
    expiration: TimeSpan.FromMinutes(10),
    tags: ["products"]);
```

---

## 🌐 HTTP Response Caching

Enable the middleware:

```csharp
app.UseCoreCache();
```

Decorate your endpoint:

```csharp
using Core.Cache.Attributes;

[Cacheable(expirationSeconds: 300)]
public async Task<IActionResult> Get(Guid id)
{
    return Ok(await service.GetAsync(id));
}
```

HTTP response caching is applied only to endpoints decorated with
`CacheableAttribute`. The default request policy allows `GET` and `HEAD`
requests and excludes requests containing an `Authorization` header.

`CacheOptions.DefaultExpiration` sets the lifetime of a cached response when the
attribute omits `expirationSeconds`. It is not a fallback for `SetAsync` or
`GetOrAddAsync`: those operations store the entry with **no expiration** when the
`expiration` argument is `null`, so pass one explicitly when a lifetime is
needed.

---

## 📊 Why CoreSystem.Cache?

| Capability | `IDistributedCache` | CoreSystem.Cache |
|---|---:|---:|
| Cache-Aside | ❌ | ✅ |
| Tag Invalidation | ❌ | ✅ |
| Primary + Fallback Storage | ❌ | ✅ |
| HTTP Response Caching | ❌ | ✅ |
| OpenTelemetry Metrics | ❌ | ✅ |
| Configurable Cache Pipeline | ❌ | ✅ |

Redis and resilience capabilities are provided through their corresponding
companion packages and are integrated with the cache pipeline when
registered and configured.

---

## 🏗 Architecture

```text
Application
      │
      ▼
 ICoreCache
      │
      ▼
 CachePipeline
      │
      ├── Logging
      ├── Metrics
      ├── Resilience (when registered)
      └── Fallback (when available)
      │
      ▼
 Cache Storage Resolver
      │
      ├── External Storage (Primary)
      │
      └── Memory Storage (Fallback)
```

When no external cache storage is registered, Memory is used as the primary
storage. When one external storage is registered, it becomes the primary
storage and Memory becomes the fallback. The core resolver allows only one
external cache storage to be registered.

---

## 📚 Documentation

The full documentation includes:

- Getting Started
- Architecture
- Configuration
- Basic Usage
- HTTP Response Caching
- Observability
- Health Checks
- Extensibility
- Roadmap

Visit [federin.github.io/CoreSystem.Cache](https://federin.github.io/CoreSystem.Cache/)
for the complete reference.

---

## 🤝 Contributing

Issues, discussions and pull requests are welcome.

---

## 📄 License

Released under the MIT License.
