# 🗺️ Roadmap

This document outlines the planned evolution of **CoreSystem.Cache**.

The roadmap provides visibility into the long-term direction of the project. Features may evolve based on production experience, community feedback, and real-world adoption.

---

## Guiding Principles

- Production-first
- Cloud-native
- Provider-independent
- Extensible architecture
- OpenTelemetry-first
- Backward compatibility whenever possible

---

## ✅ Current Capabilities

### Core Infrastructure

- [x] In-memory cache provider
- [x] External storage abstraction
- [x] Dependency Injection integration

### Cache Pipeline

- [x] Composable execution pipeline
- [x] Logging, metrics, resilience, and fallback behaviors
- [x] Configurable pipeline ordering and conditional behaviors

### Caching Features

- [x] Cache-aside pattern (`GetOrAddAsync`)
- [x] Tag-based invalidation
- [x] Concurrent cache-population protection
- [x] HTTP response caching
- [x] Redis provider and cache rehydration component

### Observability

- [x] OpenTelemetry metrics
- [x] Redis health checks

---

## 🚧 Near-Term Goals

### Developer Experience

- [ ] More end-to-end samples
- [ ] Expanded provider and migration guides
- [ ] XML documentation improvements
- [ ] Additional integration tests

### Extensibility and Performance

- [ ] Public provider SDK
- [ ] Custom pipeline behavior registration
- [ ] Benchmark suite and additional performance metrics
- [ ] Compression behavior

---

## 🔮 Long-Term Vision

### Multi-Level Cache

- [ ] L1 and L2 cache support
- [ ] Transparent synchronization
- [ ] Adaptive expiration

### Advanced Capabilities

- [ ] Cache analytics
- [ ] Native AOT optimizations
- [ ] Source Generator support
- [ ] Community storage-provider templates

---

## Community Ideas

Suggestions and contributions are welcome through GitHub Issues, Discussions, and Pull Requests.

## Release Strategy

The project follows Semantic Versioning.

| Version | Focus |
|---------|-------|
| 1.x | Stability, bug fixes, documentation |
| 2.x | Extensibility and advanced pipeline capabilities |
| 3.x | Advanced caching strategies |
