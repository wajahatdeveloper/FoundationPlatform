# Core Utilities (Attributes, Behaviours, Extensions, Patterns, SupportTypes, Menus, Gizmos + Editor counterparts) — Architecture Audit

## Context

Every `.cs` file in the assigned scope was read in full. Coverage:

**Runtime**
- `Runtime/Attributes/` (7 files) — `[Layer]`, `[Tag]`, `[TooltipIcon]`, `RunFirst/RunLast/RunAfter/RunBefore` execution-order attributes.
- `Runtime/Behaviours/` (9 files) — small reusable MonoBehaviours: `AreaSpawner`, `CameraLookConstraint`, `CooldownTracker`, `Drag2DHandler`/`Drop2DHandler`, `IRandomProvider`/`IRandomStreamSource`, `InspectorSeparator`, `SceneSpawnReadyGate`.
- `Runtime/Extensions/` (79 files across Animations, Audio, Bitwise, Collections, Color, Core, Math, Physics, Random, Reflection, Rendering, Scene, Storage, Time, UI, Utilities, Validation) — the ~180-method extension-method library.
- `Runtime/Patterns/` (4 files) — `FragmentData<TConfig,TPayload>`, `Observable<T>`, `ObservableList<T>`, `SingletonBehaviour<T>`/`PersistentSingletonBehaviour<T>`/`Singleton<T>`.
- `Runtime/SupportTypes/` (4 files) — `CustomState`, `HSL`, `HSV`, `MaybeMonad`.
- `Runtime/Menus/` (2 files) — `MenuPaths`, `MenuPriorities` (editor-only, `#if UNITY_EDITOR`, compiled into Runtime for asmdef reasons).
- `Runtime/Gizmos/` (4 files) — `ColliderGizmo`, `GizmosComponent`, `GizmosExtensions`, `GizmosHandleText`.

**Editor**
- `Editor/Menus/` (2), `Editor/Gizmos/` (2), `Editor/Utilities/` (14), `Editor/EditorEnhancerX/` (23: settings + Infra + Features), `Editor/AssetImport/` (7), `Editor/Dialogs/` (2), `Editor/StaleComponentGuard/` (10).
- `Editor/Drawers/` (3): `LayerDrawer`, `TagDrawer`, `TooltipIconDrawer` — these drawer types belong to `Runtime/Attributes/` (`LayerAttribute`, `TagAttribute`, `TooltipIconAttribute`), which is squarely in this audit's scope, not AetherInspector's. Reviewed in full (not a cross-reference skip).

Also read: `docs/00-AgentGuide.md`, `docs/01-CorePrinciples.md`, `docs/02-Libraries.md`, `docs/09-EditorHub.md`, `docs/13-AuthoringStandards.md`, and `Documentation~/ARCHITECTURE.md`.

## Findings

### Within-Group Redundancy

**[Error]** Four pairs of extension methods with **identical name + identical parameter types** live in different static classes inside the same namespace (`AetherNexus.FoundationPlatform.Extensions`) — a latent `CS0121` "ambiguous call" compile error waiting for the first caller that omits the disambiguating cast. None are currently triggered project-wide (verified by grep), but they are landmines:
- `Shuffle<T>(this IList<T>)` — `Runtime/Extensions/Collections/CollectionExtensions.cs:274` (returns `IList<T>`) vs `Runtime/Extensions/Collections/IListExtensions.cs:59` (returns `void`). Any future `someList.Shuffle()` call fails to compile.
- `GetOrAdd<TKey,TValue>(this IDictionary<TKey,TValue>, TKey, TValue)` and its `Func<TKey,TValue>` overload — `CollectionExtensions.cs:197` and `:211` vs `Runtime/Extensions/Collections/IDictionaryExtensions.cs:306` and `:329`. Currently masked only because the two real call sites (`GameplayTagRegistry.cs`, `Wallet.cs`) use `ConcurrentDictionary`, whose own instance method wins over both extensions. A plain `Dictionary<K,V>` caller would not be so lucky.
- `SetLossyScale(this Transform, Vector3)` — `Runtime/Extensions/Core/GameObjectUtilityExtensions.cs:443` vs `Runtime/Extensions/Core/UnityTransformExtensions.cs:1277`. Worse than the others: even setting aside the ambiguity, the two bodies compute **different math** (one does `lossyScale.Pow(-1).ScaleBy(...)`, the other resets to `Vector3.one` then divides component-wise) — whichever one a future single-file fix "resolves" to would silently change behavior for the other's callers.
- `SetWidth`/`SetHeight(this RectTransform, float)` — `Runtime/Extensions/UI/RectTransformExtensions.cs:1061`/`1066` vs `Runtime/Extensions/UI/UIExtensions.cs:227`/`234`.

