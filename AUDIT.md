# FoundationPlatform — Package Architecture Audit (consolidated)

**Re-audited after fixes** (this pass verified the diff from baseline commit `d714482` to current HEAD in the FoundationPlatform submodule). One process note surfaced repeatedly: several "fixes" were actually bundled into commit `d714482` itself — the same commit that added the original audit files — so a plain `d714482..HEAD` diff misses them; re-audit agents cross-checked `d714482^..HEAD` and `git show d714482` where needed to confirm real progress. Status tags below (**FIXED** / **PARTIALLY FIXED** / **NOT FIXED** / **NEW**) reflect verified current state.

## Re-audit results: what got fixed

- **AetherInspector — all 5 prior findings FIXED** (doc drift, the duplicated "Nested Drawers" settings block, the `??` on `AssetPreview`, the legacy-flag cleanup, `Editor/Drawers/` consolidation) — confirmed bundled into `d714482` itself. The `d714482..HEAD` diff itself was unrelated: a mechanical, verified-correct optional-parameter-to-overload conversion across 7 files. Remaining open (never required fixes): ~24 empty `catch {}` blocks around reflection calls, unchanged.
- **HierarchyX — all 3 prior findings FIXED** (`soloButtons` now defaults `false`; `LayerColor`/`PanelChip` optional params removed; `Documentation~/ARCHITECTURE.md` now has a full HierarchyX section) — also bundled into `d714482`. **NEW code, NEW findings**: `HierarchyXFolderIcons.cs` was added and has two real bugs — its `applyToHierarchy` per-rule toggle is checkboxed but never actually read by `FolderIcons.TryResolve`, so it does nothing; and it draws the identical icon rect as `HierarchyXBestIcon` with no opaque background, causing icon collisions/transparency bleed when both are enabled.
- **ProjectWindowX — 2 of 3 prior findings FIXED** (full `ARCHITECTURE.md` section added; namespace ambiguity resolved as deliberate). The settings-keyword fix (`"sync"`/`"out of sync"` alongside `"drift"`) did **NOT** land — byte-identical keyword set. **Major NEW addition**: a whole new `Editor/ProjectWindowX/Panel/` subsystem was built, faithfully mirroring HierarchyX's `Panel/` mechanism (same `TypeCache` discovery pattern, same menu-item convention) — genuinely a single-registry addition, not a competing seam; its one real consumer is GameEngineCore's new `ContentAreasProjectPanelSection.cs` (see that package's audit). Two new issues: the new Panel subsystem is itself undocumented in `ARCHITECTURE.md` (asymmetric with the HierarchyX section right below it), and — more seriously — **a regression**: the same change window removed `defineConstraints: ["HOMAM_GEC"]` from four editor asmdefs (`HierarchyX.Editor`, `ProjectWindowX.Editor`, `EditorEnhancerX.Editor`, `StaleComponentGuard.Editor`), making them compile unconditionally, while `ARCHITECTURE.md` was simultaneously edited to still claim they're gated — a self-contradicting doc/code drift introduced by the very fix pass meant to reduce drift.
- **Messaging/EventBus — 3 of 6 prior findings FIXED**: `IEntitySpawnState.cs`/`BaseValidatableEvent.cs` deleted; `AddRuleSystemDefine()`'s project-wide define-symbol mutation removed entirely; the ~450-line hardcoded GameEngineCore class-name list replaced with a proper `IRuleSystemDebugClassifier` seam, now documented. **NOT fixed**: `Events.cs`'s stale menu-path comment; the three near-duplicate toolbar/tab-controller trios (~1176 lines, unchanged).
- **DebugX / CoroutineX / Validation & Tooling — largely FIXED.** DebugX: dead `DebugLogger`/`IDebugLogger`/`LoggerFactory`/`LogConfig` path deleted, `ExplicitErrorDedupe`'s unbounded dedup fixed, the unguarded `using UnityEditor;` now properly gated, the class/namespace collision now documented in `docs/00-AgentGuide.md` §3. Still open: `DebugX.LogArray<T>` still bypasses `LogPipeline.Emit`. CoroutineX: resolved differently than recommended — rather than reverting the vendor coupling, **CoroutineX was reclassified as first-party** (removed from the vendored-code table; `ARCHITECTURE.md` now states the DebugX coupling is a deliberate first-party dependency), and the ~50-line dead commented-out block was deleted. Validation & Tooling: `PrefabLightmapData.cs` moved out of `Editor/`; `UIValidationPolicy` now defaults to Strict; `LightmapConfiguration.OnValidate` writes are inequality-guarded; bonus fixes (`Weaver` de-EditorWindow'd, path-helper consolidated, `DownloadSoundFromStoryBlock.cs` removed). Still open: `ARCHITECTURE.md`'s Editor-tooling table still missing ~10 tools.
- **Core Utilities (Extensions grab-bag) — ALL 4 confirmed ambiguous-overload pairs RESOLVED** (`Shuffle`, `GetOrAdd`, `SetLossyScale` — including reconciling the two implementations that used to disagree on math — and `SetWidth`/`SetHeight`), plus both determinism-disclaimer gaps, `AreaSpawner`'s fail-fast, `ReflectionExtensions.cs`'s editor-only guard, and the `ARCHITECTURE.md` corrections. All 7 numbered recommendations from the original audit are done. None of this was in the reviewed diff window either — confirmed already present at the `d714482` baseline.
- **Animation — essentially NOT FIXED**, one of the few subsystems with no real progress: all 8 optional-parameter violations in `AnimatorBridgeBase`/`PlayableGraphBridge`/`MixerStates` are byte-for-byte unchanged, the `AnimatorConstantsGenerator`-vs-Playables-graph dual-ownership question is completely untouched, and the `Awake()` ref-caching inconsistency is unchanged. (A few items — the dead `owner` parameter, a duplicated `FindEntryById`, the `ARCHITECTURE.md` Animation section rewrite — were fixed, but bundled into `d714482` itself, predating this review window.)

