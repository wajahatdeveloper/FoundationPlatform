# Identity — Architecture Audit

## Context

Scope read in full:
- `Runtime/Identity/IdentityComponent.cs`
- `Runtime/Identity/IdentityField.cs`
- `Editor/Identity/IdentityComponentEditor.cs`
- `Editor/Identity/IdentityDuplicationHandler.cs`
- `Editor/Identity/IdentityFieldDrawer.cs`
- `Editor/Identity/IdentityGizmoDrawer.cs`

The `Identity` value type itself is **not** in either assigned folder — it is defined at
`Runtime/Messaging/EventBus/Identity.cs` (namespace `AetherNexus.FoundationPlatform.Messaging`), alongside
`IIdentity` (`Runtime/Messaging/EventBus/IIdentity.cs`) and `BaseGameEvent`
(`Runtime/Messaging/EventBus/BaseGameEvent.cs`, which implements `IIdentity`). It was read in full for
context because every file in the assigned scope depends on it directly. `Runtime/Identity/` and
`Editor/Identity/` contain only **consumers** of `Identity` (`IdentityComponent`, the `[IdentityField]`
attribute + its drawer, duplicate-ID enforcement, and a scene-view gizmo) — not the type definition.

Cross-checked against `docs/00-AgentGuide.md` §3 (`Identity` gotcha), `docs/01-CorePrinciples.md`,
`docs/10-Integrations.md` (player-identity boundary), `docs/11-Determinism.md`, and
`Documentation~/ARCHITECTURE.md`.

## Findings

### Ownership

No findings. Exactly one `Identity` type exists in the package
(`Runtime/Messaging/EventBus/Identity.cs:9`, `readonly struct Identity : IEquatable<Identity>`). Nothing in
`Runtime/Identity/` or `Editor/Identity/` defines a competing identity concept — `IdentityComponent`
(`Runtime/Identity/IdentityComponent.cs:14`) and `IdentityFieldAttribute`
(`Runtime/Identity/IdentityField.cs:8`) are both thin consumers of the single `Identity` struct, not
alternate identity types. (The folder-naming mismatch — the type isn't physically in `Runtime/Identity/`
— is a real issue, but it's a documentation/organization concern, not competing ownership; see
"Doc/Architecture Drift" below.)

### Correctness vs Documented Contract

No findings. The implementation matches `docs/00-AgentGuide.md`'s gotcha exactly:

- Implicit `string → Identity` conversion (`Runtime/Messaging/EventBus/Identity.cs:38`,
  `public static implicit operator Identity(string id) => new Identity(id);`) means
  `Identity x = null;` constructs `Identity(null)`, and `Equals` (`Identity.cs:28`,
  `string.Equals(_id, other._id, ...)`) makes it compare equal to `Identity.None`
  (`Identity.cs:11`, `= default`) since both have `_id == null`.
- `GetHashCode()` (`Identity.cs:31`) is exactly `_id.GetHashCode()` — the documented "string hash, not
  collision-free" — and is not overridden or shadowed anywhere else in the package.
- Searched the entire `com.aethernexus.foundationplatform` package for any call site that uses
  `Identity.GetHashCode()` as a lookup key, unique surrogate, or player number: none found.
  `IdentityDuplicationHandler` (`Editor/Identity/IdentityDuplicationHandler.cs:21,45,78,88,95`) keys its
  `Dictionary<string,int>` by `id.Value` (the real string), never by hash — correct usage per the
  doc's own warning.

### Redundancy/Simplification

- **Info** — Design-time ID generation format `$"e:{Guid.NewGuid():N}"` is duplicated verbatim in two
  places: `Runtime/Identity/IdentityComponent.cs:53` (`GenerateDesignTimeId()`) and
  `Editor/Identity/IdentityFieldDrawer.cs:104` (the "New" button handler). A future change to the prefix
  convention (e.g. adding a type tag) requires remembering to update both. Low impact — worth a shared
  helper (e.g. a `static string IdentityComponent.NewDesignTimeId()` or a small utility in the `Identity`
  namespace) if this area is touched again, not urgent enough to justify a standalone change today.
- **Info** — `UnityEditor.EditorUtility.SetDirty(this)` inside `#if UNITY_EDITOR` is repeated identically
  three times in `IdentityComponent.cs` (`AssignIdentity` line 45, `GenerateDesignTimeId` line 55,
  `ClearIdentity` line 66). Harmless duplication, not a correctness issue.
- No dead code, legacy/back-compat shims, or unnecessary abstraction layers found in either folder.
  `IdentityFieldAttribute` is a deliberately empty `PropertyAttribute` marker (standard Unity pattern, not
  redundant) and `IdentityDuplicationHandler` / `IdentityPrefabAssetPostprocessor` are each single-purpose
  with no overlapping responsibility.

### Doc/Architecture Drift

- **Warning** — `Documentation~/ARCHITECTURE.md:11-17` states: "Several core messaging / coroutine APIs
  are **global** (no namespace) for ergonomic call sites," and its Namespace map table row
  `*(global)* | Runtime/Messaging/EventBus/, Runtime/CoroutineX/, Runtime/Identity/ (value type) |
  EventBus, BaseGameEvent, Identity, CoroutineX, tween extensions` lists `Identity` (and `EventBus`,
  `BaseGameEvent`) as global/no-namespace types. This is factually wrong: `Identity`
  (`Runtime/Messaging/EventBus/Identity.cs:9`), `BaseGameEvent`
  (`Runtime/Messaging/EventBus/BaseGameEvent.cs:24`), and `EventBus`
  (`Runtime/Messaging/EventBus/EventBus.cs:8`, confirmed via namespace declaration) are all declared under
  `namespace AetherNexus.FoundationPlatform.Messaging`. Likewise every `Runtime/CoroutineX/*.cs` file is
  under `namespace AetherNexus.FoundationPlatform.CoroutineX` (checked all 8 files), not global. A reader
  relying on this table to skip a `using` statement will get `CS0246` and have to discover the real
  namespace by trial and error.