**[Warning]** "Nearest/closest object" is implemented three separate times with three different names/shapes: `TargetingExtensions.FindNearestUnit*` family (`Runtime/Extensions/Utilities/TargetingExtensions.cs`, ~8 methods), `GameObjectUtilityExtensions.FindNearestByTag/FindNearestByType/FindNearests` (`Runtime/Extensions/Core/GameObjectUtilityExtensions.cs:358-438`), and `GetClosestPoint`/`FindClosest2D`/`FindClosest` scattered across `Vector2Extensions.cs`, `Vector3Extensions.cs` (class `VectorExtensions`). No shared helper, no cross-reference between them.

**[Warning]** "Get-or-add component" is implemented four times in one file: `GameObjectComponentExtensions.AddOrGetComponent<T>(GameObject)` (`Runtime/Extensions/Core/GameObjectComponentExtensions.cs:89`), `.GetComponentOrAdd<T>(GameObject)` (line 100, a one-line alias for the former), `.GetOrAddComponent<T>(GameObject)` (line 375), `.GetOrAddComponent<T>(Component)` (line 385) — four public names for the same operation in the same file.

**[Warning]** "Set layer recursively on a hierarchy" is implemented three times: `GameObjectUtilityExtensions.SetLayerRecursively` (`Runtime/Extensions/Core/GameObjectUtilityExtensions.cs:134`) and `.MoveToLayer`→`InternalMoveToLayer` (lines 285-300, same file), plus `UnityTransformExtensions.SetLayerRecursive`→`SetLayerInternal` (`Runtime/Extensions/Core/UnityTransformExtensions.cs:22-31`, a different file).

**[Warning]** `TimeSpanExtensions.ToFormattedString(TimeSpan, string format = "hh\\:mm\\:ss")` (`Runtime/Extensions/Time/TimeSpanExtensions.cs:32`) and `TimeX.FormatDuration(TimeSpan, string format = "hh\\:mm\\:ss")` (`Runtime/Extensions/Time/TimeX.cs:139`) do the same thing (`duration.ToString(format)`) with the same default, in the two files that share the `Time/` folder.

**[Warning]** `Runtime/Gizmos/ColliderGizmo.cs` embeds its own `[CustomEditor]` class (`ColliderGizmoEditor`, lines 450-579) inline in the Runtime file behind `#if UNITY_EDITOR`, while its sibling components in the exact same folder — `GizmosComponent` and `GizmosHandleText` — correctly split their custom editors out into `Editor/Gizmos/GizmosEditor.cs` and `Editor/Gizmos/GizmosHandleTextEditor.cs`. Same folder pair, two different conventions for where editor code lives.

**[Info]** `Extensions/Storage/PlayerPrefsX.cs` (a hand-rolled binary-array PlayerPrefs codec: manual endianness handling, byte-packed float/int/Vector2/Vector3/Quaternion/Color arrays) sits in the same folder as `PersistentDataHandler.cs` + `UnityJsonPersistentDataAdapter.cs` (a newer JSON-backed generic key/value store). Both solve "persist arbitrary keyed data," from clearly different eras, with no note on which is preferred going forward.

**[Info]** Three unrelated "editor-only scene annotation" MonoBehaviours are scattered across three different folders with no shared convention: `Comment` (`Runtime/Extensions/Utilities/Comment.cs`), `GizmosHandleText` (`Runtime/Gizmos/`), `InspectorSeparator` (`Runtime/Behaviours/`). Not wrong, but illustrates the grab-bag nature of this group — a folder reorganization pass (e.g. a shared `EditorOnly/` runtime folder) would reduce the "where do I put a new one of these" ambiguity.