## Cross-references

See the GameEngineCore package audit's "Architectural note" section for how the new ProjectWindowX `Panel/` mechanism and the `IProjectPanelSection` seam get consumed on the GameEngineCore side (`ContentAreasProjectPanelSection.cs`), and its own re-audit note on the `defineConstraints` regression above.

No source files were modified during this re-audit — all agent passes and this consolidation are read/write-AUDIT.md-only.

---

## Original audit (pre-fix baseline, kept for history)

Consolidates 11 subsystem audits (each with full findings, file/line citations, and fix lists):

| Subsystem | Audit file |
|---|---|
| AetherInspector | [Runtime/AetherInspector/AUDIT.md](Runtime/AetherInspector/AUDIT.md) |
| Messaging (EventBus) | [Runtime/Messaging/AUDIT.md](Runtime/Messaging/AUDIT.md) |
| CoroutineX (vendored) | [Runtime/CoroutineX/AUDIT.md](Runtime/CoroutineX/AUDIT.md) |
| TweenX | [Runtime/TweenX/AUDIT.md](Runtime/TweenX/AUDIT.md) |
| DebugX (+ Console, Debugging) | [Runtime/DebugX/AUDIT.md](Runtime/DebugX/AUDIT.md) |
| Identity | [Runtime/Identity/AUDIT.md](Runtime/Identity/AUDIT.md) |
| Animation (+ AnimGraph) | [Runtime/Animation/AUDIT.md](Runtime/Animation/AUDIT.md) |
| Core Utilities (Attributes, Behaviours, Extensions, Patterns, SupportTypes, Menus, Gizmos + editor) | [Documentation~/AUDIT-CoreUtilities.md](Documentation~/AUDIT-CoreUtilities.md) |
| ProjectWindowX | [Editor/ProjectWindowX/AUDIT.md](Editor/ProjectWindowX/AUDIT.md) |
| HierarchyX | [Editor/HierarchyX/AUDIT.md](Editor/HierarchyX/AUDIT.md) |
| Validation & Tooling (Validation, Windows, Tools, PackageIntegration) | [Documentation~/AUDIT-ValidationAndTooling.md](Documentation~/AUDIT-ValidationAndTooling.md) |

