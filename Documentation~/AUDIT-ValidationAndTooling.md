# Validation & Tooling (Validation, Windows, Tools, PackageIntegration) — Architecture Audit (Re-Audit)

## Re-Audit Context

Follow-up pass after fixes were applied. Method: re-read the original findings below, diffed
`d714482..HEAD` for `Editor/Tools/ Editor/Utilities/ Editor/Validation/`, and read current file states
directly (several structural fixes — the `PrefabLightmapData` move, the `Weaver` `EditorWindow`
removal, the shared path-helper consolidation, the `DownloadSoundFromStoryBlock` removal — predate
`d714482` itself, the baseline "Audit" commit this diff range starts from, so they don't appear as
changed lines in the reviewed diff but are confirmed present in `HEAD`).

**Headline result: all three structural findings called out for this re-audit are fixed**
(`PrefabLightmapData` moved to `Runtime/`, `UIValidationPolicy`'s default rollout mode now `Strict`,
`LightmapConfiguration.OnValidate` writes now guarded). Beyond those, two more Redundancy findings from
the original audit are also independently fixed (`Weaver` is now a plain `static class`; the duplicated
path-under-root logic is now a single shared `PathComparisonUtility.IsPathUnder` helper), and
`DownloadSoundFromStoryBlock.cs` — flagged as questionable scope-fit — has been removed entirely. The
`Documentation~/ARCHITECTURE.md` "Editor tooling" table has been partially updated to reflect these
fixes (Weaver and Prefab Lightmap Generator entries now correctly describe the new state) but still
does not list several tools that were already undocumented in the original audit (AutoBinderWindow,
ScriptGeneratorWindow, MonoBehaviourScriptDuplicator, CodeAwareRename, ClipboardToScript,
CameraScreenshot, Package2Folder, the `EditorGUIX_ImageStringConverter`, `MenuItemDuplicatePathValidator`,
`DataPathPolicy`, or the `PackageIntegration` folder) — that Info-level drift finding remains open.

## Original Context

Scope covered, all `.cs` files read in full (30 files, ~7,260 lines):

- `Editor/Validation/` — three unrelated concerns, not one pipeline:
  - `DataPathPolicy/HierarchyPathPolicy.cs`, `HierarchyPatternMatcher.cs` — read-only asset-path
    classification/glob-matching primitives.
  - `MenuItemDuplicatePathValidator.cs` — `[InitializeOnLoad]` regression guard.
  - `UI/*` (`UIValidationEngine`, `UIValidationConventions`, `UIValidationPolicy`,
    `UIValidationPostprocessor`, `UIValidationMenu`, `UIValidationReporter`) — a self-contained linter
    for UI folder-layer conventions.
- `Editor/Windows/` — `AutoBinderWindow`, `SceneSwitcherWindow`, `ScriptGeneratorWindow`.
- `Editor/Tools/` — `CameraScreenshot`, `ClipboardToScript`, `CodeAwareRename`,
  `DownloadSoundFromStoryBlock`, `MonoBehaviourScriptDuplicator` (+ `ScriptReplacerWindow`),
  `Package2Folder`, `Weaver`, `EditorGUIX_ImageStringConverter/`, `PrefabLightmapGenerator/`,
  `PresetAutomation/`.
- `Editor/PackageIntegration/` — `HomamGecOrphanDefineCleaner.cs` only; the real domain/manifest
  "ensure semantics" pipeline lives in `com.aethernexus.gameenginecore`, out of scope.

Also read: `docs/00`, `01`, `03`, `04`, `09`, `13`, and `Documentation~/ARCHITECTURE.md` (260 lines).

## Mechanical fix (this diff range): optional-parameter cleanup — verified correct