### Execution Spine

No pipeline violations found — this is a Libraries-tier package and none of its Behaviours write authoritative simulation state through a side door.

**[Info]** `Behaviours/AreaSpawner.cs` calls `Instantiate(unitPrefab, ...)` directly (`SpawnUnits`, line ~193) rather than going through any manager/action. This is architecturally fine *if* `AreaSpawner` is purely a scene-setup/spawn-point utility invoked before or outside live simulation (its `SceneSpawnReadyGate` integration suggests exactly that), but the file itself carries no annotation saying so, and nothing in the type prevents a designer from wiring it to fire mid-match. Worth a one-line doc comment clarifying it's a scene-setup primitive, not a combat/enemy-spawn action.

### Data/Controller/View Boundary

No violations found. Library code does not read/write any product-owned Data-tier state.

**[Info]** `Extensions/Storage/PersistentDataHandler.cs` + `UnityJsonPersistentDataAdapter.cs` implement a generic device-local JSON key/value store, which is a *different* mechanism from the documented gameplay-state persistence path (`IStateContributor` in `GameEngineCore.Snapshot`, per `docs/00-AgentGuide.md` §3). Not a violation — different scope (device settings/local prefs vs. gameplay snapshots) — but worth a cross-reference note so a future consumer doesn't reach for `PersistentDataHandler` to persist simulation facts instead of `IStateContributor`.

### Ownership

Covered under Within-Group Redundancy above (four confirmed duplicate-signature pairs; three cases of "same operation, several names").

**[Info]** `ComponentExtensions.GetComponentInSelfOrParents<T>`/`GetComponentInSelfOrChildren<T>` are confirmed present at `Runtime/Extensions/Core/ComponentExtensions.cs:15` and `:26`, exactly where `docs/00-AgentGuide.md` and `AGENTS.md` say they live. **No drift** — this is the one place in the audit where the docs' orientation pointer checked out perfectly.

### Designer Surface Priority

**[Positive — no finding]** `Editor/EditorEnhancerX/` (23 files) is a well-built Scene View / Hierarchy power-tool layer: Scene View overlays (`SceneViewHub`), the native `EditorTool` rail (`DuplicateArrayTool`, `PivotRotationTool`, `PivotMoveTool`), a `[MainToolbarElement]` timescale slider, and a `SettingsProvider` under **Project Settings ▸ EditorEnhancerX** — no bespoke always-open heavy window. This matches `docs/09-EditorHub.md`'s surface-priority order well (Scene View overlays before Editor windows). Reflection into internal Unity APIs (`GlobalKeyCapture`, `AddComponentShortcut`, `EditorX.FoldInHierarchy`) is consistently self-disabling (`Available` flag + try/catch) rather than throwing, and is editor-only — compliant with "no runtime reflection outside editor tools."

**[Positive — no finding]** `Editor/StaleComponentGuard/` (10 files) — **answering the task's explicit question**: it detects components whose serialized YAML still carries top-level keys ("fields") the current script no longer declares (renamed/removed without `[FormerlySerializedAs]`). This is a **real problem, not a workaround for something that shouldn't need guarding**: Unity's YAML serializer silently keeps orphaned keys on disk after such a rename/removal, and `SerializedObject` cannot see them once a valid script is present — the only way to detect the drift is exactly what this tool does (reflect the current field set, diff it against a raw YAML read). Its primary surfaces are the Hierarchy row decorator + inspector header badge + a `HierarchyX` **Project Settings** panel (`StaleComponentsSettingsGui`); the one `EditorWindow` (`StaleComponentWindow`) is explicitly reserved for "everything on disk you haven't opened" — exactly the "last resort, project-wide sweep" carve-out `docs/09-EditorHub.md` describes. The destructive fix ("Strip") is always behind a confirm dialog. No violations.

**[Info]** `Editor/Dialogs/EditorInputDialog.cs` / `EditorYesNoDialog.cs` are small, generic `ShowPopup()` modals — reasonable as lightweight utility dialogs, not "invented bespoke windows" in the sense the guidance warns against. Their `Show(...)` signatures use optional parameters, noted under Codebase Gotchas below.

