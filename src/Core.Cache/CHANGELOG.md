# CHANGELOG

## [Unreleased]

### Removed

- **`CacheOptions.MaxCacheableSize` is gone.** It was declared with a 1 MB default and copied by `CopyFrom`, but no storage ever read it, and it never did: it arrived dead in the standalone-repository migration and was never wired to anything since. It could not be implemented coherently either. Redis serializes first, so `payload.Length` is free, but the Memory provider stores the live object and never serializes, so measuring it would mean serializing every value purely to inspect it, on the fastest path. A limit that binds for Redis users and silently does nothing for Memory users is a worse trap than no limit. Entries of any size are cached; HTTP lifetime is configured through `DefaultExpiration` and `CacheableAttribute`. **This is a breaking API change**, so the next release is 3.0.0.

The declarative caching work described here previously was shipped in 2.0.2;
that history is kept below so it is explicit rather than silently dropped.

> **Historical note.** Entries claiming aspect-oriented caching "via
> reflection-based interception" were removed from this file. No such
> infrastructure exists. `CacheableAttribute` is read only by the ASP.NET Core
> response-cache middleware (`HttpCacheHandler`), which applies it to HTTP
> endpoint results. It does not intercept service or repository method calls,
> because there is no interceptor, no `DispatchProxy` and no reflection-based
> method invocation anywhere in this package.

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