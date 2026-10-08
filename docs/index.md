# ⚡ CoreSystem.Cache

> **Production-ready distributed caching framework for .NET 8**

![NuGet](https://img.shields.io/nuget/v/CoreSystem.Cache?style=for-the-badge)
![Downloads](https://img.shields.io/nuget/dt/CoreSystem.Cache?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8.0-blue?style=for-the-badge)
![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-Enabled-purple?style=for-the-badge)

CoreSystem.Cache is a distributed caching framework for .NET 8.

It provides a unified cache API with Cache-Aside support, HTTP response caching, resilience, fallback storage, and OpenTelemetry metrics. Health checks are supplied by the provider packages rather than by the core.

The framework is built around a pipeline architecture that separates cache operations from storage providers and cross-cutting behaviors.

## 📦 Companion packages

| Package | Responsibility |
|----------|----------------|
| **CoreSystem.Serialization** | JSON, MessagePack, and Protocol Buffers serialization |
| **CoreSystem.Http** | HTTP abstractions used by the middleware |
| **CoreSystem.Resilience** | Resilience pipelines used by the fallback behavior |
| **CoreSystem.Memory** | In-memory locking support (`AddCoreMemory()`), not the cache provider itself |
| **CoreSystem.Observability** *(Optional)* | Exporter and pipeline wiring for the metrics the core already emits |
| **CoreSystem.Observability.Abstractions** | Shared observability contracts for implementing custom instrumentation and integrations |

The in-memory cache **provider** ships inside `CoreSystem.Cache` itself, so no
extra package is required to use it.


> Installing **CoreSystem.Cache** pulls in `CoreSystem.Http`, `CoreSystem.Memory`, `CoreSystem.Resilience` and `CoreSystem.Serialization` as NuGet dependencies.

> **Optional:** Install **CoreSystem.Observability** to export the OpenTelemetry metrics that the core already creates and enables — the `cache.distributed.hits` and `cache.distributed.misses` counters are registered by `AddCoreCache()` itself. Install **CoreSystem.Observability.Abstractions** only if you need to build custom observability components or integrations.

> **CoreSystem.Cache** can operate with the in-memory provider without requiring an external cache provider.

> When an external provider such as Redis is configured, companion packages can provide Redis storage, resilience, and cache rehydration capabilities.

---

## 📚 Table of Contents

- [Getting Started](./GettingStarted.md)
- [Core Cache](./Why.md)
- [Providers](./Redis/index.md)
- [Rehydration](./Rehydration/index.md)
