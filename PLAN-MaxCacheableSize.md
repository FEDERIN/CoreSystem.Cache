# Plan — `CacheOptions.MaxCacheableSize`

**Status:** deferred. Not started.
**Created:** 2026-10-07, during the repository documentation/quality audit.
**Blocks:** nothing. Can be executed independently once the audit findings are closed.

---

## 1. Problem

`CacheOptions.MaxCacheableSize` is part of the public API surface of
`CoreSystem.Cache`, is documented as a working entry-size limit, and is **never
read by any code**.

Verified occurrences, exhaustive:

| Location | What it is |
|---|---|
| `src/Core.Cache/Options/CacheOptions.cs:49` | the declaration, default `1024 * 1024` |
| `src/Core.Cache/Options/CacheOptions.cs:69` | copied by `CopyFrom` |
| `tests/Core.Cache.UnitTests/Options/CacheOptionsTests.cs:16,27` | asserts only that `CopyFrom` copies it |
| `docs/Configuration.md` | documentation, corrected during the audit |

There is no size check in `MemoryStorage`, `RedisCacheStorage` or
`HttpCacheHandler`. A search of `src/` for `.Length`, `MaxSize`, `SizeLimit`
and `byte[]` found only array lengths on `tags`.

**Already done in the audit:** the XML doc and the docs no longer claim the
limit is enforced. As of now the documentation is truthful and says the option
is reserved. What remains is the product decision below.

---

## 2. The constraint that drives the decision

The option cannot be implemented coherently across providers, because the two
storages do not represent values the same way:

- **Redis** (`RedisCacheStorage.cs:63-68`) serializes first —
  `var payload = _payloadSerializer.Serialize(value);` — so the byte length is
  available for free as `payload.Length`.
- **Memory** (`MemoryStorage.cs:53` → `CacheEntryFactory.cs:15-20`) stores the
  **live object** inside a `CacheEntryWrapper<T>`. It never serializes.
  There is no byte size to read, and measuring one would mean serializing every
  value purely to inspect it — doubling the cost of the memory path, which is
  the one path where that cost matters most.

`CoreSystem.Serialization` 1.2.3 exposes `Serialize` / `Deserialize` only; there
is no `GetSize` or equivalent (checked in the package metadata). So no size can
be obtained without producing the payload.

**Consequence:** enforcing only on Redis would make the option bite for Redis
users and silently do nothing for Memory users. That is a worse footgun than
having no limit at all, and it would need documenting as provider-specific.

---

## 3. Options

### Option A — Deprecate (`[Obsolete]`), remove in 3.0

Mark the property obsolete with a message stating it is not implemented, and
plan its removal for the next major version.

- Preserves binary and source compatibility.
- Consumers get a **compiler-visible** warning instead of a silent no-op.
- No behaviour change, so no risk to existing entries.
- Keeps the intent visible in the code rather than in a docs page.

Cost: every consumer that set the option gets a new warning. Message wording
matters, since some treat warnings as errors.

### Option B — Implement it properly (feature work)

Requires deciding the semantics first, then touching three layers:

- **Redis** — reject or skip when `payload.Length > MaxCacheableSize`. Cheap.
- **Memory** — no byte size exists. Options: force serialization to measure it
  (expensive), or move to `IMemoryCache` `SizeLimit` with a caller-supplied size
  per entry (API change).
- **HTTP** — `HttpCacheHandler.cs:69-74` captures `response.Body`; if that is
  `byte[]` the size is measurable. **Confirm the type in `CoreSystem.Http`
  before planning this layer** — the type is defined in that package, not here.

Plus: decide whether exceeding the limit throws, skips silently, or is logged.
Plus tests, plus docs, plus a new version.

### Option C — Leave reserved

Keep the property, documented as unimplemented (the current state). Zero risk,
but the public surface keeps an option that does nothing indefinitely.

---

## 4. Recommendation

**Option A — deprecate.**

Rationale:

1. Option B cannot be made coherent without either forcing serialization onto
   the memory path (Section 2) or changing the public storage abstraction. Both
   are larger than the option justifies, and the first is a performance
   regression on the fastest provider.
