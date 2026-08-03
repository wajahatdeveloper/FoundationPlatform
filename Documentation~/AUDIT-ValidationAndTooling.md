# Validation & Tooling (Validation, Windows, Tools, PackageIntegration) — Architecture Audit

## Context

Scope covered, all `.cs` files read in full (30 files, ~7,260 lines):

- `Editor/Validation/` — three unrelated concerns, not one pipeline:
  - `DataPathPolicy/HierarchyPathPolicy.cs`, `HierarchyPatternMatcher.cs` — read-only asset-path classification/glob-matching primitives (`Match`, `TryClassify`, `ExpandConcreteFolders`). Consumed by GameEngineCore's Central Authoring / domain-manifest code (confirmed by repo-wide grep), not just by anything in this package.
  - `MenuItemDuplicatePathValidator.cs` — `[InitializeOnLoad]` regression guard that logs when two `[MenuItem]` registrations collide on the same path/role.
  - `UI/*` (`UIValidationEngine`, `UIValidationConventions`, `UIValidationPolicy`, `UIValidationPostprocessor`, `UIValidationMenu`, `UIValidationReporter`) — a self-contained linter that enforces UI folder-layer conventions (UIElement/Widget/Panel/Orchestration) for scripts and prefabs under `Assets/Scripts/UI`, `Assets/Content/UI`, wired to asset-import batching.
- `Editor/Windows/` — three generic `EditorWindow`s: `AutoBinderWindow` (generates a field-binding code snippet from a scanned hierarchy), `SceneSwitcherWindow` (scene list/open/play-mode-start utility), `ScriptGeneratorWindow` (reusable generate-preview-write-script window, driven by callers elsewhere in the package).
- `Editor/Tools/` — a grab-bag of generic editor utilities: `CameraScreenshot`, `ClipboardToScript`, `CodeAwareRename` (shared regex-based class/namespace rewrite), `DownloadSoundFromStoryBlock` (third-party-website MP3 scraper), `MonoBehaviourScriptDuplicator` (+ `ScriptReplacerWindow`), `Package2Folder` (reflection-based `.unitypackage` import-to-folder), `Weaver` (Tags/Layers/Scenes/Animations/NavMesh/Shaders `.g.cs` codegen), `EditorGUIX_ImageStringConverter/` (image↔Base64 string tool), `PrefabLightmapGenerator/` (prefab lightmap bake-and-reapply component + editor), `PresetAutomation/` (auto-apply `.preset` assets on import, with settings/diagnostics window).
- `Editor/PackageIntegration/` — a single file, `HomamGecOrphanDefineCleaner.cs`, that strips the stale `HOMAM_GEC` scripting-define symbol when the GameEngineCore package is not registered. It is **not** the domain/integration-manifest "ensure semantics" pipeline described in docs/03–04; that pipeline (`PackageIntegrationManifest.cs`, domain folder/README/scripts-folder creation, `CentralAuthoringProjectConfig` registration) lives entirely in `Packages/com.aethernexus.gameenginecore/Editor/PackageIntegration/`, outside this package and outside this audit's file scope. Likewise, the "Central Validation" / "Central Window" described in docs/09 and docs/13 (`CentralWindow.cs`) lives in GameEngineCore, not in this package.

Also read: `docs/00`, `01`, `03`, `04`, `09`, `13`, and `Documentation~/ARCHITECTURE.md` (260 lines).

## Findings

### Designer Surface Priority / Last-Resort Positioning

No findings. None of the `EditorWindow`s in scope compete with Project/Hierarchy/Inspector for domain-content authoring — they are generic engine-dev utilities (scene switching, script generation/duplication, screenshot, preset automation, codegen, asset import). `Runtime/Menus/MenuPaths.cs` explicitly documents this package's tools as "unwrapped... reads as generic/native engine tooling, not a product," registered under plain `Tools/*` and `Window/*` (not `Tools/Domain/*` / `Window/Domain/*`, which is reserved for the Domain-authoring hub in GameEngineCore). The docs/09 "Central Window is last resort" concern is about the GameEngineCore `CentralWindow`, which is out of this package entirely — there is nothing in this package's scope that duplicates Project/Hierarchy/Inspector daily-authoring functionality.

### Validation Boundaries

