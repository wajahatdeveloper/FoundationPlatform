# Core Utilities (Attributes, Behaviours, Extensions, Patterns, SupportTypes, Menus, Gizmos + Editor counterparts) — Architecture Audit (Re-audit)

## Context

This is a re-audit of the original `AUDIT-CoreUtilities.md` pass, done after fixes were applied. Scope is unchanged from the original audit (see below). The re-audit was scoped by:

1. Reading the original findings in full.
2. Running `git diff d714482 HEAD -- Runtime/Extensions/ Runtime/Gizmos/ Runtime/Menus/ Editor/Menus/ Editor/Gizmos/` (1287-line diff, read in full) — this is almost entirely the mechanical "optional-parameter → explicit two-overload" fix pattern (`Foo(x, y = default)` becomes `Foo(x, y)` + a new `Foo(x)` one-liner forwarder), applied consistently across `AnimatorExtensions.cs`, `FadeExtensions.cs`, `AudioSourceExtensions.cs`, `IEnumerableExtensions.cs`, `IListExtensions.cs`, `ColorExtensions.cs`, `ColorX.cs`, `GameObjectCollectionExtensions.cs`, `GameObjectHierarchyExtensions.cs`, `UnityTransformExtensions.cs`, `MathX.cs`, `CameraExtensions.cs`, `Texture2DExtensions.cs`, `IPersistentDataAdapter.cs`/`PersistentDataHandler.cs`/`UnityJsonPersistentDataAdapter.cs`, `TimeX.cs`, `RectTransformExtensions.cs`, `InvokeExtension.cs`, `StreamExtensions.cs`, `GizmosExtensions.cs`. Spot-checked ~10 of these conversions for correctness (sentinel-default forwarding, e.g. `CameraExtensions.LookAt`'s `up == default` check, `GizmosExtensions`'s `rotation.Equals(default(Quaternion))` check) — all forward to the exact original default value, no behavior change. Two small unrelated changes also ride in this diff: `MenuPaths.cs`/`EditorAssetFolders.cs` rename "Domain" → "Content Area" in a couple of menu labels/doc-comments and drop two now-dead menu entries (`PackageAudit`, `PackagePopulate`) — cosmetic, outside this audit's findings, not a regression.
3. **Critically**: the four confirmed ambiguous-overload pairs and the determinism/doc-drift/reflection/dead-field/gizmos-editor-location findings all live in files that were **not touched by the `d714482..HEAD` diff at all** (`CollectionExtensions.cs`, `IDictionaryExtensions.cs`, `GameObjectUtilityExtensions.cs`, `UIExtensions.cs`, `MathExtensions.cs`, `ReflectionExtensions.cs`, `AreaSpawner.cs`, `CooldownTracker.cs`, `Documentation~/ARCHITECTURE.md`'s relevant lines). Checking `git show d714482:<path>` for each confirms **the substantive fixes were already present at the `d714482` baseline** — they landed in an earlier commit (`be05b9a "Updates"` for the Math/Architecture doc fixes) than the mechanical optional-parameter pass this diff was scoped to. So: the mechanical pass and the substantive fixes are two different, non-overlapping commits; both are reflected in current `HEAD`, which is what this document audits.
4. Re-read every file involved in each of the four ambiguous-overload pairs, `AreaSpawner.cs`, `ReflectionExtensions.cs`, `CooldownTracker.cs`, `ColliderGizmo.cs`/`Editor/Gizmos/`, `PlayerPrefsX.cs`, and the relevant sections of `Documentation~/ARCHITECTURE.md` at current `HEAD` to confirm the fixes are real, correct, and still standing (not reverted or drifted since).

**Original scope recap (unchanged):**

**Runtime**
- `Runtime/Attributes/` (7 files), `Runtime/Behaviours/` (9 files), `Runtime/Extensions/` (79 files), `Runtime/Patterns/` (4 files), `Runtime/SupportTypes/` (4 files), `Runtime/Menus/` (2 files), `Runtime/Gizmos/` (4 files).

**Editor**
- `Editor/Menus/` (2), `Editor/Gizmos/` (2 → now 3, see `SetLossyScale`/gizmos-editor fixes below), `Editor/Utilities/` (14), `Editor/EditorEnhancerX/` (23), `Editor/AssetImport/` (7), `Editor/Dialogs/` (2), `Editor/StaleComponentGuard/` (10), `Editor/Drawers/` (3).

## Findings

Each original finding is reproduced with its **current status**. Findings not listed here (Designer Surface Priority positives, Codebase Gotchas "no findings", Data/Controller/View Boundary, Execution Spine "no pipeline violations") are unchanged and not re-verified line-by-line in this pass since nothing in the diff touched those subsystems.

### Within-Group Redundancy

**[RESOLVED]** The four confirmed ambiguous-overload (`CS0121`) pairs are all resolved at current `HEAD`. Each pair now has exactly one implementation, with the other file's copy replaced by a one-line `// Note: ... kept there ...` comment pointing at the surviving location:

- **`Shuffle<T>(this IList<T>)`**: `CollectionExtensions.cs:249-251` now only carries a note ("Shuffle<T>(this IList<T>) lives in IListExtensions.cs — kept there as the single source... to avoid ambiguous... identical-signature Shuffle overloads"); the live implementation is `IListExtensions.cs:61` (`Shuffle<T>(IList<T>)`, PRESENTATION-ONLY doc comment) plus a new deterministic twin `IListExtensions.cs:76` (`Shuffle<T>(IList<T>, int seed)`). Single source, deterministic path now discoverable next to the presentation one.
- **`GetOrAdd<TKey,TValue>`**: `CollectionExtensions.cs:194-195` now only carries a note pointing at `IDictionaryExtensions.cs`; `CollectionExtensions.cs:203` keeps a *different*, non-colliding 3-type-parameter overload (`GetOrAdd<TKey,TValue,TArg>`). The two-parameter and `Func`-based overloads live solely in `IDictionaryExtensions.cs:306` and `:329`.
- **`SetLossyScale(this Transform, Vector3)`**: fully reconciled, not just deduplicated. `GameObjectUtilityExtensions.cs:447-453` is now the **sole** implementation (`source.localScale = source.lossyScale.Pow(-1).ScaleBy(targetLossyScale).ScaleBy(source.localScale)`); `UnityTransformExtensions.cs:1297` carries only a "kept there" note. The original audit's "worse than the others" concern (two implementations computing different math) is moot — there is only one body left, so no silent-behavior-change risk remains.
- **`SetWidth`/`SetHeight(this RectTransform, float)`**: `RectTransformExtensions.cs:1097` (`SetWidth`) and `:1102` (`SetHeight`) are the sole implementations; `UIExtensions.cs:222` carries only a "kept there" note.

None of these four fixes are part of the `d714482..HEAD` diff — `git log --oneline d714482..HEAD` for `CollectionExtensions.cs`/`IDictionaryExtensions.cs`/`GameObjectUtilityExtensions.cs`/`UIExtensions.cs` is empty, and `git show d714482:<file>` already contains the "kept there" notes, so this resolution landed in a commit prior to the baseline used for this re-audit's diff. Verified current, not reverted.

**[OPEN — unchanged]** "Nearest/closest object" is still implemented three separate times with three different names/shapes: `TargetingExtensions.FindNearestUnit*` family, `GameObjectUtilityExtensions.FindNearestByTag/FindNearestByType/FindNearests`, and `GetClosestPoint`/`FindClosest2D`/`FindClosest` in `Vector2Extensions.cs`/`Vector3Extensions.cs`. Not touched by any commit in range; not part of the numbered "Fixes" priority list from the original audit, so no regression — just still open.

**[OPEN — unchanged]** "Get-or-add component" is still implemented four times in `GameObjectComponentExtensions.cs` (`AddOrGetComponent<T>`, `.GetComponentOrAdd<T>`, `.GetOrAddComponent<T>(GameObject)`, `.GetOrAddComponent<T>(Component)`). Confirmed still present via grep; unchanged.

**[OPEN — unchanged]** "Set layer recursively" is still implemented three times across `GameObjectUtilityExtensions.cs` (`SetLayerRecursively`, `MoveToLayer`→`InternalMoveToLayer`) and `UnityTransformExtensions.cs` (`SetLayerRecursive`→`SetLayerInternal`). Confirmed still present; unchanged.

**[OPEN — unchanged]** `TimeSpanExtensions.ToFormattedString` and `TimeX.FormatDuration` still both format a `TimeSpan` with the same default format string. `TimeX.cs` did change in this diff (+17, mechanical optional-param split of `FormatDuration`/`GetCurrentTimeString`/`FloatToTimeString`), but the underlying duplication with `TimeSpanExtensions.cs` was not addressed — not part of the fix list.

**[RESOLVED]** `Runtime/Gizmos/ColliderGizmo.cs` no longer embeds its `[CustomEditor]` class inline. `ColliderGizmoEditor` now lives at `Editor/Gizmos/ColliderGizmoEditor.cs` (confirmed via directory listing: `Editor/Gizmos/` now contains `GizmosEditor.cs`, `GizmosHandleTextEditor.cs`, and `ColliderGizmoEditor.cs`), and grepping `Runtime/Gizmos/ColliderGizmo.cs` for `CustomEditor`/`ColliderGizmoEditor` returns no matches. Matches the sibling components' convention now.

**[RESOLVED]** `Extensions/Storage/PlayerPrefsX.cs` now carries a class-level doc comment: *"Legacy binary-array PlayerPrefs codec. Kept for existing callers/back-compat; new code should use `PersistentDataHandler` (JSON-backed) instead."* (`PlayerPrefsX.cs:8-11`). The "which is preferred" ambiguity from the original audit is resolved.

**[OPEN — unchanged]** Three unrelated "editor-only scene annotation" MonoBehaviours (`Comment`, `GizmosHandleText`, `InspectorSeparator`) remain scattered across three folders with no shared convention. Not part of the fix list; still an `[Info]`-level organizational nit.

### Ownership

Covered under Within-Group Redundancy above — all four confirmed duplicate-signature pairs are now resolved; the three "same operation, several names" cases remain open (unchanged, not previously prioritized for a fix).

**[Unchanged — no finding]** `ComponentExtensions.GetComponentInSelfOrParents<T>`/`GetComponentInSelfOrChildren<T>` still present exactly where docs say, at `Runtime/Extensions/Core/ComponentExtensions.cs:15`/`:26`. No drift, as before.

### Redundancy/Simplification

**[OPEN — unchanged, expected]** `Runtime/Gizmos/GizmosComponent.cs`'s string-typed `type` field / giant switch remains as-is — flagged for awareness only in the original audit (vendored code, not to be hand-edited per `docs/00-AgentGuide.md` §4). No change expected or observed.

**[RESOLVED]** `Behaviours/CooldownTracker.cs`'s dead field `_pauseOnEmptyWFS` has been deleted. Grepping the file for `_pauseOnEmptyWFS` returns no matches.

### Determinism

**[RESOLVED]** `CollectionExtensions.GetRandom<T>(T[])` (line 60) and `GetRandom<T>(IList<T>)` (line 71) now each carry a `PRESENTATION-ONLY: uses UnityEngine.Random. Use an IRandomProvider-based path (e.g. RandomX) in simulation code.` doc comment. `Shuffle<T>(IList<T>)` (now solely in `IListExtensions.cs:56-61`) carries the equivalent doc comment and has a deterministic `Shuffle<T>(IList<T>, int seed)` twin at `IListExtensions.cs:76`. This matches the established pattern in `IEnumerableExtensions.cs`/`RandomX.cs` that the original audit called out as the standard to follow.

**[RESOLVED]** `MathExtensions.GetDirectionFromSpread(Quaternion, float)` (`Runtime/Extensions/Math/MathExtensions.cs:298`) now carries the doc comment: *"PRESENTATION-ONLY: uses UnityEngine.Random. Use an IRandomProvider-based path (e.g. MathX.RollADice/Chance or a custom deterministic spread) in simulation code such as weapon-spread/aim-cone calculations."* (lines 295-297). `Documentation~/ARCHITECTURE.md:245` was also updated to name both fixed spots explicitly: *"`CollectionExtensions.GetRandom`/`Shuffle` ... and `MathExtensions.GetDirectionFromSpread` are explicitly documented as PRESENTATION-ONLY ... use the `IRandomProvider`-based path above for anything simulation-affecting."* Both the code disclaimers and the doc cross-reference are in place. `MathX.cs` itself did change in the `d714482..HEAD` diff (+21, mechanical `Detection2D`/`Detection3DAll` optional-param split) but the `GetDirectionFromSpread` fix lives in the sibling file `MathExtensions.cs`, which predates this diff range (confirmed present at `d714482`).

**[RESOLVED]** `Behaviours/AreaSpawner.cs`'s `GetRandomSpawnPosition()` no longer has a "loud fallback." It still logs the same `DebugX...Error(...)` when deterministic startup is active with no `IRandomProvider` assigned (`AreaSpawner.cs:324-328`), but now **throws `InvalidOperationException`** immediately after (`AreaSpawner.cs:329-330`: `"AreaSpawner on {gameObject.name}: deterministic startup is in use but no IRandomProvider is assigned."`) instead of falling through to `UnityEngine.Random.Range`. This is now genuine fail-fast, matching `docs/00-AgentGuide.md`'s rule. The class doc comment was also updated to state it "Supports both deterministic (via IRandomProvider) and non-deterministic (Unity Random) spawning" (`AreaSpawner.cs:16`), though the original audit's separate, lower-priority `[Info]` suggestion — a one-line doc comment clarifying `AreaSpawner` is a scene-setup primitive and not a combat/enemy-spawn action — was not added; not part of the numbered fix list, so this is a pre-existing minor gap, not a regression.

**[OPEN — unchanged]** `Extensions/Utilities/InvokeExtension.cs`'s `MonoBehaviour.Invoke(Action, float, bool realtime)` still has no simulation/presentation naming distinction. The file did change in the `d714482..HEAD` diff (mechanical split into `Invoke(me, action, time, realtime)` + `Invoke(me, action, time)` forwarding to `realtime: false`), but this was purely the optional-parameter fix, not a determinism annotation; not part of the fix list.

**[OPEN — unchanged]** `Behaviours/CooldownTracker.cs` still drives timing off `UnityEngine.Time.deltaTime`/`Time.time` with no presentation-only disclaimer. Unchanged; not part of the fix list (only the dead-field cleanup was).

### Doc/Architecture Drift

**[RESOLVED]** `Documentation~/ARCHITECTURE.md:157` now correctly describes `MaybeMonad` as *"non-generic static class of LINQ-chain extension methods (`With`, `Return`, `If`, `Unless`, `Do`, `IfNotNull`) — not a generic `Some`/`None` optional-value wrapper."* Matches the actual type.

**[RESOLVED]** `Documentation~/ARCHITECTURE.md:158` now correctly describes `CustomState` as *"a bare `MonoBehaviour` wrapping `Dictionary<string,string> keyValuePairs`, used by `MonoBehaviourExtensions.RunOnce`/`RunOncePersistent` as a per-object 'have I already fired this once' flag store — no state-machine semantics."* Matches the actual type.

**[RESOLVED]** `Documentation~/ARCHITECTURE.md`'s Editor-tooling table now includes all three previously-missing entries (lines 286-288): `StaleComponentGuard` (`Editor/StaleComponentGuard/`), `EditorEnhancerX` (`Editor/EditorEnhancerX/`), and `AssetImport` (`Editor/AssetImport/`), each with a one-line description matching the subsystem's actual behavior (e.g. StaleComponentGuard's description correctly calls out the Hierarchy decorator + inspector badge + Project Settings panel + last-resort sweep window pattern verified in the original audit).

**[RESOLVED]** `Documentation~/ARCHITECTURE.md:140` now reads *"logs **Info** (not Error) when a second copy is found..."* — the wording drift for `SingletonBehaviour<T>` is fixed.

None of these four `ARCHITECTURE.md` fixes are part of the `d714482..HEAD` diff (`git diff d714482 HEAD -- Documentation~/ARCHITECTURE.md` only shows two unrelated line changes, both in the ProjectWindowX/HierarchyX sections about `SettingsExtras.Register`, nothing to do with these findings) — confirmed present already at the `d714482` baseline via `git show d714482:Documentation~/ARCHITECTURE.md`.

**[Unchanged — cross-reference, not this audit's to fix]** The doc's namespace map still calls the inspector engine "Framework Inspector"/`FrameworkInspector`, while in-scope code (`AreaSpawner.cs`, `MenuPaths.cs`, `StaleComponentInspectorBadge.cs`, `CommentEditor.cs`) references `AetherNexus.FoundationPlatform.AetherInspector`. Not re-checked in depth this pass; still flagged for whichever audit owns AetherInspector.

### Codebase Gotchas (docs/00 §3)

**[RESOLVED]** `Runtime/Extensions/Reflection/ReflectionExtensions.cs` (`HasMethod`/`HasField`/`HasProperty`) is now wrapped in `#if UNITY_EDITOR` (lines 5 and 38), with a header comment explaining the rationale: *"Editor-only: no runtime callers found in this project, and the project's 'no runtime reflection outside editor tools' rule bans compiling GetMethod/GetField/GetProperty-based reflection into player builds. Guarded rather than moved to an Editor asmdef so any existing `using AetherNexus.FoundationPlatform.Extensions;` call sites in editor-only code keep resolving."* This is no longer reachable from a player build. Not part of the `d714482..HEAD` diff; confirmed present at `d714482` baseline already.

**[RESOLVED — mechanical pass, this audit's primary diff]** The project-wide "no optional parameters" violation across `Runtime/Extensions/*` is what the entire `d714482..HEAD` diff addresses. Every touched method (`RigidbodyExtensions`-style `param = default` signatures) in the file list given for this re-audit has been converted to an explicit required-parameter overload plus a same-named zero-extra-parameter forwarding overload carrying a one-line doc comment stating the default it forwards to (e.g. `AnimatorExtensions.GetCrossFadeProgress(Animator)` → forwards to `layer: 0`; `CameraExtensions.LookAt(Camera, Vector3)` → forwards to `up: default`/`Vector3.up`). Spot-checked ~10 conversions across `AnimatorExtensions.cs`, `FadeExtensions.cs`, `CameraExtensions.cs`, `MathX.cs`, `GizmosExtensions.cs`, `RectTransformExtensions.cs`, `TimeX.cs`, `StreamExtensions.cs` — every forwarder passes the exact literal the original default parameter held; no behavior changes, no new ambiguous-signature collisions introduced (verified no new pair of identical-signature methods was created across files by this mechanical split — the forwarder's signature is unique per file since it just drops the trailing optional parameter(s), and no sibling file happens to already define that shorter signature). This finding was previously scoped as "large, separate refactor, not a spot-fix" — it appears to now be substantially done for the files in this diff's scope; whether *all* 79 files under `Runtime/Extensions/` are done was not re-verified exhaustively (only the file list given for this re-audit was diffed).

## Fixes

Status of the original seven recommended fixes, in the same priority order:

1. **DONE.** All four ambiguous-overload pairs (`Shuffle`, `GetOrAdd`, `SetLossyScale`, `SetWidth`/`SetHeight`) resolved — one implementation kept per pair, the other side replaced with a pointer comment. `SetLossyScale`'s math-disagreement risk is moot since only one body remains (the `lossyScale.Pow(-1).ScaleBy(...)` version).
2. **DONE.** `CollectionExtensions.GetRandom`/`Shuffle` and `MathExtensions.GetDirectionFromSpread` all carry `PRESENTATION-ONLY` doc comments now; `Shuffle` additionally gained a deterministic seeded overload.
3. **DONE.** `ARCHITECTURE.md`'s `MaybeMonad<T>`/`CustomState` descriptions corrected, `StaleComponentGuard`/`EditorEnhancerX`/`AssetImport` added to the Editor tooling table, `SingletonBehaviour` wording fixed.
4. **DONE.** `PlayerPrefsX` now carries an explicit "legacy, use `PersistentDataHandler` for new code" doc comment.
5. **DONE.** `ColliderGizmoEditor` moved out of `Runtime/Gizmos/ColliderGizmo.cs` into `Editor/Gizmos/ColliderGizmoEditor.cs`.
6. **DONE.** Dead `_pauseOnEmptyWFS` field deleted from `CooldownTracker`.
7. **DONE.** `ReflectionExtensions.cs` guarded behind `#if UNITY_EDITOR`.

All seven of the original audit's numbered recommendations are resolved. Remaining open items are all items that were **not** in the numbered fix list (three "same operation, several names" redundancies — nearest-object, get-or-add-component, set-layer-recursively; the `TimeSpanExtensions`/`TimeX` duplicate formatter; the three scattered scene-annotation MonoBehaviours; `GizmosComponent`'s vendored string-switch design; `InvokeExtension`/`CooldownTracker`'s lack of presentation-only disclaimers; the `AreaSpawner` scene-setup-primitive doc-comment suggestion; the `AetherInspector` naming cross-reference). These remain **[Info]/[Warning]**-level, not blocking, and were correctly left alone by this fix pass since they were explicitly deprioritized in the original audit ("noted as one consolidated item... since fixing it would be a large, separate refactor" / "Worth a one-line doc comment" / not numbered).

No new findings were introduced by the `d714482..HEAD` diff itself — it is a clean, correctly-applied mechanical pass plus two small unrelated cosmetic renames (`MenuPaths.cs`/`EditorAssetFolders.cs`, "Domain" → "Content Area" wording and removal of two dead menu entries).

## Cross-references

- **AetherInspector audit**: unchanged from the original — `ARCHITECTURE.md` still calls the inspector engine "Framework Inspector," while in-scope code references `AetherNexus.FoundationPlatform.AetherInspector`. Still needs reconciling from whichever audit owns AetherInspector.
- **`Editor/Drawers/`**: unchanged — reviewed in full in the original pass, not re-verified line-by-line this time since untouched by the diff.
- **GameEngineCore audit**: `AreaSpawner`'s deterministic-spawn path is now fail-fast (throws instead of silently falling back), which strengthens the guarantee that `SceneInitializationCoordinator`-driven scene setup either has a real `IRandomProvider` or aborts loudly — worth re-confirming from the GameEngineCore side that callers handle the new `InvalidOperationException` rather than relying on the old silent-fallback behavior.
- **Storage/persistence**: `PersistentDataHandler`/`UnityJsonPersistentDataAdapter` is now explicitly documented (via `PlayerPrefsX`'s doc comment) as the preferred path for new code, with `PlayerPrefsX` explicitly legacy. Still a distinct mechanism from `IStateContributor` (`GameEngineCore.Snapshot`) — that distinction from the original audit still holds and is unaffected by this fix.
