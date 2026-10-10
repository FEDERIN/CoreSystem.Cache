# 🩺 Health Checks

CoreSystem.Cache.Rehydration integrates with the ASP.NET Core Health Checks infrastructure through the health state the core cache maintains.

The health check can be used to expose the operational state of the cache layer, including the state of the primary provider when an external provider and fallback storage are configured.

---

## ❓ Why It Matters

In production environments, an external cache provider can become temporarily unavailable.

When fallback support is configured, the framework can switch the current cache operation to the fallback provider while keeping the application running.

A health check can expose this state so monitoring systems can distinguish between:

- A healthy cache infrastructure.
- A degraded cache infrastructure operating with the fallback provider.

---

## 🩺 Registering Health Checks

Register the ASP.NET Core Health Checks service in the application.

```csharp
builder.Services.AddHealthChecks();
```

CoreSystem.Cache itself registers no health check. `AddCoreCache()` calls
`AddCacheDiagnostics()`, which registers OpenTelemetry metrics only, so
`AddHealthChecks()` on its own exposes an endpoint with no cache entry in it.

Health checks come from the provider packages. `CoreSystem.Cache.Redis`
contributes a check named `redis_cache`, tagged `cache` and `primary`, and it
appears as soon as `AddCoreCacheRedis()` is called.

`AddHealthChecks()` is required for rehydration: `RehydrationService` takes a
`HealthCheckService` in its constructor, so without it the host fails to start.

---

## 🔌 Expose the Health Endpoint

Expose the ASP.NET Core health endpoint as usual.

```csharp
app.MapHealthChecks("/health");
```

Example:

```text
GET /health
```

---

## 💚 Health States

When a health-check implementation reports the cache provider state, the expected operational distinction is:

| Status | Description |
|---------|-------------|
| 🟢 Healthy | The primary cache provider is available. |
| 🟡 Degraded | The primary provider is unavailable and the fallback provider is being used. |

The core package keeps track of the primary state itself, through the internal
`IPrimaryHealthStateWriter`, which `FallbackBehavior` calls when a primary
operation throws. The check that surfaces that state to `/health` is implemented
by the provider package, not by the core — see [Redis Health Checks](../Redis/HealthChecks.md).

---

## 🔄 Fallback State

When the primary storage fails and a fallback provider exists, `FallbackBehavior`:

- Marks the primary storage as unavailable.
- Changes the current cache context to the fallback storage.
- Marks the operation with `CacheEntryOptions.Rehydrate`.
- Executes the operation using the fallback storage.

This state can be used by a health-check implementation to report a degraded cache condition.

---

## 🔁 Cache Rehydration

When fallback operations are marked with:

```csharp
CacheEntryOptions.Rehydrate
```

the cache entry is prepared for rehydration by the recovery components.

Recovery is implemented by this package. `RehydrationService` observes the health
checks tagged `primary` and restores the tracked entries once the primary reports
healthy again after having been observed unhealthy.

---

## 📈 Monitoring

The ASP.NET Core health endpoint can be consumed by monitoring and orchestration systems that support Health Checks.

The exact health-check response and provider-state reporting depend on the health-check implementation registered by the application or the corresponding external package.

---

## 🧭 Operational Recommendations

### 💚 Healthy

The primary cache provider is operating normally.

---

### ⚠️ Degraded

The primary provider is unavailable and cache operations are using the configured fallback provider.

Recommended actions:

- Verify the external cache provider availability.
- Review provider logs.
- Check network connectivity.
- Check provider authentication and configuration.

Once the primary provider becomes available again, the recovery components can rehydrate entries marked for recovery.

---

## ✅ Best Practices

- Expose a health endpoint for production applications.
- Monitor degraded states instead of only failures.
- Combine Health Checks with OpenTelemetry metrics.
- Use readiness probes when deploying to orchestration platforms.
- Configure alerts for prolonged degraded states.
