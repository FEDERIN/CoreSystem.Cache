# CHANGELOG

## [Unreleased]

### Added

- **Declarative Caching Support**: Introduced `[Cacheable]` attribute in `Core.Cache.Attributes` for declarative caching at the method level.
- **Aspect-Oriented Caching**: Implemented infrastructure to support automatic caching via reflection-based interception, enabling seamless integration with services and repositories beyond just API controllers.

### Changed

- **Architectural Refinement**: Enhanced the caching strategy to allow universal usage across service and repository layers using attribute-based resolution.
- **Documentation**: Updated README.md to include the new Declarative Caching feature and configuration guidelines.

## [2.1.1]

### Added

- **XML documentation is now shipped in the package.** Every public type and member in `CoreSystem.Cache` is documented, and the generated file is packed, so IntelliSense works for consumers without decompiling.
- **LoggerMessage delegates** replace `ILogger` extension calls across the logging paths, so log templates are parsed once instead of on every call. Internal only, no behavioural change.

### Fixed

- **`DefaultExpiration` was documented as a fallback for cache writes. It never was.** Its only consumer is the HTTP response cache, applied when `[Cacheable]` omits `expirationSeconds`. `SetAsync` and `GetOrAddAsync` store the entry with no expiration when `expiration` is `null`. Corrected in the docs, in the READMEs and in the XML documentation on the property.
- **`AddHealthChecks()` is a required prerequisite for rehydration** and was undocumented. `RehydrationService` takes a `HealthCheckService`, so omitting it made the host fail to start. It also gates the `redis_cache` check, without which rehydration can never observe a recovery.
- **`InstanceName` examples produced doubled-colon keys.** The Redis provider appends its own `:`, so the documented `"my-app:"` produced `my-app::products:1`.
- **Review scaffolding removed from the health check documentation**, including the claim that the rehydration service lived outside this repository. It does not; it is `CoreSystem.Cache.Rehydration`.

### Changed

- **`MaxCacheableSize` is documented as reserved and inert.** It is declared and copied but never read, so no entry-size limit is enforced.
- Quality gates are enabled: `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild` and `AnalysisLevel=latest-recommended`. Build is clean.

### Compatibility

- No public API change. Every modified type in this release is `internal`.

## [2.0.1]

### Fixed

- **Dependency Injection**: Fixed service registration when `Core.Cache` is used without an external cache provider.
- **Fallback Registration**: `FallbackBehavior` is now registered only by providers that support cache fallback.
- **Memory-Only Configuration**: `Core.Cache` can now be used with the built-in Memory provider without requiring `IPrimaryHealthStateWriter`.

### Compatibility

- No Redis provider is required to use `Core.Cache` with Memory caching.