- **Warning** — The same table row also misattributes `Identity`'s location to the `Runtime/Identity/`
  folder ("value type" annotation). The `Identity` struct actually lives in
  `Runtime/Messaging/EventBus/Identity.cs`; `Runtime/Identity/` contains only `IdentityComponent` and
  `IdentityFieldAttribute` under a *third*, entirely different namespace —
  `AetherNexus.FoundationPlatform.Identity` — which does not appear anywhere in the Namespace map table
  (lines 15-30) at all. A reader trying to find where `Identity` is declared, or what namespace
  `IdentityComponent`/`IdentityFieldAttribute` live under, is misdirected by this table.
- No drift found in the dedicated `## Identity` section (`ARCHITECTURE.md:155-159`): its description of
  `Identity` (string-based entity/channel id, `Global`/`None` sentinels, implicit `string → Identity`
  conversion) and of `IdentityComponent`/`IdentityDuplicationHandler` ("Duplicate detection is design-time
  only ... there is no runtime registry, so don't rely on uniqueness being enforced at runtime") is
  accurate and matches the code read in this audit.

### Codebase Gotchas (docs/00 §3)

- **No findings** — struct instance-field-initializer / explicit-ctor rules (C# 9, `CS8773`/`CS0171`).
  `Identity` (`Runtime/Messaging/EventBus/Identity.cs`) has exactly one instance field (`_id`, line 18,
  no initializer) and exactly one explicit constructor (line 23-26) which assigns `_id` unconditionally.
  Fully compliant — flagged here because the audited folders are the ones that construct and consume
  `Identity`, so this was checked even though the struct's file is physically elsewhere.
- **N/A** — `Debug` namespace collision (`GameEngineCore.*`): none of the audited files, nor `Identity.cs`,
  are under a `GameEngineCore.*` namespace; `IdentityComponent.cs:25` correctly resolves unqualified
  `Debug.LogError` to `UnityEngine.Debug` via `using UnityEngine;`.
- **No findings** — `??` on `UnityEngine.Object`. Checked all editor drawer/handler code
  (`IdentityComponentEditor.cs`, `IdentityFieldDrawer.cs`, `IdentityGizmoDrawer.cs`,
  `IdentityDuplicationHandler.cs`): all Unity-object checks use explicit `== null` / `!= null` (e.g.
  `IdentityGizmoDrawer.cs:25`, `if (go == null) continue;`; `IdentityDuplicationHandler.cs:63`,
  `.Where(c => c != null && ...)`). No `??` operator appears in either folder at all.

## Fixes

No code changes made (scope of this audit is read-only; `AUDIT.md` is the only file written). Recommended
follow-ups, in priority order:

1. Correct `Documentation~/ARCHITECTURE.md`'s Namespace map (lines 11-17): remove `Identity`,
   `BaseGameEvent`, and `EventBus` from the "global (no namespace)" claim/row — they are all
   `AetherNexus.FoundationPlatform.Messaging`. Same for `Runtime/CoroutineX/*` →
   `AetherNexus.FoundationPlatform.CoroutineX`. Add a row for `AetherNexus.FoundationPlatform.Identity` →
   `Runtime/Identity/`, `Editor/Identity/` (`IdentityComponent`, `IdentityFieldAttribute` + drawer/handler),
   and correct `Identity`'s own folder reference to `Runtime/Messaging/EventBus/`.
2. Optional, low-priority: extract the `$"e:{Guid.NewGuid():N}"` design-time-ID format into one shared
   helper used by both `IdentityComponent.GenerateDesignTimeId()` and `IdentityFieldDrawer`'s "New" button,
   so the convention only needs to change in one place.
3. Judgment call, not filed as a defect: whether `Identity.cs` should physically move into
   `Runtime/Identity/` to match its folder name is a design/ownership decision (it currently sits with
   `EventBus`/`BaseGameEvent` because its original/primary use is event-channel routing, per
   `ARCHITECTURE.md:60-69`) — flagging for the maintainer to decide, not doing it here per the "no file
   moves without confirmation" rule.

## Cross-references

- `docs/00-AgentGuide.md` §3, `Identity` gotcha (readonly struct, implicit string operator, hash-is-not-a-key warning).
- `docs/00-AgentGuide.md` §3, C# 9 struct rules (`CS8773`/`CS0171`).
- `docs/10-Integrations.md`, "Player identity boundary" (generic-owner vs character-scoped identity — enforced by higher-level packages that consume this primitive; not itself defined or violated in `FoundationPlatform`).
- `docs/11-Determinism.md` — n/a to this folder; `Identity` carries no randomness or timing.
- `Documentation~/ARCHITECTURE.md:11-17` (Namespace map — inaccurate, see Doc/Architecture Drift).
- `Documentation~/ARCHITECTURE.md:60-69` (Event Bus section, `Identity` role — accurate).
- `Documentation~/ARCHITECTURE.md:155-159` (dedicated `## Identity` section — accurate).
- `Runtime/Messaging/EventBus/Identity.cs`, `IIdentity.cs`, `BaseGameEvent.cs` — the type this folder's files consume (read for context, outside assigned scope, not modified).
