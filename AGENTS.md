# AGENTS.md

## 1. Engineering Standards

Before performing any task, read `CORE-SYSTEM-STANDARDS.md`.

These standards are mandatory and apply to all development
activities in this repository.

The repository-specific instructions in this file complement
the common standards.

If a project-specific rule differs from a common standard,
follow the explicit project-specific exception.

## 2. Repository Overview

CoreSystem.Cache is a repository of three independently
published NuGet caching libraries.

- Solution: CoreSystem.Cache.sln
- SDK: 10.0.201 (pinned in `global.json`, `rollForward: latestFeature`)
- Target frameworks: net8.0;net9.0;net10.0
- Source: src/Core.*
- Tests: tests/
- There is no `samples/` directory and never was.

Part of the wider CoreSystem ecosystem (Http, Memory, Redis,
Resilience, Serialization...). Those live in other
repositories: do not recreate them here.

Follow CORE-SYSTEM-STANDARDS.md for common engineering rules.

## 3. Packages

| Project | Package ID |
|---|---|
| Core.Cache | CoreSystem.Cache |
| Core.Cache.Rehydration | CoreSystem.Cache.Rehydration |
| Core.Cache.Redis | CoreSystem.Cache.Redis |

All three set `GeneratePackageOnBuild=true`; each packs its own
`README.md` via `PackageReadmeFile`, and `Core.Cache` also packs
its `CHANGELOG.md`. Never introduce a `README_NUGET.md` convention.

## 4. Architecture and Dependencies

Dependency chain:

Core.Cache.Redis
    -> Core.Cache.Rehydration
        -> Core.Cache

- Core.Cache is the base caching library.
- Rehydration depends on Core.Cache.
- Redis depends on Core.Cache.Rehydration.
- Published dependencies use pinned PackageReferences.
- Do not replace PackageReferences with ProjectReferences: a
  ProjectReference makes the packed nuspec declare the referenced
  project's current version, so a provider could ship depending
  on a version nobody can restore.
- Provider versions must reference already published packages.
- Bump central package versions in the same commit as the
  corresponding provider release (base-first publication order).
- Cross-repo CoreSystem.* dependencies only reach this build once
  they are published AND the central version is bumped.

`InternalsVisibleTo` is granted from `Core.Cache` to both sibling
providers and all four test projects (plus `DynamicProxyGenAssembly2`
for Moq). Tests touching `internal` members is intentional, not a leak.

Do not recreate other CoreSystem ecosystem libraries
inside this repository.

## 5. Build Configuration

- Root Directory.Build.props defines shared properties:
  `TargetFrameworks`, `ImplicitUsings`, `Nullable`, lock/restore
  settings, and the quality gates (`TreatWarningsAsErrors`,
  `EnforceCodeStyleInBuild`, `AnalysisLevel=latest-recommended`,
  `Deterministic`, `NuGetAudit`, `NuGetAuditMode=all`).
- src/Directory.Build.props defines package metadata plus
  `GenerateDocumentationFile=true` and
  `GeneratePackageOnBuild=true`.
- tests/Directory.Build.props sets `GenerateDocumentationFile=false`
  and `NoWarn=CA1707` (tests ship nothing; the xUnit
  `Method_Scenario` convention is the point of the double underscore).
- Both subdirectory props files must explicitly import the root
  configuration. MSBuild only takes the nearest
  `Directory.Build.props`, so without the import they shadow the
  root and `TargetFrameworks` is never set.
- Every public member in `src/` requires XML documentation (CS1591).
- The packed README must come from `$(MSBuildProjectDirectory)\README.md`,
  never from the repository root: `PackageReadmeFile` names a file
  inside the package, and the root README describes the whole suite.