### Redundancy/Simplification

**[Info]** `Runtime/Gizmos/GizmosComponent.cs` is a single MonoBehaviour with a **string-typed** `type` field (`"Cube"`, `"Frustum"`, `"WireCubeExtended"`, …) switched on in `OnDrawGizmos`, carrying ~50 serialized fields for all 15 shape variants at once (most unused per instance), mirrored 1:1 by a giant `switch` in `Editor/Gizmos/GizmosEditor.cs`. `ARCHITECTURE.md` itself notes the Gizmos folder was "originally assimilated from a vendored third-party tool" — per `docs/00-AgentGuide.md` §4, vendored code should not be hand-edited in place, so this is flagged for awareness only, not as an actionable fix.

**[Info]** `Behaviours/CooldownTracker.cs` declares `protected WaitForSeconds _pauseOnEmptyWFS` (line 56), assigned once in `Initialization()` (line 68) and never read anywhere else in the class — the actual pause timer is driven manually via `_emptyReachedTimestamp`/`TimeNow`. Dead field; safe to delete.

### Determinism

**[Warning]** `Runtime/Extensions/Collections/CollectionExtensions.cs` — `GetRandom<T>(T[])` (line 59), `GetRandom<T>(IList<T>)` (line 69), and `Shuffle<T>(IList<T>)` (line 274) all call bare `UnityEngine.Random` with **no** `IRandomProvider` overload and **no** "presentation-only" warning comment. This is inconsistent with the sibling files in the very same neighborhood: `Extensions/Collections/IEnumerableExtensions.cs` (`Random<T>`, `Shuffled<T>`, `GetRandomElementWithProbability`) and `Extensions/Random/RandomX.cs` both explicitly document their non-deterministic overloads as `"PRESENTATION-ONLY: uses UnityEngine.Random. Use the IRandomProvider overload in simulation code."` and provide the deterministic twin. `CollectionExtensions` has neither the disclaimer nor the twin, so a gameplay author reaching for the generically-named `.GetRandom()`/`.Shuffle()` on a `List<T>` gets silent non-determinism with no compiler nudge toward the safe sibling API 30 lines away in `IListExtensions.cs`/`IEnumerableExtensions.cs`.

**[Warning]** `Runtime/Extensions/Math/MathX.cs` is internally inconsistent on the same point: `RollADice(int, IRandomProvider)` (line 609) and `Chance(int, IRandomProvider)` (line 622) explicitly require and null-check a deterministic provider, but `Runtime/Extensions/Math/MathExtensions.cs`'s `GetDirectionFromSpread(Quaternion, float)` (line 291) — a name that reads like a weapon-spread/aim-cone helper — uses bare `UnityEngine.Random.Range` with no provider parameter at all, in the same Math extensions neighborhood.

**[Info]** `Behaviours/AreaSpawner.cs`'s `GetRandomSpawnPosition()` (lines ~290-332) does the loud half of fail-fast correctly — it logs a `DebugX...Error(...)` when deterministic startup is active but no `IRandomProvider` was assigned — but then still falls through to `UnityEngine.Random.Range` for the actual spawn position instead of aborting. Per `docs/00-AgentGuide.md`'s fail-fast rule ("never silently substitute"), the diagnostic without an abort is a "loud fallback," not a true fail-fast.

**[Info]** `Extensions/Utilities/InvokeExtension.cs`'s `MonoBehaviour.Invoke(Action, float time, bool realtime)` wraps `WaitForSeconds`/`WaitForSecondsRealtime` as a generic "call this after N seconds" helper with no simulation/presentation distinction in its name or doc comment — an easy trap for gameplay code that needs a delay to reach for this instead of the deterministic scheduler.

**[Info]** `Behaviours/CooldownTracker.cs` drives all timing off `UnityEngine.Time.deltaTime`/`Time.time` (with an unscaled-time toggle). Fine if used purely for UI/VFX cooldown bars; a determinism problem only if some gameplay system uses it to gate an ability's actual availability. The type carries no "presentation-only" disclaimer, and tracing every consumer was out of scope for this pass.

