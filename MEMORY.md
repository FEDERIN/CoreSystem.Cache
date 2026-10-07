# Memory

Facts and current repo state NOT covered by AGENTS.md. Read this after AGENTS.md.

## Current state

- Repo: **CoreSystem.Cache** (github.com/FEDERIN/CoreSystem.Cache). Three packages, all at **2.0.2**: `CoreSystem.Cache`, `CoreSystem.Cache.Redis`, `CoreSystem.Cache.Rehydration`. Tags `Core.Cache*/v2.0.2` exist and match the csproj versions.
- Published on NuGet.org: Cache 2.0.2 (2026-09-20), Cache.Redis 2.0.2, Cache.Rehydration 2.0.2. Older versions 1.2.0/1.3.0/2.0.0/2.0.1 and Redis/Rehydration 2.0.0/2.0.1 predate this repo's current shape.
- **Oddity:** `CoreSystem.Cache` **2.1.0** exists in the nuget.org catalog but is unlisted (registration reports a 1900-01-01 publish date, the marker for unlisted/deleted entries) and has **no tag and no matching commit** here. Don't assume 2.0.2 is "latest everywhere"; check before picking the next version number, and never reuse a published number.
- Cross-repo dependencies pinned in `Directory.Packages.props`: CoreSystem.Http 1.1.0, Memory 1.0.3, Redis 1.0.3, Resilience 2.0.1, Serialization 1.2.3. Those come from other repositories — local edits there are invisible here until published and re-pinned.
- `main` is at commit `097fbc2` (merge of PR #2, "centralize build/docs/lockfiles"). Only 2 PRs exist so far.
- `dotnet build CoreSystem.Cache.sln -c Release` is green: 0 warnings, 0 errors, 7 projects.

## Decisions and rationale

- **In-project dependencies are `ProjectReference`, cross-repo ones are `PackageReference`.** Cache.Redis → Cache.Rehydration → Cache. Deliberate: these three ship together from this repo, so building them from source is correct. The CoreSystem.* packages are separately published, so they must come from NuGet.
- `UseLocalProjectReferences` is dead code. Both csprojs declare the same `ProjectReference` twice — once inside `Condition="'$(UseLocalProjectReferences)' == 'true'"` and once unconditionally. MSBuild tolerates the duplicate (build is clean), but the switch does nothing. It was presumably meant to let local builds bypass the published-package path; since the unconditional reference already exists, delete the conditional group rather than reasoning about which one wins.
- **No strict quality gates by choice-of-omission.** Unlike the sibling CoreSystem repos, this one sets no `TreatWarningsAsErrors` / `AnalysisLevel` / `GenerateDocumentationFile`. `dotnet format --verify-no-changes` in CI is the only style gate that actually bites. Treat "warnings are errors" as false here.
- `Core.Cache.csproj` grants `InternalsVisibleTo` to the sibling providers and all four test projects. This is how the unit tests reach `internal` members; it is intentional, and stripping it will break the suites.
- Integration tests use Testcontainers (PostgreSql, Redis), so they need a running Docker daemon. Unit tests do not.

## Open items

- **Fixed (Oct 2026, this working tree):** `ci.yml` now points at `CoreSystem.Cache.sln`; `publish.yml` restored to its committed version; `samples-smoke.yml` neutralized; `docs/Redis/index.md` broken link fixed; `dotnet format` applied. Verified: format gate exit 0, build 0 errors, 156 tests green, `mkdocs build --strict` exit 0. **Not committed yet.**
- **Fixed:** `src/Core.Cache.Redis/README.md` was a **byte-identical copy** of `src/Core.Cache/README.md` (SHA256 `EE5B64C6…`): it told Redis consumers to install the core package and never mentioned `AddCoreCacheRedis()`. Rewritten and verified inside the packed `.nupkg`.
- **Fixed:** `CacheOptions.DefaultExpiration` was documented as the fallback for cache writes in `docs/BasicUsage.md`, `docs/Configuration.md` (table, Cache Expiration, Best Practices, intro) and `src/Core.Cache/README.md`; the XML doc on the property itself was wrong too, so it also fed consumers' IntelliSense. Reality: its only consumer is `HttpCacheHandler.cs:58-60`, for `[Cacheable]` endpoints that omit `expirationSeconds`. `MemoryStorage.cs:42-48` and `RedisCacheStorage.cs:66` apply **no** expiration when `expiration` is `null`.
- `CacheOptions.MaxCacheableSize` is **documented as inert** (XML doc + `docs/Configuration.md` corrected in the audit) but the product decision is open: deprecate with `[Obsolete]`, implement, or leave reserved. Full plan in **`PLAN-MaxCacheableSize.md`** at the repo root — execute it after the remaining audit findings. Blocking design fact: Redis serializes so `payload.Length` is free, but Memory stores the live object and never serializes, so there is no byte size to check there without paying serialization on the fastest path.
- `docs/HealthChecks.md:30,59,86` contains review scaffolding that renders on the public site ("the source **provided for this review**", "**was not included in the source reviewed here**"). Same three sentences duplicated in `docs/Rehydration/HealthChecks.md`.
- `RehydrationService` needs `HealthCheckService`, so `AddHealthChecks()` is mandatory — undocumented. Following the Getting Started verbatim throws at host start. The same applies to Redis: the `redis_cache` check only exists if the app calls `AddHealthChecks()`, because `AddCacheDiagnostics` calls `AddMetrics()` and never `AddHealthChecks()`.
- `options.InstanceName = "my-app:"` in `src/Core.Cache.Rehydration/README.md:41` and `docs/Rehydration/GettingStarted.md:14` yields a doubled colon — `CacheRedisRegistration.cs:53` appends its own separator.
- `src/Core.Cache/DependencyInjection/MemoryRegistration.cs:24` has dead commented code referencing `IRehydrationTracker`, a type that never existed post-split.
- Decide whether to enable `TreatWarningsAsErrors` + `AnalysisLevel=latest-recommended` + `GenerateDocumentationFile`. Cost measured: 66 CS1591 in `src/`, 12 CA1848 (`LoggerMessage`), 1 CA1716 (`next` in `ICacheBehavior`), 1 CA1711 (`CacheDelegate`). The last two are breaking API changes — suppress with justification, do not rename.
- `codeql.yml` now matches the CoreSystem base project: C# uses `build-mode: none` (officially supported for C# since June 2024). The `Run manual build steps` step is dead code by design — nothing uses `manual` — and is the documented fallback if the analyze step ever fails.

## Gotchas

- **Solution name is `CoreSystem.Cache.sln`.** Any command, script or workflow copied from another CoreSystem repo that says `CoreSystem.sln` is wrong. This has already broken CI here.
- **The working tree carries a large pre-existing staged changeset** (`.opencode/`, workflows, lock files, `Directory.Build.props`) that is not in `HEAD` (`097fbc2`). Inspect `git diff HEAD` before committing — do not bundle it into unrelated work.
- That staged changeset was partly a **regression copied from another repo**: it replaced the repo-correct `publish.yml` (per-package test mapping + OIDC `NuGet/login@v1`) with a version using `CoreSystem.sln` and a raw `secrets.NUGET_API_KEY`. It was reverted. `documentation.yml`'s staged changes were genuine improvements and were kept.
- All 125 `.cs` files carry a UTF-8 BOM. `dotnet format` preserves it. Do not strip it.
- Docs build is `--strict`, so a nav entry pointing at a missing file aborts the deploy. Nine pages are currently orphaned from the nav (INFO only, does not fail the build).
- `Core.Cache.UnitTests.csproj` explicitly excludes `Behaviors\**` from compilation — those files are reference material, not tests. Adding code there will not run.
- `RestoreLockedMode` is on and `packages.lock.json` files are committed. After any package change: `dotnet restore CoreSystem.Cache.sln --use-lock-file --force-evaluate`, then commit the lock files.
- Docs: use `.venv\Scripts\mkdocs.exe build`; CI runs `mkdocs build --strict`, so an unlisted `docs/**` file will not fail but a *nav* entry pointing at a missing file will. `serve-docs.bat` is gitignored-but-allow-listed.
- Published NuGet versions are immutable; `--skip-duplicate` hides a repeated push instead of reporting it.
- Files are UTF-8, many with a BOM. Don't normalize line endings or BOMs — it creates noise-only diffs.
- `main` is protected: branch + `gh pr create`, never push directly.