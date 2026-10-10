# ⚡ CoreSystem.Cache.Rehydration

> **Cache recovery component for CoreSystem.Cache on .NET 8**

CoreSystem.Cache.Rehydration restores entries that were kept in the memory
fallback after the primary cache provider becomes healthy again. It does not
implement a cache provider: it reads tracked entries from the memory fallback
and writes them to the current primary storage.

![NuGet](https://img.shields.io/nuget/v/CoreSystem.Cache.Rehydration?style=for-the-badge)
![Downloads](https://img.shields.io/nuget/dt/CoreSystem.Cache.Rehydration?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8.0-blue?style=for-the-badge)

------------------------------------------------------------------------

## ✨ Features

-   ✅ Restores fallback entries into the primary storage after a recovery
-   ✅ Recovery driven by `primary`-tagged health checks, not by a timer alone
-   ✅ Preserves remaining expiration and tags when writing back
-   ✅ Per-entry fault isolation: one failure never aborts the cycle
-   ✅ Background service with a configurable interval
-   ✅ Disabled by default when the core cache is disabled

------------------------------------------------------------------------

## 📦 Installation

```bash
dotnet add package CoreSystem.Cache.Rehydration
```

This package requires an external primary storage. Install
`CoreSystem.Cache.Redis`, which registers Redis as that primary:

```bash
dotnet add package CoreSystem.Cache.Redis
```

| Package | Responsibility |
|----------|----------------|
| [**CoreSystem.Cache**](https://www.nuget.org/packages/CoreSystem.Cache) | Cache orchestration, storage resolution and fallback support |
| [**CoreSystem.Cache.Rehydration**](https://www.nuget.org/packages/CoreSystem.Cache.Rehydration) | Recovery of tracked fallback entries into the primary storage |

------------------------------------------------------------------------

## 🚀 Quick Start

Register the core cache and a primary provider first.

```csharp
services.AddCoreCache(options =>
{
    options.InstanceName = "my-app";
});

services.AddCoreCacheRedis(options =>
{
    options.Configuration = redis =>
    {
        redis.EndPoints.Add("localhost", 6379);
    };
});

services.AddHealthChecks();
```

Then register rehydration.

```csharp
services.AddCoreCacheRehydration(options =>
{
    options.Enabled = true;
    options.Interval = TimeSpan.FromSeconds(30);
});
```

`AddHealthChecks()` is **required**. `RehydrationService` takes a
`HealthCheckService` in its constructor, and the background service is resolved
when the host starts. Without health checks registered, the host fails to start
with an `InvalidOperationException`.

Rehydration registration requires an enabled `CoreSystem.Cache` registration and
a registered external primary storage. `IExternalCacheStorage` is the internal
contract the provider registers that primary through; you do not implement it
yourself. If the core cache is disabled, the rehydration services are not
registered. If the primary storage is missing, registration throws an
`InvalidOperationException`.

------------------------------------------------------------------------

## 🔄 How Recovery Works

Recovery applies to entries marked with `CacheEntryOptions.Rehydrate`. The
memory fallback tracks those keys.

```text
Memory fallback
      │
      ▼
Tracked cache keys
      │
      ▼
MemoryRehydrationSource
      │
      ▼
CacheRehydrator
      │
      ▼
PrimaryRehydrationTarget
      │
      ▼
ICacheStorageResolver.Primary
```

Each entry carries its key and value, and may also carry its remaining
expiration and tags. Both are forwarded to the primary when available.

When an entry is stored successfully it is removed from the memory fallback. If
storing it fails, the failure is logged, the entry stays in the fallback for a
later cycle, and processing continues with the next entry. Expired or
unavailable memory entries are skipped.

------------------------------------------------------------------------

## ❤️ Primary Recovery Detection

Rehydration does not run merely because the primary is currently healthy. The
service inspects health checks tagged `primary`, and fires only after observing
the primary as unhealthy and then as healthy. If no `primary`-tagged check
exists, rehydration never triggers.

| Situation | Behaviour |
|---|---|
| `AddHealthChecks()` was never called | the host fails to start with an `InvalidOperationException` |
| `AddHealthChecks()` was called but nothing is tagged `primary` | no exception; the primary is treated as permanently unavailable and rehydration silently never runs |

With `CoreSystem.Cache.Redis` registered, the `redis_cache` check carries the
`primary` tag, so the second case only appears when rehydration is combined with
a provider that contributes no `primary`-tagged check.

After a successful cycle the component does not repeat while the primary stays
healthy; a new recovery requires another unhealthy-to-healthy transition.

------------------------------------------------------------------------

## ⚙️ Configuration

| Option | Default | Description |
|--------|---------|-------------|
| `Enabled` | `true` | Enables registration of the rehydration services |
| `Interval` | `30 seconds` | Delay used by the background rehydration service |

```csharp
services.AddCoreCacheRehydration(options =>
{
    options.Enabled = true;
    options.Interval = TimeSpan.FromSeconds(30);
});
```

When `Enabled` is `false`, the options instance stays registered but the
rehydration source, target, rehydrator, service and hosted background service
are not.

------------------------------------------------------------------------

## 🏗 Architecture

```text
         ┌──────────────────────┐
         │ IRehydrationSource   │  reads tracked entries
         │  (Memory fallback)   │
         └──────────┬───────────┘
                    │
                    ▼
         ┌──────────────────────┐
         │   ICacheRehydrator   │  coordinates the transfer
         └──────────┬───────────┘
                    │
                    ▼
         ┌──────────────────────┐
         │ IRehydrationTarget   │  writes to the primary
         └──────────────────────┘
```

`RehydrationService` drives the cycle from health-check transitions, and
`RehydrationBackgroundService` runs it on the configured interval. The
abstractions that separate source from target, like `ICacheStorageResolver`
behind them, are internal implementation boundaries; the package exposes no
public provider SDK for replacing either.

------------------------------------------------------------------------

## 📚 Documentation

The full reference is published at
[federin.github.io/CoreSystem.Cache/Rehydration](https://federin.github.io/CoreSystem.Cache/Rehydration):

-   [Overview](https://federin.github.io/CoreSystem.Cache/Rehydration/)
-   [Getting Started](https://federin.github.io/CoreSystem.Cache/Rehydration/GettingStarted/)
-   [Architecture](https://federin.github.io/CoreSystem.Cache/Rehydration/Architecture/)
-   [Configuration](https://federin.github.io/CoreSystem.Cache/Rehydration/Configuration/)
-   [Basic Usage](https://federin.github.io/CoreSystem.Cache/Rehydration/BasicUsage/)
-   [Roadmap](https://federin.github.io/CoreSystem.Cache/Rehydration/Roadmap/)

------------------------------------------------------------------------

## 🤝 Contributing

Issues and pull requests are welcome. Build and test the full solution with
`dotnet build CoreSystem.Cache.sln -c Release` and
`dotnet test CoreSystem.Cache.sln -c Release` before opening a PR.

------------------------------------------------------------------------

## 📄 License

Released under the MIT License.
