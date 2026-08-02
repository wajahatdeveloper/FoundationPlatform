# ProjectWindowX — Architecture Audit

## Context

Scope: `Packages/com.aethernexus.foundationplatform/Editor/ProjectWindowX/` — all 18 non-meta files read in
full:

- `ProjectWindowX.cs`, `ProjectWindowXPassRegistry.cs`, `ProjectWindowXContextMenuRegistry.cs`,
  `ProjectWindowXSettings.cs`, `ProjectWindowXSettingsProvider.cs`
- `IProjectWindowXPass.cs`, `IProjectWindowXContextMenu.cs`
- `Passes/ContextActions.cs`, `Passes/ContextActionsPass.cs`, `Passes/FileExtensionLabels.cs`,
  `Passes/FileExtensionLabelsPass.cs`, `Passes/FolderIcons.cs`, `Passes/FolderIconsPass.cs`,
  `Passes/ZebraRows.cs`, `Passes/ZebraRowsPass.cs`
- `Actions/AudioPreview.cs`, `Actions/CreateAssetActions.cs`, `Actions/ScriptTemplates.cs`
- `ProjectWindowX.Editor.asmdef` (read for architecture/ownership context)

**What this subsystem actually is**, confirmed by reading every file: FoundationPlatform's
`ProjectWindowX` is a *mechanism-only* layer. It owns the single `EditorApplication.projectWindowItemOnGUI`
hook (`ProjectWindowX.cs`), a row-decoration pass pipeline (`IProjectWindowXPass` +
`ProjectWindowXPassRegistry`, `TypeCache`-discovered), and a hover "+" create-menu pipeline
(`IProjectWindowXContextMenu` + `ProjectWindowXContextMenuRegistry`, likewise `TypeCache`-discovered).
It ships four built-in passes (zebra rows, file-extension labels, folder icons, the hover "+" button)
plus generic Unity-primitive create actions (script/material/shader/animation/asmdef scaffolding). It
does **not** itself implement domain/mapped-type authoring (create domain, create mapped types, fix
out-of-sync, drift/out-of-sync badges) — those are contributed by consumer packages
(`Packages/com.aethernexus.gameenginecore/Editor/ProjectWindowX/AuthoringProjectWindowXConsumers.cs`,
`DomainFolderColorPass.cs`, `LevelOwnershipBadgePass.cs`) through the two registries. This split matches
docs/09's extension pattern ("contribute ProjectWindowX passes/menus") and was verified by grepping the
whole repo for `EditorApplication.projectWindowItemOnGUI`: `ProjectWindowX.cs` is the **only** subscriber
project-wide, so there is exactly one Project-window row-decoration pipeline, not two competing ones.

The entire `ProjectWindowX.Editor` assembly is gated: `defineConstraints: ["HOMAM_GEC"]` +
`versionDefines` on `com.aethernexus.gameenginecore` — it only compiles when GameEngineCore is present
as a UPM package (same gating as `HierarchyX.Editor`, `EditorEnhancerX.Editor`,
`FoundationPlatform.StaleComponentGuard.Editor`). This is a deliberate, documented Asset-Store publishing
pattern (see `docs/Notes/FoundationPlatform-PUBLISHING.md` line 56, `docs/Notes/GameEngineCore-PUBLISHING.md`
§`HOMAM_GEC` define) — not a bug — but it is not reflected in the package's own `ARCHITECTURE.md` (see
Doc/Architecture Drift below).

## Findings

### Designer Surface Priority

No findings. Within this scope, ProjectWindowX correctly stays a Project-window-native mechanism: the
hover "+" menu and row overlays operate directly on Project window rows via
`EditorApplication.projectWindowItemOnGUI`, with no redirect to an EditorWindow for anything this layer
owns. The domain-specific "create domain / create mapped types / fix out-of-sync / badges" workflows the
docs describe as the #1 designer-surface priority are correctly implemented as *consumers* of this
mechanism in GameEngineCore, not duplicated or replaced by a separate window — consistent with docs/09's
"prefer Project/Hierarchy/Inspector entry points over a new EditorWindow."

### Unique-Only UI Rule

No findings. Nothing in this scope draws a second asset browser, a path/name/icon column list, or
Ping-as-primary navigation. `FileExtensionLabels` (`Passes/FileExtensionLabels.cs:12-37`) adds a small
grey extension tag Unity doesn't already show in list mode; `ZebraRows` (`Passes/ZebraRows.cs:8-23`) is a
row tint; `FolderIcons` (`Passes/FolderIcons.cs:14-31`) overlays a custom icon on the existing row icon.
All three decorate the real Project window row rather than reimplementing any part of it.