`d714482..HEAD` converts optional parameters into overload pairs across most files in this audit's
scope: `ReflectionGuard.Method` (`Editor/EditorEnhancerX/Infra/ReflectionGuard.cs`),
`CameraScreenshot.Take`, `PresetAutomationSettings.FindOrCreateSettingsAsset`,
`AuthoringUxShared.DrawTooltipIcon`, `DataFolderMappingConfig`'s
`DiscoverExemptRootFolders`/`IsPathUnderExemptScope`/`IsTypeScriptUnderExemptScope` (3 methods),
`EditorGUIX.DrawLine`/`DrawBackgroundLine`/`DrawBackgroundBox`/`DropArea<T>`,
`EditorX.DrawDirectionalLine`/`DrawTinyArrow`/`DrawFlyPath`/`GetPrefabPath`, `ScriptableObjectX.CreateAsset<T>`,
and `ScriptingDefineSymbolController.ReimportScripts`. All spot-checked: each new reduced-arity overload
calls the full method with the exact default value that was removed (e.g.
`DropArea<T>(areaText, height) => DropArea<T>(areaText, height, false, null)`,
`DiscoverExemptRootFolders() => DiscoverExemptRootFolders(null)`).

Cross-repo call-site check for the two-defaulted-parameter cases (`EditorGUIX.DropArea<T>`,
`EditorX.DrawDirectionalLine`, `EditorX.DrawTinyArrow`, which only got one reduced-arity overload each,
not one per default) found no call site anywhere in the `HOMAM` tree using a partial arg count that
would now fail to resolve — the only real call sites for `DrawDirectionalLine`/`DrawTinyArrow` are
inside `EditorX.cs` itself (full-arity), and `DropArea<T>` has no call sites found outside this file.
`IsPathUnderExemptScope`/`IsTypeScriptUnderExemptScope` are called with either 1 or 2 args from
`com.aethernexus.gameenginecore`'s `CentralAuthoringRegistry.cs`/`ScriptsHierarchyValidator.cs` — both
arities resolve correctly against the new overloads. No compile breaks found. Not itemized further
per instructions.

Separately, this diff range also renamed several path constants from `Assets/Data/*` to
`Assets/Content/*` across `DataFolderMappingConfig.cs`, `HierarchyPathPolicy.cs`,
`HierarchyPatternMatcher.cs` (comment only), and `UIValidationConventions.cs`/`UIValidationEngine.cs`
— a consistent, unrelated rename applied uniformly across all path-policy files in scope; not a defect,
noted for completeness since it touches files this audit covers.

## Findings

### Designer Surface Priority / Last-Resort Positioning
No findings. Unchanged.

### Validation Boundaries

- **RESOLVED** — `PrefabLightmapData.cs`'s runtime-facing `MonoBehaviour` (originally
  `Editor/Tools/PrefabLightmapGenerator/PrefabLightmapData.cs`, compiled only into the Editor-only
  assembly) has been **moved to `Runtime/Tools/PrefabLightmapGenerator/PrefabLightmapData.cs`**.
  Confirmed via glob — the file now lives under `Runtime/`, so it will exist in player builds as
  intended by its own doc comment ("applies stored lightmap data when the prefab is instantiated").
  `Documentation~/ARCHITECTURE.md`'s "Editor tooling" table (now line 298) documents the split
  explicitly: *"the `PrefabLightmapData` `MonoBehaviour` compiles into player builds; only the
  `Lightmapping.Bake()`/`PrefabUtility`-dependent baking pipeline (`PrefabLightmapBaker`) and its
  custom inspector are editor-only."* This implements Fix #1 from the original audit exactly as
  suggested (structural move, not a logic change).
- No other findings — unchanged.

### Severity Semantics