- **Warning** — Runtime component compiled only into an Editor-only assembly; would silently no-op in shipped builds if ever used.
  `Editor/Tools/PrefabLightmapGenerator/PrefabLightmapData.cs` (whole file; `Awake` lines 71–74, `OnEnable`/scene-load reapply lines 76–84, 271–276) is a plain `MonoBehaviour` (no `#if UNITY_EDITOR` around its runtime lifecycle) whose own doc comment says it "allow[s] lightmaps to be baked once and reused across scenes... automatically applies stored lightmap data when the prefab is instantiated" — i.e. it is meant to run in the built game. But the file lives under `Editor/`, which is covered by `Editor/FoundationPlatform.Editor.asmdef` (`"includePlatforms": ["Editor"]`, confirmed by reading the asmdef; no nested asmdef overrides `Tools/PrefabLightmapGenerator/`). That means the type does not exist in player builds at all: any prefab carrying this component in a real build would show a missing script and never reapply baked lightmaps. Repo-wide search found no prefab/scene currently referencing `PrefabLightmapData`, so it is presently dormant rather than actively broken — but it is a landmine for whoever adopts the tool. Fix is a folder move (Runtime-side component + Editor-side baking/menu code split), not a logic change.
- No other findings. The rest of the in-scope validation code (`UIValidationEngine`, `HierarchyPathPolicy`/`HierarchyPatternMatcher`, `MenuItemDuplicatePathValidator`) is genuinely editor-time-only: it inspects `AssetDatabase` paths, `MonoScript`/`TypeCache` reflection, and asset contents — never true runtime data (player requests, payloads, dynamic state).

### Severity Semantics

- **Warning** — Declared severities are silently overridden to Warning by a lenient default, undermining "same issue class maps to same severity."
  `Editor/Validation/UI/UIValidationPolicy.cs` lines 30–40 (`ResolveSeverity`): unless the developer has manually switched `Tools/Linting/Rollout Mode: Strict` (an `EditorPrefs` toggle, defaulting to `WarningFirst`), every rule except `UIV000` (`ConfigMissingOrInvalid`) is reported as `Warning` regardless of how it is coded in `UIValidationEngine.cs` (e.g. `ScriptOutsideMappedFolders`, `PrefabOutsideMappedFolders`, `PanelRootReferencesOrchestration`, `WidgetRootReferencesPanelOrOrchestration`, `ReverseLayerReference`, `InvalidNamingSuffix`, `SerializedDomainDependencyOnRoot`, `MismatchedPrefabFolderLayer` are all built via `AddError`/`UIValidationSeverity.Error` in the engine). Effective severity is therefore a per-machine EditorPrefs setting, not a stable property of the issue class, contrary to docs/13 ("Same issue class maps to same severity across tools"). A team member who has never touched the rollout-mode menu will see every one of these as a non-blocking Warning even though the code author intended several as blocking Errors.

### Master List / Manifest Machinery

No scope violation found, but the folder does not contain what its name implies:
- `Editor/PackageIntegration/HomamGecOrphanDefineCleaner.cs` only removes a stale scripting-define symbol (`RemoveSymbol`, lines 59–67) after checking `PackageInfo.GetAllRegisteredPackages()` — a narrow, self-contained hygiene task. It never creates, moves, renames, or deletes domain folders/manifests, so it doesn't violate the docs/04 "Generated domains" ensure-semantics rule — it simply isn't part of that system at all.
- The actual domain/manifest "ensure-creates-only" pipeline (folder + README + scripts-folder creation, `CentralAuthoringProjectConfig` registration) lives in `Packages/com.aethernexus.gameenginecore/Editor/PackageIntegration/PackageIntegrationManifest.cs`, outside this audit's scope, so its ensure-semantics compliance cannot be assessed from this package.
- This package's real, load-bearing contribution to that system is `Validation/DataPathPolicy/HierarchyPathPolicy.cs` + `HierarchyPatternMatcher.cs` (confirmed via repo-wide grep to be consumed by `AuthoringAssetOperations.cs`, `CentralAuthoringAssetPostprocessor.cs`, `CentralAuthoringRegistry.cs`, `DomainCatalog.cs`, and `PackageIntegrationManifest.cs` in GameEngineCore). This code is pure read-only classification/matching (`AssetDatabase.FindAssets`/`IsValidFolder`, string matching) — it never writes anything, so ensure-semantics simply doesn't apply to it, and it is compliant by construction.

### Ownership

No findings of competing/duplicate pipelines within this package: UI Validation (layer/naming conventions), `MenuItemDuplicatePathValidator` (menu-registration regression guard), and `DataPathPolicy` (path classification) each cover distinct, non-overlapping concerns, and none of them duplicate the GameEngineCore Central Validation / manifest pipeline. See the naming-collision note below (Doc/Architecture Drift) — it is a naming/documentation issue, not a functional ownership conflict.