### Designer Vocabulary Compliance

No findings on visible/user-facing text. Every designer-facing string in this scope that maps to a
docs/14 renamed term already uses the new wording:

- `ProjectWindowXSettingsProvider.cs:71` — `new GUIContent("Out-of-Sync Badges")`
- `ProjectWindowXSettingsProvider.cs:73-75` — `"Out-of-Sync Badge Color"`, `"Out-of-Sync Badge Tooltip"`,
  `"Out-of-Sync Badge Icon"`
- `ProjectWindowXSettings.cs:42` — default tooltip text `"Asset is outside its declared folder pattern"`
  (no "drift" wording)

No bare "Drift", "Tier-1 aggregate", "Tier-2 curated collection", or redundant "Registry"/"Definition"
`CreateAssetMenu` text exists anywhere in this scope (verified by grep across all 18 files).

- **Info** — `ProjectWindowXSettingsProvider.cs:24` keeps `"drift"` as a Settings-search keyword
  (`keywords = new HashSet<string> { …, "authoring", "drift", "badge", … }`) but does not also list
  `"sync"` / `"out of sync"`, the designer-facing term the pane's own labels now use. A designer typing
  the term they actually see in the UI ("out of sync") to find this settings pane via Unity's
  Project Settings search would not match. Low impact (the pane is also reachable via the `Project` →
  `ProjectWindowX` category directly), but worth a one-line addition for consistency with docs/14's
  rename intent.
  - Why it matters: docs/14 renames text so designers search/recognize the term they see; leaving only
    the old jargon as the discovery alias undercuts that for the one place (search) where the rename is
    supposed to also improve findability.

### Shared Action Vocabulary

No findings. This scope contains no Validate/Fix-common-issues/Apply-fix/Refresh-or-rebuild/Apply/Preview
workflow (that vocabulary belongs to the authoring-validation tools in GameEngineCore's Central
Authoring, out of this scope). The buttons this scope does define — `"Export..."`, `"Import..."`,
`"Reset to Defaults"` (`ProjectWindowXSettingsProvider.cs:86,91,100`) and the hover-menu create labels
(`"Folder"`, `"Script/MonoBehaviour"`, `"Material"`, `"Play Audio"`, etc., `Passes/ContextActions.cs:36-63`)
— are a different action category (settings import/export, generic asset scaffolding), not near-synonyms
for the standard vocabulary set, so the "don't invent near-synonyms" rule does not apply to them.

### Create Menu Conventions

No findings — nothing to check. This scope defines no `[MenuItem]` and no shipped `[CreateAssetMenu]`.
The only `CreateAssetMenu` occurrence is inside the *generated scaffold text* for a new ScriptableObject
script (`Actions/ScriptTemplates.cs:41-48`, `menuName = "{NAME}"`), i.e. boilerplate written into a file
the developer names afterward — not a menu path this package ships or controls, so the two-segment-root
and order-band rules don't apply to it.

### Ownership

No findings. Repo-wide grep for `EditorApplication.projectWindowItemOnGUI` returns exactly one hit —
`ProjectWindowX.cs:27` — confirming a single Project-window row-decoration pipeline. GameEngineCore's
`AuthoringProjectWindowXConsumers.cs`, `DomainFolderColorPass.cs`, and `LevelOwnershipBadgePass.cs`
correctly plug into this one pipeline via `IProjectWindowXPass` / `IProjectWindowXContextMenu` rather than
hooking the callback a second time.

### Redundancy/Simplification