2. A provider-dependent limit is a trap: the same `MaxCacheableSize = 1MB`
   would be enforced for Redis users and ignored for Memory users, and neither
   group could tell from the docs alone.
3. Option A converts a silent lie into an explicit signal, which is the whole
   problem, and keeps the door open for a coherent 3.x design.

Revisit if the intent was ever a genuine entry-size guard; in that case it
belongs in a major version, together with a storage abstraction that can
express entry size for non-serialized providers.

---

## 5. Steps for the recommended path (Option A)

1. Confirm the decision with the package owner. If the intent is Option B, stop
   and re-plan before touching code.
2. Add `[Obsolete]` to `CacheOptions.MaxCacheableSize` with a message that says
   the option is not implemented and points at the removal in 3.0. Do not use
   `error = true`; a warning is intended.
3. Update the XML doc `<remarks>` to match the deprecation wording, keeping the
   "no cache storage reads this value" statement from the audit fix.
4. Update `docs/Configuration.md`:
   - the options table row,
   - the `## Maximum Cacheable Size` section, replacing the current
     "reserved and currently has no effect" warning with the deprecation notice.
5. Grep for remaining references and make each one consistent:
   ```
   Select-String -Path src,tests,docs -Pattern MaxCacheableSize
   ```
   `CacheOptionsTests.cs:16,27` can stay — it only asserts `CopyFrom` copies the
   value, which remains true.
6. Because `[Obsolete]` is now in use, decide whether the existing audit item
   "enable `TreatWarningsAsErrors`" becomes harder: consumers are not affected,
   but this repo's own build will warn wherever the property is referenced.
   Verify the build stays clean.

## Steps if Option B is chosen instead

1. Answer the semantics question first: exceeding the limit throws, skips, or
   logs-and-skips?
2. Decide how Memory expresses entry size. This is the blocking design question;
   everything else follows from it.
3. Confirm `CapturedResponse.Body`'s type in `CoreSystem.Http`.
4. Implement storage-by-storage, with tests per provider, and a metric or log
   line for rejected entries so the behaviour is observable.

---

## 6. Verification

Whatever the option, before considering it done:

```powershell
dotnet build CoreSystem.Cache.sln -c Release          # 0 errors
dotnet test CoreSystem.Cache.sln -c Release           # 156 tests green baseline
dotnet format CoreSystem.Cache.sln --verify-no-changes
.venv\Scripts\mkdocs.exe build --strict
```

Plus, for Option A, confirm no new warning is introduced in this repo's own
build, and confirm the `CoreSystem.Cache` `.nupkg` still packs and carries an
updated `README.md`.

---

## 7. Release constraint — resolve before shipping any fix

Published NuGet versions are immutable, so this change needs a new version of
`CoreSystem.Cache`. **The next number is not settled:**

- Current csproj and tags: **2.0.2**.
- `CoreSystem.Cache` **2.1.0** already exists in the nuget.org catalogue but is
  **unlisted** (the registration API reports a `1900-01-01` publish date, the
  marker for unlisted entries), and it has **no matching tag and no matching
  commit** in this repository.

So 2.1.0 must not be reused, and its origin should be understood before a
number is chosen. `CoreSystem.Cache.Redis` and `CoreSystem.Cache.Rehydration`
are at 2.0.2 and would need the same treatment if their surfaces change.

Note that the README corrections already made in this working tree (Redis
README, `DefaultExpiration`, `MaxCacheableSize`, `InstanceName`) also require a
new version to reach consumers. They do not require this plan to be finished
first, but they should ship together.

---

## 8. Risks

- **Deprecating a property consumers already set.** They will see a new
  warning. Wording must be explicit that nothing breaks.
- **Scope creep into a 3.x design.** Option B touches three layers and the
  public storage abstraction. Keep it out of a patch release.
- **Untracked working tree.** The audit fixes live uncommitted alongside a
  pre-existing staged changeset. Sequence the commits before tagging.