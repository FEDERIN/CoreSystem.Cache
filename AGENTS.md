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

## Build properties
Root `Directory.Build.props`: `TargetFramework`, `ImplicitUsings`, `Nullable`, `RestorePackagesWithLockFile`, `RestoreLockedMode`, plus the quality gates `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, `AnalysisLevel=latest-recommended`, `Deterministic`.

`src/Directory.Build.props` adds `GenerateDocumentationFile=true`, so **every public member in `src/` needs an XML doc** (CS1591) and the generated file is packed into the `.nupkg`. Write real summaries, not filler.

`tests/Directory.Build.props` sets `GenerateDocumentationFile=false` and `NoWarn=CA1707`, since tests ship nothing and the xUnit `Method_Scenario` convention is the point of the double underscore.

**Both subdirectory files import the root explicitly.** MSBuild stops at the nearest `Directory.Build.props`, so without that import they shadow the root and `TargetFramework` is never set — restore then fails NU1004 and **rewrites the committed lock files with empty ones** instead of leaving them alone.

Suppressions are inline `[SuppressMessage]` with a justification. CA1716 (`ICacheBehavior.InvokeAsync`'s `next`) and CA1711 (`CacheDelegate`) are suppressed because renaming either breaks a published 2.x contract; do not "fix" them.

## Commands
- Build: `dotnet build CoreSystem.Cache.sln -c Release`
- Test all: `dotnet test CoreSystem.Cache.sln -c Release` (CI uses Release)
- Single project: `dotnet test tests/Core.Cache.Redis.UnitTests -c Release`
- Formatting is enforced in CI: `dotnet format CoreSystem.Cache.sln --verify-no-changes`; run `dotnet format CoreSystem.Cache.sln` to apply fixes.
- Regenerate lock files: `generate-lock-files.bat` (local only, gitignored exception).
- Docs: use `.venv\Scripts\mkdocs.exe build` (CI runs `mkdocs build --strict`). Do not use system Python. `serve-docs.bat` is a local-only convenience.

## CI / branches
- `ci.yml`: restore → build (Release) → `dotnet format --verify-no-changes` → test (Release) → NuGet vulnerability report on push/PR to `main`. **The vulnerability gate is the restore step, not the last one.** `dotnet list package --vulnerable` always exits 0 even when it reports a High advisory, so that step is informational; the real gate is `NuGetAudit` + `NuGetAuditMode=all` in the root `Directory.Build.props` combined with `TreatWarningsAsErrors`, which turns NU1902/NU1903 into a hard restore failure.
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
- ~~The NuGet audit step in `ci.yml` has `continue-on-error: true`~~ **Fixed.** Removing that flag alone would have changed nothing: `dotnet list package --vulnerable` exits 0 even when it reports a High advisory, in both text and `--format json`. The gate is now `NuGetAudit` + `NuGetAuditMode=all` in the root `Directory.Build.props`, which makes restore fail on NU1902/NU1903 via `TreatWarningsAsErrors`. Verified by temporarily adding a vulnerable package: restore fails across all seven projects. The repo audits clean at the time of writing.
- `CacheOptions.MaxCacheableSize` is declared and copied but **never read** by any code. Its docs correctly say it is reserved and inert. Treat it as a no-op; do not document it as a working limit.
- `docs/Configuration.md` describes `MaxCacheableSize` in the options table and its own section. If that property is ever removed, both go with it.

## Memory
- At the start, read `MEMORY.md` for current state and past decisions.
- When a task finishes, update it: current state, decisions with rationale, mistakes to avoid.
- Keep it short (max ~50 lines); drop what no longer adds value.
- If something becomes a permanent rule, move it to `AGENTS.md` instead.
- Never store sensitive data (keys, tokens, personal data).