- **RESOLVED** — `Editor/Validation/UI/UIValidationPolicy.cs`'s `GetRolloutMode()` now defaults to
  `UIValidationRolloutMode.Strict`: `EditorPrefs.GetInt(RolloutModePrefKey, (int)
  UIValidationRolloutMode.Strict)`, and the fallback for an invalid stored value also returns `Strict`.
  Previously the default (and invalid-value fallback) was `WarningFirst`, which silently downgraded
  every rule except `UIV000` to Warning regardless of how it was coded in `UIValidationEngine.cs`. Now
  declared severities (`AddError`/`UIValidationSeverity.Error` for `ScriptOutsideMappedFolders`,
  `PanelRootReferencesOrchestration`, etc.) take effect out of the box for anyone who hasn't touched the
  rollout-mode setting, closing the "per-machine EditorPrefs setting instead of a stable property of the
  issue class" gap. Implements Fix #2's first option ("default to Strict").

### Master List / Manifest Machinery
No findings — unchanged. `HomamGecOrphanDefineCleaner.cs` still narrowly scoped; `DataPathPolicy`
still the package's real, read-only contribution to the cross-package manifest system.

### Ownership
No findings of competing/duplicate pipelines — unchanged.

### Redundancy/Simplification

- **RESOLVED** — Duplicated path-under-root normalization logic. `HierarchyPathPolicy.IsPathUnderRoot`
  (`Editor/Validation/DataPathPolicy/HierarchyPathPolicy.cs:40`) and `UIValidationConventions.IsPathUnder`
  (`Editor/Validation/UI/UIValidationConventions.cs:109`) both now delegate to a single shared
  `PathComparisonUtility.IsPathUnder(path, root)` helper instead of each implementing the
  backslash-normalize/trim/`StartsWith` logic independently. Implements Fix #4 exactly as suggested.
- **RESOLVED** — Dead `EditorWindow` inheritance. `Tools/Weaver.cs` is now `public static class Weaver`
  (confirmed via grep for `class Weaver`), matching its siblings (`CodeAwareRename`, `Package2Folder`).
  `Documentation~/ARCHITECTURE.md`'s tooling table now documents this explicitly: *"Weaver |
  `Editor/Tools/Weaver.cs` | Constant / package rebuild utilities (plain `static class`, not an
  `EditorWindow`)"*. Implements Fix #5.
- **Not fixed** — Two windows still solve the same "generate a code snippet for the user" problem
  without sharing infrastructure. `Windows/AutoBinderWindow.cs` still has no
  `ScriptGeneratorWindow`/clipboard-copy integration (grepped for `ScriptGeneratorWindow`/
  `CopyToClipboard` in `AutoBinderWindow.cs` — no matches). Unchanged from original audit, low priority.
- **RESOLVED (removed)** — `Tools/DownloadSoundFromStoryBlock.cs`, flagged in the original audit as a
  personal-convenience utility with questionable fit for "Base platform layer" tooling, **no longer
  exists** in the package (confirmed via `ls` — file not found). Implements Fix #7 by removing rather
  than relocating, and its absence from `Documentation~/ARCHITECTURE.md`'s tooling table is therefore
  correct, not drift.

### Doc/Architecture Drift

- **PARTIALLY FIXED** — `Documentation~/ARCHITECTURE.md`'s "Editor tooling" table has been updated for
  the two tools whose behavior changed this pass (Weaver, Prefab Lightmap Generator — both now
  accurately describe the current split/class-kind), but the broader gap from the original audit is
  still open: `Windows/AutoBinderWindow.cs`, `Windows/ScriptGeneratorWindow.cs`,
  `Tools/MonoBehaviourScriptDuplicator.cs` (+ `ScriptReplacerWindow`), `Tools/CodeAwareRename.cs`,
  `Tools/ClipboardToScript.cs`, `Tools/CameraScreenshot.cs`, `Tools/Package2Folder.cs`,
  `Tools/EditorGUIX_ImageStringConverter/*`, `Validation/MenuItemDuplicatePathValidator.cs`,
  `Validation/DataPathPolicy/*`, and the entire `Editor/PackageIntegration/` folder are still absent
  from the table (grepped the table for each name — no matches). `DownloadSoundFromStoryBlock.cs`'s
  removal from the table is correct now that the file itself is gone (see Redundancy above), not part
  of this remaining gap.
