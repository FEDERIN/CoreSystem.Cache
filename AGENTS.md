# AGENTS.md

.NET 8 (SDK pinned to 8.0.402 in `global.json`) repository of three independently published NuGet caching libraries. Project folders are `src/Core.*`, package IDs are `CoreSystem.Cache*`. Solution: **`CoreSystem.Cache.sln`**. CoreSystem.Cache is part of the wider CoreSystem ecosystem (Http, Memory, Redis, Resilience, Serialization, Observability…), but those live in other repositories — do not re-create them here.

## Packages

| Project | Package ID | Depends on |
|---|---|---|
| `src/Core.Cache` | `CoreSystem.Cache` | `PackageReference`: CoreSystem.Http, CoreSystem.Memory, CoreSystem.Resilience, CoreSystem.Serialization |
| `src/Core.Cache.Rehydration` | `CoreSystem.Cache.Rehydration` | `ProjectReference`: Core.Cache |
| `src/Core.Cache.Redis` | `CoreSystem.Cache.Redis` | `ProjectReference`: Core.Cache.Rehydration · `PackageReference`: CoreSystem.Redis |

The dependency chain is `Core.Cache.Redis → Core.Cache.Rehydration → Core.Cache`. Those are **unconditional project references** — an agent must not "fix" them into package references. Only the cross-repo CoreSystem.* dependencies are NuGet packages, so changes to those packages do not reach this build until they are published and the central version is bumped.

All three projects set `GeneratePackageOnBuild=true`, so every build also produces a `.nupkg`. Each packs its own `README.md` via `PackageReadmeFile`; `Core.Cache` additionally packs its `CHANGELOG.md`. Never introduce a `README_NUGET.md` convention.

## Layout
- `tests/Core.Cache.UnitTests`, `Core.Cache.Redis.UnitTests`, `Core.Cache.Rehydration.UnitTests`: xUnit v3 + FluentAssertions + Moq.
- `tests/Core.Cache.IntegrationTests`: Testcontainers (`Testcontainers.PostgreSql`, `Testcontainers.Redis`) — needs Docker.
- `docs/` + `mkdocs.yml`: MkDocs site. `docs/`, `docs/Redis/` and `docs/Rehydration/` mirror the same page set; `mkdocs.yml` nav must list a page for it to build. Deployed to GitHub Pages on push to `main`.
- There is **no `samples/` directory** in this repository.

`InternalsVisibleTo` is granted from `Core.Cache` to both sibling providers and all four test projects (plus `DynamicProxyGenAssembly2` for Moq). Tests may touch `internal` members; that is intentional, not a leak.

## Dependencies / restore
- Versions are managed centrally in `Directory.Packages.props` (`ManagePackageVersionsCentrally`). Never put `Version=` on a `PackageReference`.
- `Directory.Build.props` sets `RestorePackagesWithLockFile` + `RestoreLockedMode`; `packages.lock.json` files are committed. After changing a package run `dotnet restore CoreSystem.Cache.sln --use-lock-file --force-evaluate` and commit the lock files, or restore fails.
- `Directory.Build.props` imports `Directory.Build.local.props` if present (gitignored). See `Directory.Build.local.props.example`. Note: the `UseLocalProjectReferences=true` switch it demonstrates is currently **inert** — the two conditional `ProjectReference` groups in `Core.Cache.Redis.csproj` and `Core.Cache.Rehydration.csproj` duplicate unconditional ones. It only becomes meaningful if a conditional project reference is ever made genuinely exclusive.

## Build properties — read this before assuming strict gates
`Directory.Build.props` sets only `TargetFramework`, `ImplicitUsings`, `Nullable`, `RestorePackagesWithLockFile`, `RestoreLockedMode`.

