# CHANGELOG

## [3.0.0]

### Removed

- **`CacheOptions.MaxCacheableSize` is gone.** It was declared with a 1 MB default and copied by `CopyFrom`, but no storage ever read it, and it never did: it arrived dead in the standalone-repository migration and was never wired to anything since. It could not be implemented coherently either. Redis serializes first, so `payload.Length` is free, but the Memory provider stores the live object and never serializes, so measuring it would mean serializing every value purely to inspect it, on the fastest path. A limit that binds for Redis users and silently does nothing for Memory users is a worse trap than no limit. Entries of any size are cached; HTTP lifetime is configured through `DefaultExpiration` and `CacheableAttribute`, and memory pressure is bounded with `IMemoryCache.SizeLimit` or the Redis server's `maxmemory-policy`. **Migration: delete any assignment to `MaxCacheableSize`.**
- **`ICacheTagIndex<T>`, `MemoryKeyTracker` and `DefaultRequestCachePolicy` are now `internal`.** All three were declared `public` while the contract they implement is `internal`, so no consumer outside the assembly could ever substitute or implement them: a public type whose only interfaces are internal is not a usable extension point, it is just a wider API surface. `ICacheTagIndex<T>` was the last public member of `Storage/Abstractions/`, whose siblings (`ICacheStorage`, `IExternalCacheStorage`, `ICacheEntry`, `ICacheEntryFactory`, `ICacheEntryInspector`, `ICacheKeyTracker`) were already internal. `DefaultRequestCachePolicy` matched its own XML documentation, which stated that the contract it satisfies is internal, and its sibling `DefaultResponseCachePolicy` was already `internal sealed`. The substitution seams these types sit behind are unchanged: `ICacheStorageResolver` still resolves the primary storage, and `CacheRedisRegistration` and `HttpRegistration` still register the contracts rather than the concrete types. **Migration: reference `IExternalCacheStorage` instead of `ICacheTagIndex<T>`, and remove any direct use of `MemoryKeyTracker` or `DefaultRequestCachePolicy`; all three are wired by the library at registration time.** Documented internal types are emitted into the packaged `Core.Cache.xml`, so removing their XML comments also drops eight unreachable entries from the shipped documentation file. These two changes are why this is a major release.

### Fixed

- **`docs/Configuration.md` described the disabled-cache registration incorrectly.** It claimed the no-op `ICoreCache` "cannot be resolved". It can: resolving `ICoreCache` keeps working when `Enabled` is `false`, which is deliberate. What is not possible is naming or pattern-matching the concrete type, because it is `internal`. The two facts are now separated.
- **`site/` was not gitignored**, so running `mkdocs build` locally left an untracked directory that `git add -A` would have committed.

### Changed

- The NuGet vulnerability audit is now a real gate. The `dotnet list package --vulnerable` step always exits `0` even when it reports a High severity advisory, so removing `continue-on-error` alone would have changed nothing. The gate is `NuGetAudit` with `NuGetAuditMode=all` plus `TreatWarningsAsErrors`, which turns NU1902/NU1903 into a restore failure. Transitive dependencies are now covered too; the default audits direct dependencies only.
- `dependabot.yml` shipped the GitHub template with empty `package-ecosystem` and `directory`, so it matched nothing and never ran. It now covers NuGet and GitHub Actions. A Dependabot NuGet PR arrives failing NU1004 because Dependabot does not regenerate `packages.lock.json`; run `dotnet restore CoreSystem.Cache.sln --use-lock-file --force-evaluate` and commit the lock files in the same PR.
- Workflows moved to current action majors (`checkout@v7`, `setup-dotnet@v6`, `setup-python@v7`, `upload-pages-artifact@v5`, `deploy-pages@v5`), which also stops the Node 20 deprecation warning that every run printed.
- `publish.yml` pushes the exact package read from `<PackageId>` instead of a `./nupkgs/*.nupkg` glob, and no longer uses `--skip-duplicate`, which downgraded a 409 Conflict to a warning and let a re-publish report success without publishing. A 409 now fails the run. A partially completed three-tag release is therefore no longer resumable by re-running the tags that succeeded.
- The duplicated conditional `ProjectReference` groups in `Core.Cache.Redis` and `Core.Cache.Rehydration` are gone. MSBuild was silently collapsing them, so the csproj misdescribed its own reference graph.
- The CHANGELOG no longer claims aspect-oriented caching "via reflection-based interception". No such infrastructure exists; `CacheableAttribute` is read only by the HTTP response cache middleware.

## [Unreleased]

Nothing pending.

The declarative caching work described in earlier versions of this file was
shipped in 2.0.2; that history is kept below so it is explicit rather than
silently dropped.

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