## Package-wide themes (seen across ≥3 subsystems — not isolated slips)

1. **`Documentation~/ARCHITECTURE.md` is severely out of date, package-wide.** This is the single dominant finding of the whole pass. Concretely:
   - Describes AetherInspector under its **old name "Framework Inspector"** with namespaces/classes/folders that no longer exist (`FrameworkEditor`, `Runtime/FrameworkInspector/`, etc.) — the sample code in the doc would not compile.
   - **Never mentions ProjectWindowX or HierarchyX at all** — the project's own #1 and #2-ranked designer surfaces, ~36 files and two `TypeCache` extensibility contracts other packages depend on, are invisible to anyone reading this package's architecture doc.
   - Misdescribes `Identity`'s namespace/location, `MaybeMonad<T>` (claims a generic `Some`/`None` type; actual is a non-generic static extension class), and `CustomState` (claims "state-machine support type"; actual is a flag-store `MonoBehaviour`).
   - Omits `StaleComponentGuard`, `EditorEnhancerX`, `AssetImport`, and roughly half of `Editor/Tools`/`Editor/Windows` from its own "Editor tooling" table.
   - Documents CoroutineX as ordinary first-party code with **no mention it's vendored** (the vendored/do-not-hand-edit rule lives in exactly one place, `docs/00-AgentGuide.md` §4).
   - Animation's `## Animation` section covers roughly 4 of 15+ real types and points at an "invariants doc" that doesn't exist.

   **Why this matters**: `docs/00-AgentGuide.md` §2 requires updating the package's `ARCHITECTURE.md` in the same task as any contract/naming change. None of these renames/additions propagated. Any agent or developer following the documented "read `ARCHITECTURE.md` first" path — which is exactly what this audit itself was instructed to do — gets actively misled.

2. **Optional parameters are pervasive**, in direct, wide violation of the project's "no optional parameters" rule: TweenX's whole fluent API, Animation's `AnimatorBridgeBase`/`PlayableGraphBridge`/`MixerState` family, HierarchyX's `LayerColor`/`PanelChip`, dozens of call sites across `Runtime/Extensions/*`, and several DebugX constructors. Reads as a systemic, inherited convention rather than isolated mistakes — worth a single project-wide decision (grandfather it with a documented exception, or schedule a real cleanup pass) rather than fixing one file at a time.

3. **Namespace/class name collisions of the same shape as the documented `Debug`/`GameEngineCore.Debug` gotcha, not yet written down**: `DebugX` (class) vs. `AetherNexus.FoundationPlatform.DebugX` (namespace) — confirmed already forcing `DebugX.DebugX.Logger(...)` double-qualification in real consumer code (`SingletonBehaviour.cs`, `AreaSpawner.cs`). Recommend adding this to `docs/00-AgentGuide.md` §3 next to the existing entry.