**Not set here:** `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, `AnalysisLevel`, `Deterministic`, `GenerateDocumentationFile`, SourceLink. There are no `src/Directory.Build.props` or `tests/Directory.Build.props` files, so no per-package metadata and no CA1707/CA1848 suppressions.

Consequences: warnings do **not** fail the build, and missing XML docs on public APIs are **not** a build error. XML docs are still expected by convention and by the package consumers. If you need hard gates, add them explicitly and say so — do not assume the strict setup of the other CoreSystem repos.

## Commands
- Build: `dotnet build CoreSystem.Cache.sln -c Release`
- Test all: `dotnet test CoreSystem.Cache.sln -c Release` (CI uses Release)
- Single project: `dotnet test tests/Core.Cache.Redis.UnitTests -c Release`
- Formatting is enforced in CI: `dotnet format CoreSystem.Cache.sln --verify-no-changes`; run `dotnet format CoreSystem.Cache.sln` to apply fixes.
- Regenerate lock files: `generate-lock-files.bat` (local only, gitignored exception).
- Docs: use `.venv\Scripts\mkdocs.exe build` (CI runs `mkdocs build --strict`). Do not use system Python. `serve-docs.bat` is a local-only convenience.

## CI / branches
- `ci.yml`: restore → build (Release) → `dotnet format --verify-no-changes` → test (Release) → NuGet vulnerability audit (`continue-on-error: true`) on push/PR to `main`.
- `documentation.yml`: `mkdocs build --strict` → GitHub Pages, gated on `docs/**` + `mkdocs.yml`.
- `publish.yml`: tag-driven, see Release below. `codeql.yml` and `dependabot.yml` also exist.
- `samples-smoke.yml` is **inert**: there is no `samples/` directory here, and its path filters match only `samples/**`, so it only runs on `workflow_dispatch`. Do not re-add `src/**` to its filters.
- `main` is protected: never push directly. Branch, then `gh pr create`.
- `*.bat` files are gitignored except `serve-docs.bat` and `generate-lock-files.bat` (explicitly allow-listed in `.gitignore`).
- Write files as UTF-8 explicitly. PowerShell `Set-Content` without `-Encoding utf8` emits mojibake (`U+FFFD`) and silently corrupts files; prefer the editor tools. Many committed files carry a UTF-8 BOM from Visual Studio — that is the existing state, do not churn it.

## Release
- Tag-driven via `publish.yml`. Tag format `<src folder name>/v<version>`, e.g. `Core.Cache.Redis/v2.0.2`. The workflow derives the project from the tag prefix and validates that `src/<prefix>` exists.
- It restores and builds **one** project with `/p:Version=<version>`, runs **all** tests, packs, then pushes to NuGet.org and GitHub Packages with `--skip-duplicate`.
- Keep `<Version>`, `AssemblyVersion`, `FileVersion` and `InformationalVersion` in the csproj in sync with the tag.
- Published versions are immutable — never reuse a version number.
- When a public surface changes, update that project's `README.md` (it ships in the nupkg) and the matching `docs/` pages.

## Known repo defects (fix when convenient)
- `ci.yml` uses `CoreSystem.Cache.sln` and `publish.yml` uses per-project paths with a per-package test mapping. Both were broken by a copy of the workflow set from another CoreSystem repo — do not reintroduce `CoreSystem.sln`.
- `samples-smoke.yml` is **inert by design**: there is no `samples/` directory, so its path filters match only `samples/**`. It runs on `workflow_dispatch` and activates by itself if a samples project is added. Do not re-add `src/**` to its filters.
- The NuGet audit step in `ci.yml` has `continue-on-error: true`, so it never blocks a merge. Remove that once the audit is clean.
- Nine pages exist but are absent from the `mkdocs.yml` nav, so the site never links them: `Redis/{HttpCache,Observability,Why}.md` and `Rehydration/{Extensibility,HealthChecks,HttpCache,Observability,Why}.md`.
- `CacheOptions.MaxCacheableSize` is declared and copied but **never read** by any code. Its docs now say it is inert; the open product decision (deprecate vs implement) is written up in `PLAN-MaxCacheableSize.md`.
- `AddCoreCacheRehydration()` does not document that `AddHealthChecks()` must be called; omitting it throws at host start. The same applies to Redis: without `AddHealthChecks()` the `redis_cache` check never exists, so rehydration cannot observe a recovery.

## Memory
- At the start, read `MEMORY.md` for current state and past decisions.
- When a task finishes, update it: current state, decisions with rationale, mistakes to avoid.
- Keep it short (max ~50 lines); drop what no longer adds value.
- If something becomes a permanent rule, move it to `AGENTS.md` instead.
- Never store sensitive data (keys, tokens, personal data).