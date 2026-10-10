# 🚀 Getting Started

`CoreSystem.Cache.Rehydration` is an optional component used with
`CoreSystem.Cache` and an external cache provider.

## 📦 Installation

```bash
dotnet add package CoreSystem.Cache.Rehydration
```

This pulls in `CoreSystem.Cache` as a dependency. Rehydration also requires an
external cache provider, so add the provider package as well when using one, for
example `CoreSystem.Cache.Redis`.

## 📋 Prerequisites

Register `CoreSystem.Cache` first and register an external cache provider before
enabling rehydration.

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

`InstanceName` is used as a Redis key prefix. The provider appends its own `:`
separator, so do not add a trailing colon: `"my-app"` produces
`my-app:products:1`, while `"my-app:"` would produce `my-app::products:1`.

`AddHealthChecks()` is **required**. `RehydrationService` takes a
`HealthCheckService` in its constructor, and the background service is resolved
when the host starts. Without it the host fails to start with an
`InvalidOperationException`.

Then register rehydration:

```csharp
services.AddCoreCacheRehydration(options =>
{
    options.Enabled = true;
    options.Interval = TimeSpan.FromSeconds(30);
});
```

Registration behaves as follows:

| Situation | Result |
|---|---|
| `AddCoreCache()` was never called | throws `InvalidOperationException` |
| No `IExternalCacheStorage` is registered | throws `InvalidOperationException` |
| The core cache is registered but disabled | services are not registered, **no exception** |
| `RehydrationOptions.Enabled` is `false` | services are not registered, **no exception** |

In the last two cases the options instance stays registered, but the rehydration
source, target, rehydrator, service and hosted background service are not.

## 🔁 Recovery Flow

When the core fallback stores an entry with `CacheEntryOptions.Rehydrate`,
the memory storage tracks that entry.

After the primary cache becomes healthy again, the rehydration service restores
the tracked entries to the primary provider.