4. **Two parallel "old mechanism + new mechanism" pairs exist without a documented decision on which wins**:
   - AetherInspector's rich attribute engine vs. the older per-attribute `Editor/Drawers/` (`LayerDrawer`/`TagDrawer`/`TooltipIconDrawer`) for `Runtime/Attributes/`.
   - Animation's Playables-based AnimGraph vs. `AnimatorConstantsGenerator`'s Mecanim `AnimatorController` hash-codegen — **confirmed both live simultaneously** in the shipping `CharacterAnimator` (GameFramework, to be cross-checked in that package's audit).

5. **Determinism discipline is inconsistent within the same neighborhood.** Several files correctly document "PRESENTATION-ONLY, use the `IRandomProvider` overload for simulation" right next to sibling files that use bare `UnityEngine.Random` with no such disclaimer or deterministic twin (`CollectionExtensions.GetRandom`/`Shuffle`, `MathExtensions.GetDirectionFromSpread`). Nothing here is confirmed to leak into actual gameplay from within this package, but the inconsistent labeling is exactly what invites a future accidental violation downstream.

## Findings ranked by severity/impact

### Critical / Error
- **TweenX — `FeedbackTimeFreeze` (resolved — removed).** Previously could corrupt global `Time.timeScale` when restore was dropped on kill. Class deleted; timescale / hit-stop belongs in GameEngineCore GameManager, not Foundation. See TweenX audit, Determinism §.
- **Core Utilities — four confirmed ambiguous-overload pairs** (`Shuffle`, `GetOrAdd`, `SetLossyScale`, `SetWidth`/`SetHeight`) sitting in different files of the same namespace — latent `CS0121` compile errors, one of which (`SetLossyScale`) has **two implementations that compute different math**, so "just delete one" would silently change behavior for whichever side loses.
- **DebugX — dead second logging implementation** (`DebugLogger`/`IDebugLogger`/`LoggerFactory`/`LogConfig`) gates on a config object nothing else writes to; a silent second source of truth for "is this log enabled" if anyone resurrects it.
- **DebugX — `ExplicitErrorDedupe` can silently drop unrelated future errors forever** (unbounded thread-static string-match set, never cleared) — a direct fail-fast violation inside the project's own mandated logging framework, plus an unbounded memory leak.
- **DebugX — unguarded `using UnityEditor;` in the Runtime assembly** (`includePlatforms: []`) — likely `CS0246` on an actual player build; invisible in-editor. Unverified without a real build, flagged as the same class of trap as the documented `Debug` namespace gotcha.

### High / Warning
- Doc/architecture drift, package-wide (see theme #1 above) — recommend one dedicated pass rewriting `Documentation~/ARCHITECTURE.md` rather than patching it per-subsystem.
- **CoroutineX vendor-integrity risk**: hand-patched error paths wired to `DebugX`, plus a ~50-line dead alternate implementation left commented out in the vendor file — no upstream diff available to confirm scope of drift; vendored status isn't documented anywhere a contributor would see it before editing.
- **HierarchyX — `soloButtons` re-implements Unity's own Scene-Visibility hover icons** via the same public API at the same row position — a direct instance of the "Unique-only UI" rule the project's own docs call out, worth a deliberate on/off decision.
- **EventBus layering violation**: hardcodes ~15 GameEngineCore class names in the foundation layer's core runtime file for debug-display cosmetics; `EventPublishHistoryWindow` can mutate project-wide Scripting Define Symbols from a debug-window button — both go beyond what a foundation-layer diagnostic tool should reach into.
- **EventBus — dead product-domain leak**: `IEntitySpawnState` (unit/city spawn indices) has zero usages and violates docs/02's "must not own product-specific domain content" for this genre-agnostic layer.
- **AetherInspector — duplicated settings UI block causes silent data loss**: the "Nested Drawers" section in `InspectorXSettingsProvider` is drawn twice; edits through the second (unguarded) copy never persist.
- **Animation — dual animator mechanisms coexist undocumented** (Playables graph + legacy Mecanim hash-codegen), confirmed in the shipping `CharacterAnimator`.
- **Validation & Tooling — a runtime-intended `MonoBehaviour` (`PrefabLightmapData`) is compiled Editor-only** — would show as a missing script in any real player build if ever adopted; currently dormant but a landmine.
- **Validation & Tooling — declared validation severities are silently downgraded** by a per-machine `EditorPrefs` default (`WarningFirst`), so "same issue class → same severity" doesn't actually hold until someone flips a toggle.

### Medium / Info-leaning-Warning
- Core Utilities: "get-or-add component" implemented 4 ways, "nearest object" 3 ways, "set layer recursively" 3 ways, two competing local-persistence mechanisms (`PlayerPrefsX` vs. `PersistentDataHandler`/adapter) with no stated preference.
- `ColliderGizmo` inlines its custom editor in the Runtime file while sibling Gizmos components correctly split theirs into `Editor/` — inconsistent convention in the same folder.
- `ReflectionExtensions.cs` (`HasMethod`/`HasField`/`HasProperty`) compiles unguarded into the Runtime assembly — tension with "no runtime reflection outside editor tools."
- DebugX's own reflection-based caller-info path runs unconditionally (not `#if UNITY_EDITOR`-gated) in every log call including `DEVELOPMENT_BUILD` player configurations, unlike a sibling file in the same package that correctly gates the same kind of reflection.
- ProjectWindowX/HierarchyX/EventBus editor tooling each have minor internal duplication (parallel logic-class+wrapper-class pass pairs; near-identical tab-controller trios) — cosmetic, not correctness risk.

### Low / Info
- Several harmless legacy aliases and self-documented "legacy flag" fields still load-bearing in AetherInspector (`ButtonAttribute.Stretch`, `FoldoutInSection`/`SectionFoldoutTitle`) — candidates for a follow-up cleanup once call sites are confirmed migrated, not urgent.
- `CooldownTracker` carries one dead field (`_pauseOnEmptyWFS`).
- Minor duplicated ID-format string between `IdentityComponent` and `IdentityFieldDrawer`.
- `Weaver` retains a vestigial, unused `EditorWindow` base class.

## Cross-references carried forward to later package audits

- **GameFramework audit (queued)**: confirm whether `CharacterAnimator.cs` (which carries the `AnimatorConstantsGenerator`-generated region) still needs the legacy Mecanim path alongside the Playables graph; check `QuestSystem`/`ItemSystem`/`CharacterSystem`/`RagdollHelper` for actual `CoroutineX.Run`/`Routines.Delay` call sites and whether any feed a gameplay-affecting timer (would violate docs/11); check `RagdollHelper`'s own vendored status for the same hand-patch/undocumented pattern found in CoroutineX; re-check `AttributeBarView.cs`'s use of `TweenValue.Float` stays presentation-only as it evolves.
- **GameEngineCore audit (next, task #2)**: verify `EngineConceptIntrospection`/chip classification (already spot-checked clean from the HierarchyX side); audit `DomainHierarchyDecorator`/`SessionHierarchyDecorator`/panel-section files for their own doc/vocabulary/gotcha compliance; confirm `Editor/PackageIntegration/PackageIntegrationManifest.cs`'s ensure-semantics compliance (out of FoundationPlatform's scope entirely); check `AuthoringProjectWindowXConsumers.cs` etc. for vocabulary compliance (spot-checked clean already).
- **TacticalFeatures audit (queued)**: no FoundationPlatform-side findings feed this directly, but note the general "two mechanisms, no documented decision" pattern found repeatedly in this package (AetherInspector/Drawers, AnimGraph/AnimatorConstantsGenerator) is worth watching for when auditing CombatSystem's vs. TacticalFeatures' separate `Targeting`/`Threat` folders.

## Not yet resolved / needs a product decision (not a code defect)

- Whether the `DebugX`↔`CoroutineX` coupling and the `RunAsync`/`WaitForCompletionAsync` additions in the vendored `CoroutineX.cs` are sanctioned integrations or accidental patches — needs a real upstream diff plus a maintainer decision.
- Whether `HierarchyXRowControls.soloButtons` should default off (Unity already covers it) or stay as a documented, deliberate redundancy.
- Whether `DownloadSoundFromStoryBlock.cs` belongs in FoundationPlatform at all.

No source files were modified during this audit — all 11 subsystem passes and this consolidation are read/write-one-AUDIT.md-only, per the approved plan.
