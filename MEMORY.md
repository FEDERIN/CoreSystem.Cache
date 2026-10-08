# Memory

Facts and current repo state NOT covered by AGENTS.md. Read this after AGENTS.md.

## Current state

- **CoreSystem.Cache**, three packages all at **2.0.2**: `CoreSystem.Cache`, `CoreSystem.Cache.Redis`, `CoreSystem.Cache.Rehydration`. Tags `Core.Cache*/v2.0.2` match the csproj versions. Published on NuGet.org 2026-09-20.
- `main` at `c4a8b23` after 8 merged PRs that fixed the repository: broken CI (`CoreSystem.sln` did not exist), a failing format gate, a link that aborted the Pages deploy, a Redis README that was a byte-identical copy of the core one, and ~20 documentation defects that misstated the public API.
- Baseline: `dotnet build -c Release` is 0 errors / **1 warning** (CS1998 in a test), 156 tests pass, format gate and `mkdocs build --strict` both exit 0, 27 doc pages with 0 orphans from the nav.
- **2.1.0 trap:** `CoreSystem.Cache` 2.1.0 exists in the nuget.org catalogue but is **unlisted** (registration reports 1900-01-01) with no tag and no matching commit here. Do not reuse that number, and find out where it came from before choosing a version.
- Cross-repo pins: Http 1.1.0, Memory 1.0.3, Redis 1.0.3, Resilience 2.0.1, Serialization 1.2.3. Edits to those repos are invisible here until published and re-pinned.

## Decisions and rationale

- **In-project deps are `ProjectReference`, cross-repo are `PackageReference`.** All three cache packages ship from here, so building them from source is right; the CoreSystem.* packages are separately published so they must come from NuGet. csproj edits and `packages.lock.json` must move in the **same commit** or `RestoreLockedMode` breaks restore.
- `UseLocalProjectReferences` is **dead code**: both csprojs declare the same `ProjectReference` twice, once conditional and once unconditional. MSBuild tolerates it, the switch does nothing.
- **No strict quality gates**, by omission. No `TreatWarningsAsErrors`, `AnalysisLevel` or `GenerateDocumentationFile`. "Warnings are errors" is false here; `dotnet format --verify-no-changes` is the only style gate that bites.
- `InternalsVisibleTo` from `Core.Cache` to the sibling providers and all four test projects is intentional — that is how the tests reach `internal` members.
- Integration tests use Testcontainers, so they need a running Docker daemon. Unit tests do not.
- `CacheOptions.MaxCacheableSize` is **dead and treated as a no-op**: declared, copied by `CopyFrom`, never read. Docs say so. Do not describe it as a working limit.
- `codeql.yml` uses `build-mode: none` for C#, which is officially supported and verified working here. The `Run manual build steps` step never executes and is the documented fallback.

## Open items

- **Quality gates.** Enabling them costs, measured: 66 CS1591 in `src/`, 12 CA1848 (`LoggerMessage` delegates), 1 CA1716 (`next` in `ICacheBehavior`), 1 CA1711 (`CacheDelegate`), 149 CA1707 in tests, 1 CS1998. The CA1716/CA1711 fixes are **breaking API changes** — suppress with a justification instead of renaming. Needs a `tests/Directory.Build.props` for CA1707.
- `src/Core.Cache/DependencyInjection/MemoryRegistration.cs:24` has dead commented code referencing `IRehydrationTracker`, a type that never existed post-split.
- Every documentation fix from the audit needs **a new package version** to reach consumers, because published versions are immutable.

## Gotchas

- **Solution is `CoreSystem.Cache.sln`.** Anything saying `CoreSystem.sln` was copied from a sibling repo and is wrong; it already broke CI once.
- **`mkdocs build --strict` does not report unbalanced code fences.** Count ``` occurrences per file instead. An unpaired fence shipped once and passed CI.
- Scans for U+FFFD will not find CP850 mojibake. `ÔÜí` in place of `⚡` means UTF-8 content passed through a DOS OEM codepage; look for `[À-ÿ]` clusters. That corruption is **lossy** for emoji outside the Basic Multilingual Plane, so those files must be rewritten, not repaired.
- All `.cs` files carry a UTF-8 BOM and `dotnet format` preserves it. Do not strip it or churn line endings; `core.autocrlf=true` and there is no `.gitattributes`, so match whatever the file already does.
- `docs/` nav must list a page for it to be reachable, and a nav entry pointing at a missing file aborts the deploy.
- `Core.Cache.UnitTests.csproj` excludes `Behaviors\**` from compilation — reference material, not tests.
- `main` is protected: branch, then `gh pr create`.

## External references

- **[GitHub](https://github.com/FEDERIN/CoreSystem.Cache)** · **[NuGet](https://www.nuget.org/packages/CoreSystem.Cache)** · **[Docs](https://federin.github.io/CoreSystem.Cache/)**