### Redundancy/Simplification

- **Info** — Duplicated path-under-root normalization logic.
  `Validation/DataPathPolicy/HierarchyPathPolicy.cs` lines 40–49 (`IsPathUnderRoot`) and `Validation/UI/UIValidationConventions.cs` lines 108–117 (`IsPathUnder`) implement the identical algorithm (backslash-normalize, trim trailing slash, `StartsWith`/`Equals` with `OrdinalIgnoreCase`) independently, in two files under the same `Validation/` tree. One shared helper would remove the duplication.
- **Info** — Dead `EditorWindow` inheritance.
  `Tools/Weaver.cs` line 19 declares `public class Weaver : EditorWindow` but the class has no `OnGUI` override and is never opened via `GetWindow<Weaver>()` anywhere — it is used purely as a static utility class (all members are `static` methods invoked from `[MenuItem]`s). The `EditorWindow` base is vestigial; every sibling codegen tool in the same folder (`CodeAwareRename`, `Package2Folder`) is a plain `static class`.
- **Info** — Two windows solve the same "generate a code snippet for the user" problem without sharing infrastructure. `Windows/AutoBinderWindow.cs` builds and previews a field-binding snippet in its own `EditorGUILayout.TextArea` with no "Copy to Clipboard" affordance (only a label telling the user to "Copy and paste it into your script," lines 100–101), while `Windows/ScriptGeneratorWindow.cs` — used elsewhere in this same package by `MonoBehaviourScriptDuplicator` — already provides generate/preview/copy-to-clipboard/write-to-disk for exactly this kind of workflow. AutoBinder does not build on it.
- **Info** — Scope fit. `Tools/DownloadSoundFromStoryBlock.cs` is a single-purpose scraper tied to one third-party website ("Story Block"), unrelated to Unity/game tooling. `Documentation~/ARCHITECTURE.md` describes this package as "Base platform layer for the AetherNexus ecosystem" with reusable, engine-agnostic tooling; this tool reads as a personal convenience utility that happens to live here rather than foundational platform tooling (it is also, along with several other Tools/Windows files, undocumented in ARCHITECTURE.md — see Doc/Architecture Drift).

### Doc/Architecture Drift

- **Warning** — `Documentation~/ARCHITECTURE.md`'s "Editor tooling" table (lines 216–231) only documents a subset of what is actually in this audit's scope. Present: UI Validation, Preset Automation, Scene Switcher, Weaver, Prefab Lightmap Generator. Absent: `Windows/AutoBinderWindow.cs`, `Windows/ScriptGeneratorWindow.cs`, `Tools/MonoBehaviourScriptDuplicator.cs` (+ its nested `ScriptReplacerWindow`), `Tools/CodeAwareRename.cs`, `Tools/ClipboardToScript.cs`, `Tools/CameraScreenshot.cs`, `Tools/DownloadSoundFromStoryBlock.cs`, `Tools/Package2Folder.cs`, `Tools/EditorGUIX_ImageStringConverter/*`, `Validation/MenuItemDuplicatePathValidator.cs`, `Validation/DataPathPolicy/*`, and the entire `Editor/PackageIntegration/` folder. Given docs/03 states each package's `ARCHITECTURE.md` provides "symbol-level detail... read when extending or debugging that package," roughly half the files in this audit's scope have no entry.
- **Info** — Cross-reference to the Validation Boundaries finding above: `PrefabLightmapData`'s own doc comment describes runtime behavior ("reused across scenes," "applies... when the prefab is instantiated") that contradicts its Editor-only assembly placement — the code's stated intent and its actual compiled scope disagree.
- **Info** — Folder-name collision, not a functional conflict: this package has an `Editor/PackageIntegration/` folder and GameEngineCore has an unrelated `Editor/PackageIntegration/` folder holding the actual manifest "ensure" pipeline. Someone orienting by folder name alone could reasonably assume this package owns manifest machinery it does not.

### Codebase Gotchas

- **Warning** — Unconditional serialized-field write in `OnValidate` (the exact pattern docs/00 §3 warns about).
  `Tools/PrefabLightmapGenerator/LightmapConfiguration.cs` lines 127–133: `OnValidate()` unconditionally reassigns `maxLightmapsPerBatch`, `maxShaderCacheSize`, `maxRenderersWarningThreshold` via `Mathf.Max(1, ...)` on every fire (scene-open, recompile) with no inequality guard, matching the documented "flags the object modified even when the value is byte-identical" trap. Low real-world impact (a single rarely-touched settings asset), but it is a textbook instance of the pattern the docs specifically call out; an inequality guard (`if (x < 1) x = 1;`) would make it idempotent per the recommended fix.