### Doc/Architecture Drift

**[Warning]** `Documentation~/ARCHITECTURE.md:149` describes `MaybeMonad<T>` as "nullable wrapper (`Some`/`None`), functional optional." The actual type (`Runtime/SupportTypes/MaybeMonad.cs`) is a **non-generic static class** of LINQ-chain extension methods (`With`, `Return`, `If`, `Unless`, `Do`, `IfNotNull`) — there is no `Some`/`None` API and no generic `MaybeMonad<T>` type at all. The doc describes a different design than what ships.

**[Warning]** Same table, `ARCHITECTURE.md:150`: `CustomState` is described as a "state-machine support type." The actual `CustomState` (`Runtime/SupportTypes/CustomState.cs`) is a bare `MonoBehaviour` wrapping `Dictionary<string,string> keyValuePairs`, used exclusively by `MonoBehaviourExtensions.RunOnce`/`RunOncePersistent` as a per-object "have I already fired this once" flag store — it has no state-machine semantics (no states, no transitions).

**[Warning]** `ARCHITECTURE.md`'s "Editor tooling" table (lines 216-230) lists Framework Inspector, DebugX Console, Event Bus windows, Tween Debugger, UI Validation, Preset Automation, Entity Debugger, Game State window, Scene Switcher, Weaver, and Prefab Lightmap Generator, but has **no entry at all** for `StaleComponentGuard` (10 files), `EditorEnhancerX` (23 files), or the `AssetImport` plugin pipeline (7 files) — three substantial, currently-shipping subsystems entirely inside this audit's scope. Per `docs/00-AgentGuide.md` §2's doc-freshness rule, additions of this size should have updated the package's own `ARCHITECTURE.md`.

**[Info]** `ARCHITECTURE.md:132` says `SingletonBehaviour<T>` "logs error on duplicate." The actual code (`Runtime/Patterns/SingletonBehaviour.cs:103`) logs `Info`, not `Error` ("Newly loaded scene had a second copy; keeping the session survivor and destroying the duplicate") — a harmless wording drift, not a behavior concern.