- **Resolved (moot)** — The cross-reference note about `PrefabLightmapData`'s doc comment contradicting
  its Editor-only placement is resolved along with the underlying move.
- **Unchanged** — Folder-name collision between this package's `Editor/PackageIntegration/` and
  GameEngineCore's unrelated folder of the same name. Not a functional conflict, not fixed, not urgent.

### Codebase Gotchas

- **RESOLVED** — `Tools/PrefabLightmapGenerator/LightmapConfiguration.cs`'s `OnValidate()`
  (now lines 127-134) is guarded with inequality checks exactly as recommended:
  ```csharp
  private void OnValidate()
  {
      // Ensure values are within valid ranges. Guarded so a no-op OnValidate (values already
      // in range) doesn't dirty the asset on every domain reload/scene open.
      if (maxLightmapsPerBatch < 1) maxLightmapsPerBatch = 1;
      if (maxShaderCacheSize < 1) maxShaderCacheSize = 1;
      if (maxRenderersWarningThreshold < 1) maxRenderersWarningThreshold = 1;
  }
  ```
  The comment explicitly names the pattern being avoided (dirtying the asset on a no-op fire),
  confirming this was a deliberate fix, not incidental. Implements Fix #3 exactly as suggested.
- Other Codebase Gotchas checks (`??` on `UnityEngine.Object`, struct-rule violations, `Debug`
  namespace collision not applicable to this package) — unchanged, still no findings.

## Fixes

Status of the original priority list:

1. ~~Move `PrefabLightmapData.cs` out of `Editor/` into `Runtime/`~~ — **DONE**.
2. ~~Make `UIValidationPolicy`'s default rollout mode intentional~~ — **DONE** (defaults to `Strict`).
3. ~~Guard `LightmapConfiguration.OnValidate()` writes with inequality checks~~ — **DONE**.
4. ~~Extract the duplicated path-under-root helper~~ — **DONE** (`PathComparisonUtility.IsPathUnder`).
5. ~~Drop the unused `EditorWindow` base from `Weaver`~~ — **DONE**.
6. **Still open** — Update `Documentation~/ARCHITECTURE.md`'s "Editor tooling" table to list the
   currently-undocumented tools. Two entries were updated as a side effect of fixes #1 and #5, but the
   broader list from the original audit (AutoBinderWindow, ScriptGeneratorWindow,
   MonoBehaviourScriptDuplicator, CodeAwareRename, ClipboardToScript, CameraScreenshot, Package2Folder,
   EditorGUIX_ImageStringConverter, MenuItemDuplicatePathValidator, DataPathPolicy, PackageIntegration)
   remains undocumented.
7. ~~Confirm with the user whether `DownloadSoundFromStoryBlock.cs` should stay or be
   relocated/removed~~ — **DONE** (removed).

## Cross-references

- `docs/00-AgentGuide.md` §3 — OnValidate dirty-write trap (now correctly applied in
  `LightmapConfiguration.cs`), assembly boundaries.
- `docs/01-CorePrinciples.md` — Validation boundaries.
- `docs/03-Frameworks.md` — Per-package `ARCHITECTURE.md` symbol-level detail.
- `docs/04-DataAndDomains.md` — Generated domains / ensure-semantics.
- `docs/09-EditorHub.md` — Surface priority #6 (editor windows, last resort).
- `docs/13-AuthoringStandards.md` — Severity semantics (now correctly enforced by default via
  `UIValidationPolicy`'s `Strict` default).
- `Packages/com.aethernexus.foundationplatform/Documentation~/ARCHITECTURE.md` — "Editor tooling" table
  (partially updated; see Doc/Architecture Drift above for what's still missing) and assembly
  definitions section.
- `Packages/com.aethernexus.gameenginecore/Editor/PackageIntegration/PackageIntegrationManifest.cs` and
  `Editor/CentralAuthoring/CentralWindow.cs` — out of this package's scope, referenced only for scope
  clarification.
