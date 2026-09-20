# CoreSystem.Cache

Production-ready caching libraries for modern .NET applications.

CoreSystem.Cache is a modular caching solution for .NET applications, providing a core cache implementation together with Redis distributed caching and cache rehydration capabilities.

## Components

### CoreSystem.Cache

The core caching library provides the main caching capabilities and orchestration.

It is designed to provide:

- Cache-aside operations
- Memory-based caching
- Tag-based invalidation
- Configurable cache execution pipeline
- Optional resilience integration
- Health checks
- Observability and metrics
- Extensibility through abstractions

### CoreSystem.Cache.Redis

Redis support provides distributed caching capabilities for applications that need shared cache state across multiple instances.

It provides:

- Redis-backed cache storage
- Distributed cache operations
- Integration with the CoreSystem.Cache infrastructure
- Health checks
- Observability
- Configurable Redis connectivity

### CoreSystem.Cache.Rehydration

The Rehydration component provides capabilities for rebuilding or restoring cached data when the cache is unavailable, expired, or needs to be reconstructed.

It is designed to work together with the core caching infrastructure without coupling application code directly to a specific cache provider.

## Architecture

```text
                    ┌──────────────────────────┐
                    │     Application / API    │
                    └────────────┬─────────────┘
                                 │
                                 ▼
                    ┌──────────────────────────┐
                    │    CoreSystem.Cache      │
                    │                          │
                    │  Cache infrastructure    │
                    │  Cache orchestration     │
                    │  Invalidation            │
                    │  Observability           │
                    └────────────┬─────────────┘
                                 │
                    ┌────────────┴────────────┐
                    │                         │
                    ▼                         ▼
          ┌──────────────────┐      ┌──────────────────────┐
          │ Cache Providers  │      │ Cache Rehydration    │
          │                  │      │                      │
          │ Memory           │      │ Rebuild / restore    │
          │ Redis            │      │ cached data          │
          └────────┬─────────┘      └──────────────────────┘
                   │
                   ▼
          ┌──────────────────┐
          │      Redis       │
          │    Distributed   │
          │      Cache       │
          └──────────────────┘
```

## Installation

Install only the components required by your application.

### Core Cache

```bash
dotnet add package CoreSystem.Cache
```

### Redis

```bash
dotnet add package CoreSystem.Cache.Redis
```

### Rehydration

```bash
dotnet add package CoreSystem.Cache.Rehydration
```

## Getting Started

A typical application starts with the core cache infrastructure and can add Redis or Rehydration when required.

Refer to the project documentation for the current configuration and usage examples:

**[CoreSystem.Cache Documentation](https://federin.github.io/CoreSystem.Cache/)**

## Documentation

The documentation covers:

- Getting Started
- Core Cache
- Architecture
- Configuration
- Basic Usage
- HTTP Cache
- Health Checks
- Observability
- Extensibility
- Redis Provider
- Redis Configuration
- Redis Basic Usage
- Redis Health Checks
- Redis Extensibility
- Rehydration
- Rehydration Configuration
- Rehydration Basic Usage
- Rehydration Roadmap

## Design Goals

CoreSystem.Cache is designed around several principles:

- **Modularity** — use only the components your application needs.
- **Provider independence** — keep application code decoupled from the underlying cache provider.
- **Distributed caching** — support shared cache state through Redis.
- **Resilience** — integrate with resilience capabilities when required.
- **Observability** — expose metrics and health information.
- **Extensibility** — allow applications to extend the caching infrastructure.
- **Production readiness** — provide infrastructure suitable for modern .NET applications.

## Use Cases

CoreSystem.Cache can be used in applications that need:

- Application-level caching
- Cache-aside patterns
- Distributed caching
- Redis-backed caching
- Cache invalidation
- HTTP response caching
- Cache health monitoring
- Cache observability
- Cache reconstruction and rehydration

## Related Projects

CoreSystem.Cache is part of the broader CoreSystem ecosystem.

**[CoreSystem](https://federin.github.io/CoreSystem/)**

## Requirements

- .NET 8
- Redis is required only when using `CoreSystem.Cache.Redis`

## License

See the [LICENSE](./LICENSE) file for license information.
