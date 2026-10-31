# Memory

Facts and current repo state NOT covered by AGENTS.md. Read after AGENTS.md.

## Current state

- **Published chain verified against `flatcontainer/index.json` (Oct 2026):**
  `CoreSystem.Cache` 3.0.1 live, `CoreSystem.Cache.Rehydration` 2.1.2 live,
  `CoreSystem.Cache.Redis` 2.1.2 live, base-first order intact, every nuspec
  dependency resolvable. `main` also carries release prep for 3.1.0 / 2.2.0 / 2.2.0.
- **Rehydration and Redis never shipped 3.0.0** (jump 2.1.1 → 2.1.2); 3.0.0 is live
  for the core only, superseded by 3.0.1. Do not treat "3.0.0 ×3 shipped" as fact.
- **The .NET 10 restore failure (NU1605) is fixed only in the unpublished
  3.1.0 / 2.2.0 line**, not in anything already published. Root cause was
  Resilience 2.0.x constraining `Microsoft.Extensions.*` to 8.0.x; Resilience
  2.1.0 drops it. Nine bare-consumer probes (3 pkgs × 3 TFMs) restore clean.
- **Burned core versions below 2.1.1**: 2.1.0 (unlisted, from a base-repo test)
  and 2.0.3. Never reuse them.
- Analyzers under `latest-all`: CA2007 (130 ConfigureAwait) and CA1849 (16) are
  the noise floor. CA1064/CA1032 on `CacheDeserializationException` are dismissed
  on purpose (internal sealed, never reaches a consumer). Do not chase them.
- Baseline: build 0 warnings, format gate and `mkdocs build --strict` clean,
  423 unit tests pass (141 distinct × 3 TFMs); integration tests need Docker.

## Decisions

- In-repo deps are pinned `PackageReference` (released per tag, not lockstep).
  Cost: bump pins by hand, same commit as the version bump, base-first.
- Quality gates all on; the vulnerability gate is restore
  (`NuGetAudit` + `NuGetAuditMode=all` + TreatWarningsAsErrors), NOT a
  `dotnet list package --vulnerable` step (it exits 0 under advisories).
- `InternalsVisibleTo` to providers/tests is intentional.
- `UseLocalProjectReferences` only toggles lock files/locked mode; the
  conditional ProjectReference groups it used to gate were removed.

## Gotchas

- Full-solution restore rewrites the `CoreSystem.Cache` pin inside the Redis lock
  file to the live version — a correct update, not damage. Lock line-count claims
  of "367 → 61" came from a dirty tree; clean counts are Redis 367, Rehydration 323.
- On .NET 10, `BackgroundService.StartAsync` no longer runs `ExecuteAsync`
  synchronously: await a `TaskCompletionSource`, not a bare `StartAsync`.
- Multi-targeting runs one MTP process per TFM in parallel: any fixed port
  collides (`AddressInUseException`, victim moves between runs). Ephemeral ports.
- `mkdocs build --strict` does not catch unbalanced code fences; count ``` s.
- nuget.org `/nuspec`/`.nupkg` 404s in this sandbox lie; only
  `/v3-flatcontainer/<id>/index.json` is reliable (lags a few minutes).
  Authoritative: throwaway csproj restore in a temp dir, read `obj/project.assets.json`.
- New SDK 10 analyzer CA1873 fires on Moq `Verify` expression trees; suppressed
  inline, do not "fix" the mock.
- Action majors verified against the GitHub releases API: checkout v7,
  setup-dotnet v6, setup-python v7, upload-pages-artifact v5, deploy-pages v5,
  NuGet/login v1, codeql v4. Do not guess; query the API.

## Encoding

- Console codepage is ibm850: byte-level checks must read via raw path +
  explicit UTF-8 decode, never through PowerShell string pipelines.
- Never pipe emoji-containing files through PowerShell into `gh --body-file -`.
- All `.cs` carry a UTF-8 BOM that `dotnet format` preserves; do not churn.

## External references

- **[GitHub](https://github.com/FEDERIN/CoreSystem.Cache)** ·
  **[NuGet](https://www.nuget.org/packages/CoreSystem.Cache)** ·
  **[Docs](https://federin.github.io/CoreSystem.Cache/)**
