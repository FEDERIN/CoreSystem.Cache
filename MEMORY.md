# Memory

Facts and current repo state NOT covered by AGENTS.md. Read this after AGENTS.md.

## Current state

- **CoreSystem.Cache**, three packages. **2.1.1 is published on nuget.org and GitHub Packages for all three**, tags `Core.Cache/v2.1.1`, `Core.Cache.Rehydration/v2.1.1`, `Core.Cache.Redis/v2.1.1`. Verified by a clean-consumer `dotnet restore` that resolved all three at 2.1.1 plus the pinned cross-repo set, and by inspecting the shipped nuspecs. Nothing is outstanding.
- **Publish order is base-first, not reverse.** MEMORY previously said to tag in reverse dependency order; that is **wrong** and was corrected during the actual release. An unversioned `ProjectReference` becomes a nuspec dependency of `>= 2.1.1`, so tagging Rehydration first published a package whose `CoreSystem.Cache >= 2.1.1` dependency did not yet exist — unrestorable for ~2 minutes until the base landed. Correct order is `Core.Cache` → `Core.Cache.Rehydration` → `Core.Cache.Redis`, which guarantees that whenever a package appears on the feed every version it needs is already there.
- `publish.yml` is **not atomic** across the three packages, and uses `--skip-duplicate`, which silently hides a retry. If a run goes green without doing anything, read its log: confirm `Pushing ...nupkg` plus `Your package was pushed.`, because the run's exit code alone proves nothing.
- `main` at `2b2a864`, working tree clean, no open PRs. 12 PRs were merged in the audit session that started from a repository whose CI referenced a non-existent solution file.
- Baseline: `dotnet build -c Release` is **0 warnings / 0 errors** with `TreatWarningsAsErrors` on, 156 tests pass, format gate and `mkdocs build --strict` both exit 0, 27 doc pages with 0 orphans from the nav.
- **Why 2.1.1 and not 2.0.3:** `CoreSystem.Cache` 2.1.0 is in the nuget.org catalogue as **unlisted**. It was a publish test from the **base monorepo** `FEDERIN/CoreSystem` (its nuspec names that repository, commit `e422379b`, and the older dependency set Http 1.0.6 / Memory 1.0.0 / Resilience 2.0.0 / Serialization 1.2.0). NuGet cannot delete a published version, so it could only be unlisted. The number is permanently burned: never reuse 2.1.0. Going to 2.0.3 would have stepped the sequence backwards past something briefly visible on the feed. A minor is correct on content grounds too, because this release ships XML documentation, so consumers gain IntelliSense they did not have.
- Cross-repo pins: Http 1.1.0, Memory 1.0.3, Redis 1.0.3, Resilience 2.0.1, Serialization 1.2.3. Edits to those repos are invisible here until published and re-pinned.

## Decisions and rationale

- **In-project deps are `ProjectReference`, cross-repo are `PackageReference`.** All three cache packages ship from here, so building them from source is right; the CoreSystem.* packages are separately published so they must come from NuGet. csproj edits and `packages.lock.json` must move in the **same commit** or `RestoreLockedMode` breaks restore.
- `UseLocalProjectReferences` is **dead code**: both csprojs declare the same `ProjectReference` twice, once conditional and once unconditional. MSBuild tolerates it, the switch does nothing.
- **Quality gates are on** (Oct 2026): `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, `AnalysisLevel=latest-recommended`, `Deterministic`. `src/` requires XML docs on every public member and the generated file is packed into the `.nupkg`; `tests/` opts out of docs and of CA1707. CA1716 and CA1711 are suppressed with justifications because both fixes break published 2.x API.
- `InternalsVisibleTo` from `Core.Cache` to the sibling providers and all four test projects is intentional — that is how the tests reach `internal` members.
- Integration tests use Testcontainers, so they need a running Docker daemon. Unit tests do not.
- `CacheOptions.MaxCacheableSize` is **dead and treated as a no-op**: declared, copied by `CopyFrom`, never read. Docs say so. Do not describe it as a working limit.
- `codeql.yml` uses `build-mode: none` for C#, which is officially supported and verified working here. The `Run manual build steps` step never executes and is the documented fallback.

## Open items

- None blocking. The audit is closed and 2.1.1 is live.
- Consider making `publish.yml` push only the packed project instead of `./nupkgs/*.nupkg`. It happens to be correct today because `Pack --output ./nupkgs` writes one file, but the glob is an accident waiting for a csproj that packs twice.
- Consider dropping `--skip-duplicate` from the nuget.org push so a re-run fails loudly instead of reporting success without publishing.

## Gotchas

- **Solution is `CoreSystem.Cache.sln`.** Anything saying `CoreSystem.sln` was copied from a sibling repo and is wrong; it already broke CI once.
- **A failed restore under `RestoreLockedMode` rewrites `packages.lock.json` with empty entries instead of leaving it alone.** A restore that fails with NU1004 leaves the committed lock files gutted, and every later restore then fails for the same reason. If restore starts failing with "target frameworks of the project differ", run `git checkout -- 'src/*/packages.lock.json'` before investigating anything else.
- **`mkdocs build --strict` does not report unbalanced code fences.** Count ``` occurrences per file instead. An unpaired fence shipped once and passed CI.
- Scans for U+FFFD will not find CP850 mojibake. `ÔÜí` in place of `⚡` means UTF-8 content passed through a DOS OEM codepage; look for `[À-ÿ]` clusters. That corruption is **lossy** for emoji outside the Basic Multilingual Plane, so those files must be rewritten, not repaired.
- All `.cs` files carry a UTF-8 BOM and `dotnet format` preserves it. Do not strip it or churn line endings; `core.autocrlf=true` and there is no `.gitattributes`, so match whatever the file already does.
- `docs/` nav must list a page for it to be reachable, and a nav entry pointing at a missing file aborts the deploy.
- `Core.Cache.UnitTests.csproj` excludes `Behaviors\**` from compilation — reference material, not tests.
- **Do not trust a nuget.org 404 as proof a package is missing.** In this sandbox, `/v3-flatcontainer/<id>/<version>/<id>.nuspec` and `.nupkg` return 404 for packages that demonstrably exist, including the repo's own long-published 2.0.2. Only `/v3-flatcontainer/<id>/index.json` was reliable, and it lags by several minutes after a publish. The authoritative check is a throwaway `dotnet restore` of a project referencing the package, then read the resolved graph out of `obj/project.assets.json` and the nuspec/XML out of `~/.nuget/packages`.
- `main` is protected: branch, then `gh pr create`.

## External references

- **[GitHub](https://github.com/FEDERIN/CoreSystem.Cache)** · **[NuGet](https://www.nuget.org/packages/CoreSystem.Cache)** · **[Docs](https://federin.github.io/CoreSystem.Cache/)**