- **Info** — Four built-in passes are each split into a "logic" static class plus a thin
  `IProjectWindowXPass` wrapper that does nothing but forward parameters:
  `Passes/ContextActions.cs` + `Passes/ContextActionsPass.cs`,
  `Passes/FileExtensionLabels.cs` + `Passes/FileExtensionLabelsPass.cs`,
  `Passes/FolderIcons.cs` + `Passes/FolderIconsPass.cs`,
  `Passes/ZebraRows.cs` + `Passes/ZebraRowsPass.cs`. Each wrapper's `Draw` is 2-4 lines
  (e.g. `ZebraRowsPass.Draw`, `Passes/ZebraRowsPass.cs:11-15`, is just a `listMode` guard plus one call).
  This is internally consistent across all four, but it doubles the file count (8 files for 4 behaviors)
  and is not how consumers actually implement the same interface — GameEngineCore's
  `AuthoringDriftBadgePass` and `DomainFolderContextMenu`/`TypeAuthoringContextMenu`
  (`AuthoringProjectWindowXConsumers.cs:13-61,64-131,134-199`) implement `IProjectWindowXPass` /
  `IProjectWindowXContextMenu` directly in one class each, with no separate logic class.
  - Why it matters: not a bug, but it is avoidable indirection relative to the pattern the interfaces
    were actually designed for (proven by how the real external consumer uses them) — a future pass added
    to this file set is likely to copy the heavier two-file shape by precedent, when one class would do.

### Doc/Architecture Drift

- **Warning** — `Documentation~/ARCHITECTURE.md` never mentions `ProjectWindowX` anywhere in its 260
  lines — not in the namespace map (lines 11-32), not in the "Editor tooling" table (lines 216-231, which
  lists Framework Inspector, DebugX Console, Event Bus windows, Tween Debugger, UI Validation, Preset
  Automation, Entity Debugger Overlay, Game State window, Scene Switcher, Weaver, Prefab Lightmap
  Generator — no ProjectWindowX row), and not in the "Key design decisions" section. This is the single
  largest editor subsystem omission in the doc: 18 files, two `TypeCache`-discovered extension points that
  GameEngineCore builds its entire Project-window domain-authoring UX on top of, and — per docs/09 —
  the #1-ranked designer surface in the whole project. `docs/00-AgentGuide.md` §2 requires updating the
  package's `ARCHITECTURE.md` "after a change that alters a contract, ownership, or layer boundary" — a
  two-registry extension pipeline this central was never added.
  - Also missing from the doc: the `HOMAM_GEC` conditional-compile gating on this assembly (and its three
    siblings) is not mentioned in `ARCHITECTURE.md` even though it materially qualifies the doc's own
    top-line claim ("No dependencies on other AetherNexus gameplay packages", line 5) — technically still
    true (no assembly *reference* exists, gating is `versionDefines`-only) but a reader would not learn
    from `ARCHITECTURE.md` that this entire designer-facing subsystem does not exist at all in a build
    without GameEngineCore installed. That fact is correctly documented, just in
    `docs/Notes/FoundationPlatform-PUBLISHING.md` (line 56) and `docs/Notes/GameEngineCore-PUBLISHING.md`
    (§`HOMAM_GEC` define) instead of the package's own architecture reference.
  - Why it matters: an agent or developer reading only `ARCHITECTURE.md` (the documented entry point for
    "systems, namespaces, assemblies" per `Documentation~/index.md:8`) would not discover ProjectWindowX
    exists, how its two registries work, or that it silently compiles out without GameEngineCore — for
    the tool the project's own docs call the most important designer surface.

- **Info** — Namespace inconsistency: `ProjectWindowX.Editor.asmdef` declares
  `"rootNamespace": "AetherNexus.FoundationPlatform"`, but every type in this scope lives in the bare
  `namespace ProjectWindowX { … }` (e.g. `ProjectWindowX.cs:6`, `ProjectWindowXSettings.cs:6`, all
  `Passes/*.cs` and `Actions/*.cs` files), not `AetherNexus.FoundationPlatform.ProjectWindowX` or similar.
  `ARCHITECTURE.md`'s namespace map (lines 11-32) states "Most types live under
  `AetherNexus.FoundationPlatform.*`... Global APIs stay global by design" and lists only `EventBus`,
  `BaseGameEvent`, `Identity`, `CoroutineX`, tween extensions as the intentional global exceptions —
  `ProjectWindowX` is not on that list, so a reader can't tell if the bare namespace here is deliberate or
  an oversight. Low severity: `rootNamespace` only affects Unity's default namespace suggestion for
  *newly created* scripts in this folder via the Editor's Create menu, so nothing currently breaks: it
  would just seed a mismatched namespace on the next file added here through Unity's own script template.

### Codebase Gotchas (docs/00 §3)

No findings. Checked every file for the specific traps called out in docs/00 §3:

- `??` only appears on `string` operands from `Path.GetDirectoryName(...)` (`Actions/CreateAssetActions.cs:58,70,80,114`)
  — never on a `UnityEngine.Object`-derived type. Compliant.
