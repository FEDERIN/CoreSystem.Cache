# ⚙️ Configuration

This guide describes the configuration options available in
**CoreSystem.Cache**.

You'll learn how to configure:

- Cache provider behavior
- Serialization
- HTTP response cache expiration
- Cache key instance name
- Cache entry rehydration options

---

## 📋 Configuration Overview

The framework is configured through the `AddCoreCache()` extension.

```csharp
builder.Services.AddCoreCache(options =>
{
    // Configure the framework here
});
```

---

## ⚙️ Configuration Options

| Option | Description | Default |
|----------|-------------|---------|
| Enabled | Enables or disables the cache implementation | `true` |
| InstanceName | Optional prefix for cache keys | `null` |
| DefaultExpiration | HTTP response cache lifetime, used when `[Cacheable]` omits `expirationSeconds` | 30 minutes |
| SerializerType | Serialization format | JSON |

---

## 🔛 Enable or Disable the Cache

The cache can be disabled while keeping the same `ICoreCache` abstraction available.

```csharp
builder.Services.AddCoreCache(options =>
{
    options.Enabled = false;
});
```

When disabled, `AddCoreCache()` still registers an `ICoreCache`, but it is a
no-op implementation rather than the pipeline-backed one. The registration is
there on purpose: resolving `ICoreCache` keeps working, so application code does
not need to change when the cache is switched off. The concrete type is
`internal`, so it cannot be named, substituted, or pattern-matched from another
assembly.

Every call to `GetOrAddAsync()` runs the factory and returns its result, since
nothing is ever stored. Each read, write, remove, and tag invalidation is a
no-op that returns the empty value for its type instead of throwing.

---

## 🏷️ Instance Name

Prefixes cache keys with an application or environment identifier, when an
external provider is registered.

```csharp
options.InstanceName = "CatalogApi";
```

The option is intended to help avoid key collisions when multiple applications share the same cache infrastructure.

The Memory provider does not apply this prefix, so the option has no effect when
no external provider is registered.

---

## ⏳ Cache Expiration

`DefaultExpiration` applies **only to the HTTP response cache**. It sets the
lifetime of a cached response when the endpoint's `CacheableAttribute` does not
specify `expirationSeconds`.

```csharp
options.DefaultExpiration =
    TimeSpan.FromMinutes(30);
```

It is **not** a fallback for direct cache operations. `SetAsync` and
`GetOrAddAsync` store the entry with no expiration at all when the `expiration`
argument is `null`, so each call that needs a lifetime must pass one explicitly.

```csharp
await cache.SetAsync(
    "products",
    products,
    TimeSpan.FromMinutes(5));
```

There is no entry-size limit. Entries of any size are cached; use
`DefaultExpiration` and the `CacheableAttribute` to control how long an HTTP
response stays cached.

---

## 🔢 Serialization

Choose the serializer used by the cache.

Serialization is provided by **CoreSystem.Serialization**.

## 🧾 JSON

```csharp
options.SerializerType =
    SerializerType.Json;
```

JSON is the default serializer.

---

## 📦 MessagePack

```csharp
options.SerializerType =
    SerializerType.MessagePack;
```

---

## 📦 Protocol Buffers

```csharp
options.SerializerType =
    SerializerType.Protobuf;
```

---

## 🔁 Cache Entry Rehydration

`CacheEntryOptions` provides the `TrackForRehydration` flag.

The fallback pipeline uses:

```csharp
CacheEntryOptions.Rehydrate
```

when an operation is redirected from the primary storage to the fallback storage.

This marks the entry for rehydration when the external provider becomes available again.

The rehydration process itself belongs to the external-provider/recovery components and is not configured directly through the current `CacheOptions` class.

---

## 🔌 External Providers

`CoreSystem.Cache` can operate with its Memory provider without an external cache provider.

When an external provider is registered, it becomes the primary storage and Memory is used as the fallback storage.

The external provider configuration is handled by the corresponding provider package and is not part of the `CacheOptions` class shown in the current `CoreSystem.Cache` implementation.

---

## 💡 Recommended Configurations

### 🧪 Development

```csharp
builder.Services.AddCoreCache(options =>
{
    options.SerializerType = SerializerType.Json;
    options.DefaultExpiration = TimeSpan.FromMinutes(5);
});
```

---

### 🏭 Production

The core package does not define a production provider configuration inside `CacheOptions`.

When using an external provider, configure that provider through its corresponding package and keep the common cache settings in `AddCoreCache()`.

---

## ✅ Best Practices

- Use an `InstanceName` when multiple applications share the same cache infrastructure.
- Configure `DefaultExpiration` for HTTP-cached responses, and pass an explicit `expiration` on every `SetAsync` or `GetOrAddAsync` call that needs a lifetime.
- Prefer an explicit expiration over relying on a default: without one, entries do not expire.
- Choose the serializer according to the application's requirements.
- Use the Memory provider when an external distributed provider is not required.
- Configure external provider options in the corresponding provider package.
