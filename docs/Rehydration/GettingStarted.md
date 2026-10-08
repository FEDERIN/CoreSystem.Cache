# 🚀 Getting Started

`CoreSystem.Cache.Rehydration` is an optional component used with
`CoreSystem.Cache` and an external cache provider.

## Prerequisites

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
```

`InstanceName` is used as a Redis key prefix. The provider appends its own `:`
separator, so do not add a trailing colon: `"my-app"` produces
`my-app:products:1`, while `"my-app:"` would produce `my-app::products:1`.
```

Then register rehydration:

```csharp
services.AddCoreCacheRehydration(options =>
{
    options.Enabled = true;
    options.Interval = TimeSpan.FromSeconds(30);
});
```

The registration requires:

- `Core.Cache` to be registered and enabled;
- an `IExternalCacheStorage` to be registered.

Otherwise, registration throws an `InvalidOperationException`.

## Recovery Flow

When the core fallback stores an entry with `CacheEntryOptions.Rehydrate`,
the memory storage tracks that entry.

After the primary cache becomes healthy again, the rehydration service restores
the tracked entries to the primary provider.