- No `struct` declarations with instance field initializers or under-assigning constructors in this
  scope. The only `struct` text is inside string *templates* for generated shader/script boilerplate
  (`Actions/ScriptTemplates.cs:29,93-94`), not actual C# types compiled in this assembly.
- No `OnValidate` / `OnAfterDeserialize` anywhere in this scope — nothing here writes serialized fields
  from those callbacks, so the "silently dirties the scene" trap does not apply.
- `ProjectWindowXSettings` (`ProjectWindowXSettings.cs:12`) uses `ScriptableSingleton<T>` with a
  `[FilePath(...)]` attribute, the standard Unity editor-settings pattern — not a gotcha case.

## Fixes

No files were edited (per instructions, only this AUDIT.md was written). Suggested fixes for the findings
above, in priority order:

1. Add a `ProjectWindowX` row to `Documentation~/ARCHITECTURE.md`'s "Editor tooling" table (and a short
   paragraph near the namespace map) covering: the two `TypeCache`-discovered registries
   (`IProjectWindowXPass`, `IProjectWindowXContextMenu`), the four built-in passes, the hover "+"
   create-menu, the Project Settings ▸ ProjectWindowX provider, and the `HOMAM_GEC` gating (with a
   pointer to `docs/Notes/FoundationPlatform-PUBLISHING.md` for the publishing rationale rather than
   duplicating it).
2. Add `"sync"` / `"out of sync"` to the settings-provider search keywords in
   `ProjectWindowXSettingsProvider.cs:21-25` alongside the retained `"drift"` alias.
3. Optional/low priority: collapse the four logic-class + wrapper-class pass pairs into single classes
   implementing `IProjectWindowXPass` directly (matching the pattern GameEngineCore's own passes already
   use), or explicitly document why the split exists if it's intentional (e.g. reuse of the static `Draw`
   methods from elsewhere) — a quick grep did not find another caller of `ContextActions.Draw`,
   `FileExtensionLabels.Draw`, `FolderIcons.Draw`, or `ZebraRows.Draw` outside their own wrapper pass, so
   the split does not currently serve a reuse purpose.
4. Optional: either move `ProjectWindowX`'s types under `AetherNexus.FoundationPlatform.ProjectWindowX`
   for consistency with the package's stated namespace convention, or add it to
   `ARCHITECTURE.md`'s list of intentional global-namespace exceptions so the choice reads as deliberate.

## Cross-references

- `docs/09-EditorHub.md` — surface priority list (§"Surface priority (designer-first)", item 1) and
  "Unique-only inspector rule" — basis for the Designer Surface Priority / Unique-Only UI Rule sections.
- `docs/13-AuthoringStandards.md` — shared workflow section order, severity semantics, shared action
  vocabulary, create-menu roots/order bands, mapping enforcement (Mapped/Out of Sync/Unclaimed) — basis
  for the Shared Action Vocabulary and Create Menu Conventions sections.
- `docs/14-DesignerVocabulary.md` — term-mapping table ("Drift" → "Out of Sync", "Tier-1 aggregate" →
  "Master List", etc.) — basis for the Designer Vocabulary Compliance section.
- `docs/00-AgentGuide.md` §3 — codebase gotchas (`??` on Unity objects, struct rules, unconditional
  `OnValidate`/`OnAfterDeserialize` writes) — basis for the Codebase Gotchas section.
- `docs/Notes/FoundationPlatform-PUBLISHING.md` (line 56) and `docs/Notes/GameEngineCore-PUBLISHING.md`
  (§`HOMAM_GEC` define) — document the `HOMAM_GEC` asmdef gating that `ARCHITECTURE.md` omits.
- `Packages/com.aethernexus.gameenginecore/Editor/ProjectWindowX/AuthoringProjectWindowXConsumers.cs`,
  `DomainFolderColorPass.cs`, `LevelOwnershipBadgePass.cs` — the actual domain-authoring consumers of this
  package's `IProjectWindowXPass` / `IProjectWindowXContextMenu` extension points (out of this audit's
  scope; referenced only to confirm single-pipeline ownership and that consumer-side vocabulary is already
  compliant, e.g. `"Fix Out of Sync"`, `"Create Domain"`, `"Rename Domain"`, `"Delete Domain"` in
  `AuthoringProjectWindowXConsumers.cs:83,107,112,122,186`).