**[Info — cross-reference, not this audit's to fix]** The doc's namespace map still calls the inspector engine "Framework Inspector"/`FrameworkInspector` throughout, while code inside this audit's own scope (`Behaviours/AreaSpawner.cs`, `Menus/MenuPaths.cs`, `Editor/StaleComponentGuard/StaleComponentInspectorBadge.cs`, `Editor/Utilities/CommentEditor.cs`) references `AetherNexus.FoundationPlatform.AetherInspector` — an apparent rename that never propagated to `ARCHITECTURE.md`. Flagging for whichever audit pass owns AetherInspector; not re-analyzed here.

### Codebase Gotchas (docs/00 §3)

**No findings** for the following, actively checked across the full scope:
- **`??` on `UnityEngine.Object`**: none found. Every `??`/`??=` hit in scope resolves to `List<T>`, `string`, a delegate, or `System.Type` — never a Unity Object reference.
- **Struct instance-field-initializer / unassigned-field violations**: none found. Every struct in scope (`HSL`, `HSV`, `MinMax01`, `RectSetting`, `ShortcutBinding`, `ComponentOfInterface<T>`, `StaleFinding`, and the various nested `Enumerator`/`OfComponentEnumerator<T>` structs in `GameObjectExtensions.cs`/`GameObjectHierarchyExtensions.cs`) assigns every field in every explicit constructor. Several enumerator structs explicitly use the documented lazy-property workaround for a per-instance scratch buffer (`List<T> _componentCache; List<T> componentCache => _componentCache ?? (_componentCache = new List<T>());`) with a comment calling out the C# 9 constraint by name.
- **Unconditional `OnValidate`/`OnAfterDeserialize` serialized writes**: none found. The only `OnValidate` in scope (`Extensions/Utilities/Comment.cs:34`) writes only the built-in `enabled` field, and only when `Application.isPlaying` — a window that cannot occur during a normal edit-mode scene open/recompile. Not perfectly idempotent (no inequality guard before the write), so flagged as an **Info**-level nit rather than a real dirtying risk.

**[Warning]** `Runtime/Extensions/Reflection/ReflectionExtensions.cs` (`HasMethod`/`HasField`/`HasProperty`) compiles into the **Runtime** assembly with no `#if UNITY_EDITOR` guard, making `GetType().GetMethod/GetField/GetProperty` reflection reachable from gameplay code — contrary to the project's "no runtime reflection outside editor tools" rule.

**[Info]** The project-wide "no optional parameters" rule is violated pervasively and consistently throughout `Runtime/Extensions/*` (e.g. `RigidbodyExtensions.AddForwardForce(..., ForceMode mode = ForceMode.Force)`, `PhysicsExtensions.Raycast(..., LayerMask layerMask = default, ...)`, dozens more) and in `Editor/Dialogs/EditorYesNoDialog.Show(string, string, Action onYes = null, Action onNo = null)`. This reads as inherited/vendored-style utility code predating the rule's adoption, not something introduced recently — noted as one consolidated item rather than dozens of line-level findings, since fixing it would be a large, separate refactor, not a spot-fix.

## Fixes

No code was changed as part of this audit (per instructions, only this document was written). Recommended next steps, roughly in priority order:

1. Resolve the four ambiguous-overload pairs (`Shuffle`, `GetOrAdd`, `SetLossyScale`, `SetWidth`/`SetHeight`) by deleting one side of each pair and redirecting callers — `SetLossyScale` needs a design decision first since the two implementations disagree on math.
2. Add an `IRandomProvider` overload (or at minimum a "PRESENTATION-ONLY" doc comment) to `CollectionExtensions.GetRandom`/`Shuffle` and to `MathExtensions.GetDirectionFromSpread`, matching the pattern already established in `IEnumerableExtensions.cs`/`RandomX.cs`/`MathX.cs`.
3. Update `Documentation~/ARCHITECTURE.md`: correct the `MaybeMonad<T>` and `CustomState` descriptions, add `StaleComponentGuard`/`EditorEnhancerX`/`AssetImport` to the Editor tooling table, and fix the `SingletonBehaviour` wording.
4. Pick one of `PlayerPrefsX` vs `PersistentDataHandler`/`UnityJsonPersistentDataAdapter` as the supported path for new code and note the other as legacy in a doc comment.
5. Move `ColliderGizmoEditor` out of `Runtime/Gizmos/ColliderGizmo.cs` into `Editor/Gizmos/`, matching its sibling components.
6. Delete the dead `_pauseOnEmptyWFS` field in `CooldownTracker`.
7. Guard `ReflectionExtensions.cs` behind `#if UNITY_EDITOR` or move it to an Editor assembly, unless a specific runtime AOT-safe use case requires it to stay in Runtime.

## Cross-references

- **AetherInspector audit**: this package's `ARCHITECTURE.md` still calls the inspector engine "Framework Inspector," while code in this scope (`AreaSpawner`, `MenuPaths`, `StaleComponentInspectorBadge`, `CommentEditor`) references `AetherNexus.FoundationPlatform.AetherInspector`. Whoever audits AetherInspector should reconcile the naming with the doc.
- **`Editor/Drawers/`**: reviewed in full here (not skipped) — `LayerDrawer`/`TagDrawer`/`TooltipIconDrawer` are the editor half of `Runtime/Attributes/`, not AetherInspector's attribute system.
- **GameEngineCore audit**: `Behaviours/SceneSpawnReadyGate.cs` and `AreaSpawner`'s deterministic-spawn integration are wired by `SceneInitializationCoordinator` in GameEngineCore (per this file's own doc comment) — worth confirming from that side that all `AreaSpawner` instances are scene-setup-only, per the Execution Spine note above.
- **Storage/persistence**: `PersistentDataHandler`/`UnityJsonPersistentDataAdapter` (device-local JSON store) vs. `IStateContributor` (`GameEngineCore.Snapshot`, gameplay-state persistence) are two different, non-overlapping-in-purpose mechanisms; flagged here only so a future consumer doesn't conflate them.