- Suppressions are inline `[SuppressMessage]` with a justification.
  CA1716 (`ICacheBehavior.InvokeAsync`'s `next`) and CA1711
  (`CacheDelegate`) are suppressed because renaming breaks a
  published 2.x contract; do not "fix" them.

## 6. Testing

- Unit tests: xUnit v3, FluentAssertions and Moq.
- Integration tests: Testcontainers; they require a running Docker daemon.
- Unit tests execute across multiple TFMs in parallel (one test
  process per TFM, all at once).
- Hosts must use ephemeral ports to avoid collisions: with
  multi-targeting, a test host that binds a fixed port collides with
  the parallel TFM processes and one loses randomly
  (`AddressInUseException`), which reads as flakiness, not as a cause.
  Use `UseUrls("http://127.0.0.1:0")`, or await completion through
  something deterministic (a `TaskCompletionSource`) rather than
  racing the thread pool. On .NET 10, `BackgroundService.StartAsync`
  no longer runs `ExecuteAsync` synchronously.

## 7. Dependencies and Restore

- Central Package Management in `Directory.Packages.props`.
  Never put `Version=` on a `PackageReference`.
- `CentralPackageTransitivePinningEnabled` is on: a `PackageVersion`
  entry can reach a project that never asked for that package.
  Dropping such an entry breaks restore instead of falling back.
- RestorePackagesWithLockFile and RestoreLockedMode are on by
  default; `packages.lock.json` files are committed.
- After changing any package:
  `dotnet restore CoreSystem.Cache.sln --use-lock-file --force-evaluate`,
  then commit the lock files in the same commit/PR as the change.
  Locally: `local-tools\generate-lock-files.bat`.
- A failed restore under RestoreLockedMode rewrites the committed
  lock files with empty entries instead of leaving them alone. If
  restore starts failing with "target frameworks of the project
  differ", run `git checkout -- 'src/*/packages.lock.json'` before
  investigating anything else. Stale `obj/` can also fake a NU1004
  that no longer exists; delete `bin/` and `obj/` before believing it.
- Dependabot NuGet PRs arrive failing restore with NU1004, because
  Dependabot edits `Directory.Packages.props` without regenerating
  the lock files. Fix in the Dependabot PR: regenerate and commit the
  lock files. Do not disable the audit or relax RestoreLockedMode.
- Directory.Build.local.props is local-only (gitignored), imported
  by the root Directory.Build.props when present.
- `UseLocalProjectReferences` must not modify the dependency graph.
  Its only remaining effect is turning OFF lock files and locked
  mode locally, so setting it regenerates the lock files in place.
  Do not reintroduce conditional ProjectReference groups (MSBuild
  silently collapses duplicated ProjectReference items instead of
  erroring, so the csproj misdescribes its own graph).

## 8. Commands

dotnet build CoreSystem.Cache.sln -c Release
dotnet test --solution CoreSystem.Cache.sln -c Release
dotnet test --project tests/Core.Cache.Redis.UnitTests -c Release
dotnet format CoreSystem.Cache.sln --verify-no-changes

The `--solution` / `--project` form is mandatory: the test runner
is Microsoft.Testing.Platform (configured in `global.json`) and it
does not accept a positional path.

Documentation:
- Build: `.venv\Scripts\mkdocs.exe build` (CI runs
  `mkdocs build --strict`). Do not use system Python.
- The `docs/` nav in `mkdocs.yml` must list a page for it to be
  reachable; a nav entry pointing at a missing file aborts the deploy.
- `docs/`, `docs/Redis/` and `docs/Rehydration/` mirror the same page set.

## 9. CI/CD

- GitHub Actions; CI builds and tests in Release.
- Both main workflows install the 8.0, 9.0 and 10.0 SDKs. Without
  them, multi-targeting fails with MSB3030 (apphost pack not found)
  for every framework below the installed one. The SDK from
  `global.json` still builds the solution.
- `ci.yml`: restore -> build -> format check -> test -> NuGet
  vulnerability report. The vulnerability gate is the restore step:
  `NuGetAudit` + `NuGetAuditMode=all` combined with
  `TreatWarningsAsErrors` turns NU1902/NU1903 into a hard failure.
  `dotnet list package --vulnerable` always exits 0 even when it
  reports a High advisory, so that step is informational only.
- `documentation.yml`: `mkdocs build --strict` -> GitHub Pages,
  gated on `docs/**` + `mkdocs.yml`.
- `publish.yml`: tag-driven, see Release. `codeql.yml`
  (`build-mode: none`) and `dependabot.yml` also exist.
- `samples-smoke.yml` is intentionally inert: its path filters
  match only `samples/**`, which does not exist, so it runs on
  `workflow_dispatch`. It activates by itself if a samples project
  is added. Do not re-add `src/**` to its filters and do not
  reintroduce `CoreSystem.sln` references.
- Main is protected: never push directly. Branch, then `gh pr create`.
- PRs, commit messages and their reasoning must be written in
  English, even when the conversation is in Spanish.
- `*.bat` files are gitignored except the explicitly allow-listed
  `serve-docs.bat`, `generate-lock-files.bat` and
  `publish-package.bat`.

## 10. Release

- Tag format: `<src folder>/v<version>`, e.g. `Core.Cache.Redis/v2.0.2`.
  The workflow derives the project from the tag prefix and validates
  that `src/<prefix>` exists.
- Keep `<Version>`, `AssemblyVersion`, `FileVersion` and
  `InformationalVersion` in the csproj in sync with the tag.
- The workflow restores and builds ONE project, runs ALL tests,
  packs, then pushes the exact file read from `PackageId`.
- `PackageId` never matches the folder name (`Core.Cache` ->
  `CoreSystem.Cache`): never derive the filename from the tag
  prefix, and never publish a `./nupkgs/*.nupkg` glob.
- There is no `--skip-duplicate`: it downgrades a 409 Conflict to a
  warning, so a re-run of an already published version would report
  success while publishing nothing. A 409 now fails the run, which
  is correct because published versions are immutable.
- Publications are base-first: Core.Cache, then Rehydration, then
  Redis. Publishing a dependent first puts a package on the feed
  whose dependency does not exist yet.
- Published versions are immutable; never reuse a version number,
  including ones that exist on the feed as unlisted.
- Release notes are written before the package is tagged: re-measure
  any claim carried from a previous version against a throwaway
  consumer before repeating it. A nuget.org 404 on the nuspec/nupkg
  endpoint is not proof a package is missing; only
  `/v3-flatcontainer/<id>/index.json` is reliable, and it lags.
  The authoritative check is a bare temp-directory csproj restore.
- When a public surface changes, update that project's `README.md`
  (it ships in the nupkg), the matching `docs/` pages, and — when the
  change belongs to a package that keeps one (`src/Core.Cache/`) —
  its `CHANGELOG.md`.

## 11. Known Repository Constraints

- No samples directory; do not create one implicitly.
- samples-smoke.yml is intentionally inert.
- Do not reintroduce CoreSystem.sln references.
- Do not reintroduce `MaxCacheableSize` (removed in 3.0.0; it was
  never wired to anything and cannot be implemented coherently).
- Preserve existing NuGet package metadata and lock behavior.

## 12. File Conventions

- Write files as UTF-8 explicitly. PowerShell `Set-Content` without
  `-Encoding utf8` emits mojibake (U+FFFD) and silently corrupts
  files; prefer editor tools.
- Many committed files (all `.cs`) carry a UTF-8 BOM from Visual
  Studio and `dotnet format` preserves it. Do not strip BOMs or
  churn line endings; `.gitattributes` keeps LF in the index.
- The console here runs at codepage 850: any byte-level check must
  read files through a raw path and explicit UTF-8 decoding, never
  through PowerShell string pipelines. Never pipe a file containing
  emoji through PowerShell into `gh --body-file -`; it silently
  drops non-ASCII characters.
- A PowerShell pipeline's exit code is not the tool's: re-run a
  bare command before treating a piped non-zero exit as a failure.

## 13. Memory

Read MEMORY.md at the beginning of a task.
Update it when significant decisions or project state change.
Keep it concise (max ~50 lines: summarize or drop what no longer
adds value).
If something becomes a permanent rule, propose moving it to
`AGENTS.md` instead of keeping it in memory.
Never store sensitive data (keys, tokens, personal data).