- Checked and clean: `PrefabLightmapData.OnValidate()` (same folder, lines 86–91) only writes `isInitialized`, which is a private field with no `[SerializeField]` — not serialized, so no dirty risk.
- No `??` usage on a `UnityEngine.Object` was found anywhere in scope (all instances are on `string`/`Type.FullName` — e.g. `UIValidationEngine.cs:393`, `SceneSwitcherWindow.cs:137,276,282`, `ScriptGeneratorWindow.cs:63,66,67,116,197,209`, `CameraScreenshot.cs:15`, `ClipboardToScript.cs:138`, `MonoBehaviourScriptDuplicator.cs:137,151,152,163,296`). No findings.
- No struct-rule violations: the four `struct`/`readonly struct` types in scope (`ScriptGeneratorWindow.GenerationContext`, `MonoBehaviourScriptDuplicator.DuplicationSession`, `PrefabLightmapData.RendererInfo`/`LightInfo`) either have no instance field initializers with an explicit ctor assigning every field, or are plain serializable structs with no user-defined ctor. No findings.
- `Debug` namespace collision (docs/00 §3, `GameEngineCore.*` only) does not apply — nothing in this package's namespace starts with `GameEngineCore.`. No findings.

## Fixes

Per the audit rules, no source files were modified — findings only. Suggested remediation, in priority order:

1. Move `PrefabLightmapData.cs`'s runtime-facing `MonoBehaviour` (and ideally `LightmapConfiguration.cs` if it should ever be runtime-loadable) out of `Editor/` into `Runtime/`, keeping only the `[MenuItem]`/`AssetDatabase`/`Lightmapping.Bake()`-dependent baking methods and `PrefabLightmapDataEditor` under `Editor/`. This is a structural move, not a logic change, and should be confirmed with the user before touching file locations (per repo file-creation/move rules).
2. Make `UIValidationPolicy`'s default rollout mode intentional and documented (either default to `Strict` so declared severities take effect out of the box, or clearly surface in the tool's own UI that `WarningFirst` downgrades all but one rule).
3. Guard `LightmapConfiguration.OnValidate()` writes with inequality checks (`if (maxLightmapsPerBatch < 1) maxLightmapsPerBatch = 1;` etc.) per the docs/00 §3 idempotent-write pattern.
4. Extract the duplicated path-under-root helper (`HierarchyPathPolicy.IsPathUnderRoot` / `UIValidationConventions.IsPathUnder`) into one shared utility.
5. Drop the unused `EditorWindow` base from `Weaver` (make it a plain `static class` like its siblings).
6. Update `Documentation~/ARCHITECTURE.md`'s "Editor tooling" table to list the currently-undocumented tools (or explicitly note which are intentionally omitted as minor/legacy utilities).
7. Confirm with the user whether `DownloadSoundFromStoryBlock.cs` should stay in FoundationPlatform or be relocated/removed as out-of-scope personal tooling.

## Cross-references

- `docs/00-AgentGuide.md` §3 — OnValidate/OnAfterDeserialize dirty-write trap, assembly boundaries, `??` on Unity objects, struct rules.
- `docs/01-CorePrinciples.md` — Validation boundaries (editor-time vs runtime vs generated outputs).
- `docs/03-Frameworks.md` — Per-package `ARCHITECTURE.md` symbol-level detail; tier-one registry refresh contracts.
- `docs/04-DataAndDomains.md` — Generated domains / ensure-semantics ("only ever creates what is missing and never moves, renames, deletes, or generates code").
- `docs/09-EditorHub.md` — Surface priority #6 (editor windows, last resort); `IEntityDebugSection`/`IWorldDebugSection` auto-discovery (not present in this package's scope).
- `docs/13-AuthoringStandards.md` — Severity semantics (Error/Warning/Info), shared action vocabulary, mapping enforcement (Mapped/Out of Sync/Unclaimed).
- `Packages/com.aethernexus.foundationplatform/Documentation~/ARCHITECTURE.md` — "Editor tooling" table and assembly-definitions section (source for the `Editor/` → `includePlatforms: [Editor]` confirmation used in the Validation Boundaries finding).
- `Packages/com.aethernexus.gameenginecore/Editor/PackageIntegration/PackageIntegrationManifest.cs` and `Editor/CentralAuthoring/CentralWindow.cs` — the actual manifest-ensure pipeline and Central Window, both out of this package and referenced only for scope clarification.
