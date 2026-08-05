# ProjectWindowX — Architecture Audit (re-audit)

## Context

Re-audit after `git diff d714482 HEAD -- Editor/ProjectWindowX/` (1071 lines), whose headline change is a
brand-new **`Editor/ProjectWindowX/Panel/`** folder (5 files: `IProjectPanelSection.cs`,
`ProjectPanelHost.cs`, `ProjectPanelWidgets.cs`, `ProjectPanelWindow.cs`,
`ProjectWindowXPanelRegistry.cs`) mirroring HierarchyX's existing docked-panel mechanism
(`Editor/HierarchyX/Panel/`), plus edits to `Passes/FolderIcons.cs`, `ProjectWindowXSettings.cs`,
`ProjectWindowXSettingsProvider.cs`, and the asmdef. All new/changed files read in full; compared
line-by-line against `Editor/HierarchyX/Panel/HierarchyXPanelRegistry.cs`, `IHierarchyPanelSection.cs`,
`HierarchyPanelHost.cs`, `HierarchyPanelWidgets.cs`, `HierarchyPanelWindow.cs`. The relevant commit is
`d115dc9` ("ProjectWindowX Update", 2026-08-03), which also touched `HierarchyX.Editor.asmdef`,
`EditorEnhancerX.Editor.asmdef`, and `FoundationPlatform.StaleComponentGuard.Editor.asmdef`.

**What changed structurally, confirmed by reading every file:**

1. **New third extension point.** ProjectWindowX now exposes **three** independent `TypeCache`-discovered
   seams instead of two: `IProjectWindowXPass` (row decoration), `IProjectWindowXContextMenu` (hover "+"
   menu) — both pre-existing — and now `IProjectPanelSection` (docked context-panel accordion sections,
   `Panel/IProjectPanelSection.cs:72-79`). `ProjectWindowXPanelRegistry` (`Panel/ProjectWindowXPanelRegistry.cs`)
   auto-discovers concrete implementations with a public parameterless constructor via
   `TypeCache.GetTypesDerivedFrom<IProjectPanelSection>()`, exactly the same discovery pattern as the two
   pre-existing registries and as `HierarchyXPanelRegistry`. Repo-wide grep for `IProjectPanelSection` finds
   exactly **one** real consumer outside the mechanism itself:
   `Packages/com.aethernexus.gameenginecore/Editor/Surfaces/Project/ContentAreasProjectPanelSection.cs`
   ("Content Areas" section, `Order => -900`). `EditorApplication.projectWindowItemOnGUI` is still
   subscribed exactly once (`ProjectWindowX.cs`) — the new panel host does not touch that hook at all; it
   is purely additive (`EditorApplication.update` polling + `projectChanged`/`selectionChanged` events). So
   this remains a single, genuine mechanism with three seams, not two competing pipelines.

2. **`FolderIcons` made public and extended for cross-window reuse.** `FolderIcons` (`Passes/FolderIcons.cs`)
   went from `internal` to `public`, gained `TryResolve(path, settings, out icon, out matchedFolderPath)`,
   a `builtinNameMap` translation table (old/deprecated icon names → current Editor icon names, e.g.
   `"Scene Icon"` → `"SceneAsset Icon"`), and `FolderIconRule` gained a new `applyToHierarchy` bool
   (`ProjectWindowXSettings.cs:30`). This is consumed by a new `Editor/HierarchyX/HierarchyXFolderIcons.cs`
   (not in this package's own diff scope, but load-bearing for one of the findings below) so a single
   Project-window folder-icon rule set can now also paint icons on matching Hierarchy rows. This required
   `HierarchyX.Editor.asmdef` to add a `"references": ["ProjectWindowX.Editor"]` entry it didn't have before
   — HierarchyX and ProjectWindowX are no longer fully decoupled assemblies.

3. **`HOMAM_GEC` `defineConstraints` gating was removed from all four gated assemblies.**
   `ProjectWindowX.Editor.asmdef`, `HierarchyX.Editor.asmdef`, `EditorEnhancerX.Editor.asmdef`, and
   `FoundationPlatform.StaleComponentGuard.Editor.asmdef` all changed from
   `"defineConstraints": ["HOMAM_GEC"]` to `"defineConstraints": []` in the same commit, while keeping the
   `versionDefines` entry that still *defines* the `HOMAM_GEC` symbol when
   `com.aethernexus.gameenginecore` is present. A repo-wide grep for `#if HOMAM_GEC` / `#elif HOMAM_GEC`
   returns **zero** hits anywhere in the package. Net effect: these four assemblies — including all 23
   ProjectWindowX files and the brand-new `Panel/` folder — now **compile unconditionally**, regardless of
   whether GameEngineCore is installed. This is very likely a deliberate decoupling (a "Foundation"
   platform package no longer needing a downstream gameplay package installed to exist at all is the more
   defensible architecture), but it directly contradicts what `Documentation~/ARCHITECTURE.md` now
   says about this package — see Doc/Architecture Drift below, this is the most important finding in this
   re-audit.

4. **`docs/09-EditorHub.md` and `docs/13-AuthoringStandards.md` were themselves updated in this pass** to
   describe exactly this mechanism: `docs/09-EditorHub.md:9` now lists, as designer-surface priority #1,
   "**Project window** (ProjectWindowX) — Twin roots `Assets/Content` / `Assets/Scripts` (CONTENT / SCRIPTS
   badges); **docked Content Areas panel** (catalog, create, disk/scripts ping, registry status chips); …"
   and `docs/09-EditorHub.md:83` says new package designer actions should contribute under
   `Editor/Surfaces/Project/` (ProjectWindowX passes/menus/**panel sections**) — which is exactly where
   GameEngineCore's `ContentAreasProjectPanelSection.cs` lives. The new Panel/ subsystem is sanctioned,
   expected, already-documented-at-the-spine-level work, not a rogue addition.

## Findings

### Designer Surface Priority

No findings — extends the original "No findings" verdict to the new Panel subsystem. `ProjectPanelHost`
(`Panel/ProjectPanelHost.cs`) reflects the internal `UnityEditor.ProjectBrowser` type and appends a single
`IMGUIContainer` footer directly to its real `rootVisualElement`, docked above Unity's own Project status
strip, spanning full window width in both one- and two-column Project Browser layouts
(`MeasureUnityStatusBarHeight`/`ReserveSpace`, `ProjectPanelHost.cs:233-292`). This is a genuine,
in-window docked panel, not a redirect. `ProjectPanelWindow` (`Panel/ProjectPanelWindow.cs`) is explicitly
a **last-resort fallback** for Unity versions where `ProjectBrowser` can't be resolved via reflection
(`DockingSupported` gate, `ProjectPanelHost.cs:137,155-166,173-181`) — it renders the exact same sections
through the same `ProjectPanelWidgets.DrawSections`, and only ever opens automatically as a fallback, never
as the primary path. This mirrors HierarchyX's already-established `HierarchyPanelHost` /
`HierarchyPanelWindow` split exactly (down to matching `[MenuItem]` priority `2200` and the
`Window/<System>/…` menu path shape), so it is not introducing a second, competing "editor window" surface
— it is the same one-mechanism/one-fallback shape already accepted for HierarchyX, now applied to
ProjectWindowX, and is exactly what `docs/09-EditorHub.md:9,83` (updated in this same pass) describes.

### Unique-Only UI Rule

No findings, extended to the Panel subsystem. `ProjectPanelWidgets` draws foldout section headers, status
chips, and a "no sections registered" placeholder — no second asset browser, path/name/icon column list, or
Ping-as-primary navigation is drawn by the mechanism itself. (GameEngineCore's `ContentAreasProjectPanelSection`
draws domain rows with a "◎ Ping" button per row, but pinging an existing Project path from a status panel
is exactly the sanctioned "disk/scripts ping" pattern `docs/09-EditorHub.md:9` calls out by name — not a
duplicate browser. That file is GameEngineCore's own audit scope, referenced here only to confirm the one
real consumer stays vocabulary/pattern-compliant.)

### Designer Vocabulary Compliance

No findings on visible text in this scope. Grepped the whole `Editor/ProjectWindowX/` tree (including all
of `Panel/`) for `Drift`, `Tier-1`, `Tier-2`, `Tier1`, `Tier2` (case-insensitive): the only hits are in
`ProjectWindowXSettings.cs`/`ProjectWindowXSettingsProvider.cs`, both pre-existing and already
docs/14-compliant (`"Out-of-Sync Badges"`, `"Out-of-Sync Badge Color/Tooltip/Icon"`, no bare "Drift" in
visible text — same as the original audit found). None of the five new Panel files, and no new setting
added by this diff, introduces old jargon. The new "Context Panel" settings block
(`ProjectWindowXSettingsProvider.cs:110-114`: "Docked Panel", "Collapsed", "Status Chips") is plain,
consistent wording.

- **Info (unchanged from original audit)** — `ProjectWindowXSettingsProvider.cs:58-62` keywords set is
  unchanged: still `{"project","folder","icon","extension","zebra","row","create","script","material",
  "shader","template","authoring","drift","badge","context","menu"}` — no `"sync"` / `"out of sync"` was
  added alongside the retained `"drift"` alias. The only related change is the new
  `ProjectWindowXSettingsExtras.CollectKeywords(keywords)` call (`ProjectWindowXSettingsProvider.cs:63`),
  which lets *consumer* packages register their own extra-settings-block titles as search keywords — a
  different, unrelated mechanism. **This specific suggested fix from the previous audit did not land.**

### Shared Action Vocabulary

No findings, unchanged verdict. The new Panel mechanism defines no Validate/Fix/Apply/Refresh workflow of
its own (mechanism-only, same as the original passes); its only new UI text ("Docked Panel", "Collapsed",
"Status Chips", the section header's "⟳ Refresh" glyph action defined *by the consumer*, not the mechanism)
is not a near-synonym for the standard vocabulary set.

### Create Menu Conventions

No findings, unchanged verdict — the Panel subsystem adds no `[CreateAssetMenu]`. It does add one new
`[MenuItem]`: `Panel/ProjectPanelWindow.cs:17`, `"Window/ProjectWindowX/Context Panel"` at priority `2200`.
This is not a create-menu (docs/13's two-segment-root / order-band rules for authored types don't apply to
Window menus), and it exactly mirrors the sibling `HierarchyPanelWindow.cs:19`,
`"Window/HierarchyX/Setup Panel"`, also at priority `2200` — same convention, same ordering band, no
inconsistency.

### Ownership

No findings on the row-decoration pipeline (still exactly one `EditorApplication.projectWindowItemOnGUI`
subscriber project-wide, unchanged). Extended check for the new panel mechanism: `ProjectWindowXPanelRegistry`
is the only place `TypeCache.GetTypesDerivedFrom<IProjectPanelSection>()` is called project-wide, and
`ContentAreasProjectPanelSection` is the only concrete implementation outside the mechanism itself — one
registry, one real consumer, no shadow discovery path.

### Redundancy/Simplification

- **Info (unchanged from original audit)** — the four built-in row-decoration passes are still each split
  into a logic static class + thin `IProjectWindowXPass` wrapper (8 files for 4 behaviors:
  `Passes/ContextActions.cs`+`ContextActionsPass.cs`, `FileExtensionLabels.cs`+`FileExtensionLabelsPass.cs`,
  `FolderIcons.cs`+`FolderIconsPass.cs`, `ZebraRows.cs`+`ZebraRowsPass.cs`). Not touched by this diff; the
  previous audit's suggested fix (collapse to one class each) has not been applied. Still low priority.

- **Info (new)** — `Editor/ProjectWindowX/Panel/*` is a near line-for-line duplicate of
  `Editor/HierarchyX/Panel/*`: `PanelChip`/`PanelAction` are byte-identical structs re-declared in both
  namespaces (compare `Panel/IProjectPanelSection.cs:26-66` to `HierarchyX/Panel/IHierarchyPanelSection.cs:6-66`),
  `ProjectWindowXPanelRegistry` is ~95% identical to `HierarchyXPanelRegistry` (same discovery loop, same
  `Changed` event, same sort-by-`Order`), and `ProjectPanelHost`/`ProjectPanelWidgets` reuse most of
  `HierarchyPanelHost`/`HierarchyPanelWidgets`'s structure (collapsed strip, header bar, `DrawStatusChips`,
  try/catch-wrapped section calls) with only the reflected window type, size constants, and (see below)
  the accordion-default behavior differing. `HierarchyPanelWidgets.cs:9-13`'s own doc comment argues this
  duplication is deliberate ("HierarchyX's asmdef has no game dependencies, so the widget kit is
  intentionally small and self-contained") — but that argument is weaker now than when it was written:
  this very diff added `"references": ["ProjectWindowX.Editor"]` to `HierarchyX.Editor.asmdef` (for
  `FolderIcons.TryResolve` reuse, finding above), so the two assemblies are no longer decoupled, and
  `PanelChip`/`PanelAction`/the registry pattern could now be sourced from one shared type instead of two
  parallel copies. Not a bug — both copies work — but it is the same "avoidable indirection" the original
  audit flagged for the passes split, now at a larger scale (5 files, ~400 lines) and with the decoupling
  rationale for keeping them separate no longer fully intact.

- **Info (new)** — Behavioral divergence from the sibling it mirrors: `HierarchyPanelWidgets.DrawSections`
  special-cases exactly one registered section (`single` flag, `HierarchyPanelWidgets.cs:130,142,277-281`) —
  a lone section always shows its body with no foldout, since there's nothing to accordion between — and,
  for 2+ sections, `NormalizeAccordion` (`HierarchyPanelWidgets.cs:194-210`) auto-expands the *first* section
  by `Order` on first run (empty `panelCollapsedSections` reads as "all expanded", then everything after the
  first gets collapsed). `ProjectPanelWidgets.DrawSections`/`NormalizeExpandedId`
  (`Panel/ProjectPanelWidgets.cs:495-566`) has no single-section special case (a lone section still renders
  a foldout and starts collapsed) and its default is the opposite: `panelExpandedSectionId = ""` means **all
  sections start collapsed**, including a lone one. Concretely: with only GameEngineCore's one "Content
  Areas" section installed, a designer opening the Project window for the first time sees a fully collapsed
  footer and must click once to see any content, whereas the equivalent single-section case in HierarchyX
  shows its body immediately. Low severity (one click), but it is an inconsistency between two panels
  built from the same template with the same design doc reference, and a future third `Panel/` clone is
  likely to copy whichever behavior it read first.

### Doc/Architecture Drift

- **Fixed** — the original audit's largest finding (`Documentation~/ARCHITECTURE.md` never mentioning
  ProjectWindowX at all) is resolved. The doc (now 329 lines, was 260) has a dedicated `## ProjectWindowX`
  section (`ARCHITECTURE.md:249-260`), a row in the "Editor tooling" table (`ARCHITECTURE.md:284`,
  "Project-window row decoration + hover-create pipeline (see section above)"), and a namespace-map entry
  (`ARCHITECTURE.md:28`). The bare (`ProjectWindowX`, no `AetherNexus.FoundationPlatform.*` prefix)
  namespace the original audit flagged as ambiguous is now explicitly called out as one of exactly two
  intentional exceptions (`ARCHITECTURE.md:28-29,36`: "`ProjectWindowX` and `HierarchyX` are the only two
  subsystems that intentionally keep a bare namespace — not an oversight … treat as a known inconsistency
  rather than a pattern to copy for new code") — this also resolves the original audit's separate
  Namespace-inconsistency Info finding.

- **Warning (new, introduced by this same update)** — the freshly-written ProjectWindowX section is now
  **factually wrong** about assembly gating. `ARCHITECTURE.md:251` states ProjectWindowX is
  `` `HOMAM_GEC`-gated (see Assembly definitions) ``, and the "Assembly definitions" section
  (`ARCHITECTURE.md:53`) claims: *"`ProjectWindowX.Editor`, `HierarchyX.Editor`, …, `EditorEnhancerX.Editor`,
  and `FoundationPlatform.StaleComponentGuard.Editor` additionally carry `defineConstraints:
  ["HOMAM_GEC"]` + `versionDefines`… they only compile when GameEngineCore is present… these four
  designer-facing subsystems silently don't exist at all in a build without GameEngineCore installed."*
  This was true when `docs/Notes/FoundationPlatform-PUBLISHING.md`/the original audit described it, but
  commit `d115dc9` (same commit that added the Panel subsystem) changed all four asmdefs'
  `"defineConstraints"` to `[]` — confirmed by reading the current `.asmdef` files and by
  `git diff d714482 HEAD -- Editor/ProjectWindowX/ProjectWindowX.Editor.asmdef`. `versionDefines` still
  *defines* the `HOMAM_GEC` symbol, but nothing in the package (`#if HOMAM_GEC` grep across the whole
  package: zero hits) gates on it anymore. **These four assemblies now compile unconditionally**, with or
  without GameEngineCore installed — the doc's central claim about this package's own gating is stale the
  moment it was written, apparently because the asmdef edit and the doc edit happened in the same commit
  without cross-checking each other. Whoever relies on `ARCHITECTURE.md:53` to reason about whether
  ProjectWindowX/HierarchyX/EditorEnhancerX/StaleComponentGuard exist in a GameEngineCore-less build will
  be wrong. (The `Editor/PackageIntegration/HomamGecOrphanDefineCleaner.cs` cleanup utility, which removes a
  stale `HOMAM_GEC` PlayerSettings symbol when GameEngineCore isn't registered, still makes sense
  independent of this — it's about *player build* defines, not editor-assembly compilation — but its own
  doc comment ("Gated FP/UIWidgets asmdefs use `defineConstraints` + `versionDefines`") is now equally
  stale for the FP half of that sentence.)

- **Warning (new)** — the new Panel subsystem itself is undocumented in `ARCHITECTURE.md`. The
  `## ProjectWindowX` section's extension-point table (`ARCHITECTURE.md:255-258`) still lists only
  `IProjectWindowXPass` and `IProjectWindowXContextMenu` — no `IProjectPanelSection` row was added, and no
  sentence mentions the docked footer, the fallback `ProjectPanelWindow`, or the `Panel/` folder at all. This
  is a visible asymmetry within the same doc: the very next section, `## Hierarchy tooling (HierarchyX)`
  (`ARCHITECTURE.md:264-266`), explicitly describes its own analogous mechanism — *"plus a docked/fallback
  setup panel (`Panel/`) hosting accordion sections"* — and lists `IHierarchyPanelSection` in its table with
  named example implementers. The one line that *was* added to the ProjectWindowX section
  (`ARCHITECTURE.md:260`, the `ProjectWindowXSettingsExtras.Register` sentence) covers only the unrelated
  settings-extension mechanism, not the panel. Per `docs/00-AgentGuide.md` §2 ("after a change that alters a
  contract, ownership, or layer boundary, update the matching … package's `ARCHITECTURE.md` … in the same
  task"), adding a third `TypeCache`-discovered extension point to a package's most significant editor
  subsystem is exactly this kind of change.

### Codebase Gotchas (docs/00-AgentGuide.md §3)

No findings — checked every new/changed file for the specific traps called out in docs/00 §3:

- **`??` on Unity objects**: only two occurrences in `Panel/`, both safe —
  `actionList?.Count ?? 0` (`Panel/ProjectPanelWidgets.cs:182`, `actionList` is a `List<PanelAction>`, not a
  `UnityEngine.Object`) and `child.name ?? string.Empty` (`Panel/ProjectPanelHost.cs:143,169`, `.name` is a
  `string` property read off a `VisualElement`, which is not `UnityEngine.Object`-derived). No `??` on the
  reflected `ProjectBrowserType`, on `EditorWindow`/`VisualElement` instances themselves, or elsewhere.
- **Struct rules (C# 9)**: `PanelChip`/`PanelAction` (`Panel/IProjectPanelSection.cs:33-66`) have no
  instance field initializers, and every explicit constructor assigns every instance field — the
  three-field ctor assigns all three directly; the two-field ctor chains to it via `: this(label, status,
  null)`, which still assigns all three through the chain. Compliant, identical in shape to the
  already-accepted `HierarchyX` versions.
- **`OnValidate`/`OnAfterDeserialize`**: none anywhere in `Panel/` — no `MonoBehaviour`/`ScriptableObject`
  type is defined there at all (the settings fields live on the pre-existing `ProjectWindowXSettings`
  `ScriptableSingleton`, not touched by any new lifecycle callback).
- **Optional parameters**: none in any new Panel API (`IProjectPanelSection`, `ProjectPanelHost`,
  `ProjectWindowXPanelRegistry` all use required parameters throughout).
- **Reflection outside editor tools**: the two reflection calls (`typeof(EditorWindow).Assembly.GetType(
  "UnityEditor.ProjectBrowser")`, `Panel/ProjectPanelHost.cs:118-119`; `Activator.CreateInstance(type)`,
  `Panel/ProjectWindowXPanelRegistry.cs:761`) are both confined to this editor-only assembly
  (`includePlatforms: ["Editor"]`), mirroring the identical, already-established pattern in
  `HierarchyPanelHost`/`HierarchyXPanelRegistry`. Consistent with "reflection only in editor tools."

### Cross-cutting bug found while reading the `FolderIcons`/`applyToHierarchy` change (new, out-of-Panel-folder but in this diff)

- **Warning (confirmed)** — `ProjectWindowXSettings.FolderIconRule` gained `applyToHierarchy`
  (`ProjectWindowXSettings.cs:30`), and its settings-list UI (`ProjectWindowXSettingsProvider.cs:182-183`)
  presents it as a per-rule opt-in with the tooltip *"Also render this icon in the Hierarchy window for
  assets from this folder"* — implying a rule with the box **unchecked** stays Project-window-only. The new
  `HierarchyX/HierarchyXFolderIcons.cs:21` consumer calls
  `FolderIcons.TryResolve(path, ProjectWindowXSettings.instance, out var icon, out var matchedFolder)` to
  decide whether to paint an icon on a Hierarchy row — but `FolderIcons.TryResolve`
  (`Passes/FolderIcons.cs:49-83`) never reads `rule.applyToHierarchy` at all; it matches purely on
  `folderPath`/`applyToChildren`. Net effect: **every** matching folder-icon rule renders in the Hierarchy
  once the separate `HierarchyXSettings.folderIcons` master toggle is on
  (`HierarchyXSettings.cs:115`, tooltip: *"Draw assigned folder icons next to hierarchy rows whose asset path
  matches a ProjectWindowX folder icon rule with 'Apply to Hierarchy' enabled"*) — the per-rule checkbox a
  designer unchecks to keep a given rule Project-only has **no effect**. This is a genuine, verifiable
  functional bug (a dead settings toggle that contradicts its own tooltip and the consuming code's own doc
  comment), not a style nit — it directly affects what the new cross-window folder-icon feature actually
  does versus what its UI promises. Fix: add an `applyToHierarchy` check to `TryResolve` (or add a second
  entry point, e.g. `TryResolveForHierarchy`, that filters on it) and have `HierarchyXFolderIcons.Draw` use
  it.

## Status of every finding in the pre-existing (original) audit

| Original finding | Status now |
|---|---|
| Designer Surface Priority — no findings | **Still holds**, extended to cover the new Panel subsystem (see above). |
| Unique-Only UI Rule — no findings | **Still holds**, extended to cover the new Panel subsystem. |
| Designer Vocabulary — visible text already compliant | **Still holds** for all pre-existing and new text. |
| Designer Vocabulary — settings-keyword `"sync"`/`"out of sync"` not added alongside `"drift"` | **Not fixed.** Keywords set is byte-identical to before; only an unrelated consumer-keyword-registration hook was added. |
| Shared Action Vocabulary — no findings | **Still holds.** |
| Create Menu Conventions — no findings | **Still holds**; the one new `[MenuItem]` (Context Panel window) isn't a create-menu and matches HierarchyX's identical convention. |
| Ownership — single `projectWindowItemOnGUI` subscriber | **Still holds**, and extended: single `IProjectPanelSection` registry too. |
| Redundancy — 4 passes split into logic+wrapper (8 files) | **Not fixed** (unchanged); still low priority. |
| Doc/Architecture Drift — `ARCHITECTURE.md` never mentions ProjectWindowX | **Fixed** — full section, table row, namespace-map entry added. |
| Doc/Architecture Drift — `HOMAM_GEC` gating undocumented | **Fixed, then re-broken**: now documented, but the documentation describes gating behavior (`defineConstraints`) that was removed from the asmdefs in the very same commit — see new Warning above. |
| Doc/Architecture Drift — bare-namespace ambiguity | **Fixed** — `ARCHITECTURE.md` now explicitly lists `ProjectWindowX`/`HierarchyX` as the two intentional bare-namespace exceptions. |
| Codebase Gotchas — no findings | **Still holds** for all pre-existing files; extended check of all 5 new Panel files also finds no violations. |

## Fixes

No files were edited (per instructions, only this AUDIT.md was written). Suggested fixes, in priority order:

1. **Correct `Documentation~/ARCHITECTURE.md`'s gating claim.** Either restore `defineConstraints:
   ["HOMAM_GEC"]` on the four asmdefs if the gating removal was accidental, or (more likely, given it looks
   deliberate and consistent across all four) update `ARCHITECTURE.md:53` and `:251,264` to state plainly
   that `ProjectWindowX.Editor`/`HierarchyX.Editor`/`EditorEnhancerX.Editor`/
   `FoundationPlatform.StaleComponentGuard.Editor` now compile **unconditionally**; that `versionDefines`
   only defines `HOMAM_GEC` for optional `#if` guards (currently unused within this package); and that the
   top-line "no dependencies on other AetherNexus gameplay packages" claim is now true in an even stronger
   sense (these subsystems no longer disappear without GameEngineCore either). Cross-check
   `docs/Notes/FoundationPlatform-PUBLISHING.md` and `docs/Notes/GameEngineCore-PUBLISHING.md`
   (§`HOMAM_GEC` define) for the same staleness — likely out of this package's own audit scope, but they
   describe the same now-inaccurate gating story.
2. **Document the Panel subsystem in `ARCHITECTURE.md`.** Add `IProjectPanelSection` to the extension-point
   table at `ARCHITECTURE.md:255-258` and a sentence describing the docked footer + `ProjectPanelWindow`
   fallback, matching the treatment already given to HierarchyX's `Panel/` one section below it
   (`ARCHITECTURE.md:264-266`).
3. **Fix the `applyToHierarchy` dead toggle.** Make `FolderIcons.TryResolve` (or a variant used by
   `HierarchyXFolderIcons.Draw`) actually check `rule.applyToHierarchy` before returning a match for the
   Hierarchy-side caller, so the per-rule checkbox does what its tooltip says.
4. Add `"sync"` / `"out of sync"` to the settings-provider search keywords
   (`ProjectWindowXSettingsProvider.cs:58-62`) alongside the retained `"drift"` alias — carried over unfixed
   from the previous audit.
5. Optional/low priority, carried over unfixed: collapse the four logic-class + wrapper-class pass pairs
   into single classes implementing `IProjectWindowXPass` directly.
6. Optional: align `ProjectPanelWidgets`' accordion default/single-section behavior with
   `HierarchyPanelWidgets`' (auto-expand the first/only section on first run) so the two sibling panels
   behave identically out of the box, or explicitly document why ProjectWindowX's panel intentionally
   starts fully collapsed.
7. Optional: now that `HierarchyX.Editor` references `ProjectWindowX.Editor` for `FolderIcons` reuse, revisit
   whether `PanelChip`/`PanelAction`/the registry pattern should be defined once and shared rather than
   duplicated across `Editor/ProjectWindowX/Panel/` and `Editor/HierarchyX/Panel/`.

## Cross-references

- `docs/09-EditorHub.md` (updated in this pass) — line 9 names the "docked Content Areas panel" as designer
  surface priority #1; line 83 names `Editor/Surfaces/Project/` (ProjectWindowX passes/menus/**panel
  sections**) as where new package designer actions should live — basis for confirming the new Panel
  subsystem is sanctioned, expected work, not a redirect.
- `docs/13-AuthoringStandards.md` — shared workflow section order, severity semantics, shared action
  vocabulary, create-menu roots/order bands — basis for the Shared Action Vocabulary / Create Menu
  Conventions sections; the Panel mechanism itself is out of scope for the section-order/vocabulary rules
  (mechanism-only, like the pre-existing passes), which apply to its one real consumer
  (`ContentAreasProjectPanelSection.cs`, GameEngineCore's own audit scope).
- `docs/14-DesignerVocabulary.md` — term-mapping table ("Drift" → "Out of Sync" etc.) — basis for the
  Designer Vocabulary Compliance section; confirmed clean across all new Panel files and the one real
  consumer.
- `docs/00-AgentGuide.md` §2 — "update the matching … `ARCHITECTURE.md` … in the same task" — basis for both
  Doc/Architecture Drift warnings (the stale gating claim, and the undocumented Panel subsystem).
- `docs/00-AgentGuide.md` §3 — codebase gotchas (`??` on Unity objects, struct rules, `OnValidate`/
  `OnAfterDeserialize`, optional params, reflection scope) — basis for the Codebase Gotchas section; all new
  Panel files checked individually against each trap.
- `Editor/HierarchyX/Panel/HierarchyXPanelRegistry.cs`, `IHierarchyPanelSection.cs`, `HierarchyPanelHost.cs`,
  `HierarchyPanelWidgets.cs`, `HierarchyPanelWindow.cs` — the sibling mechanism this diff mirrors; read in
  full for the shape comparison (discovery pattern, `[MenuItem]` convention, accordion-default behavior,
  duplication) throughout this audit.
- `Editor/HierarchyX/HierarchyXFolderIcons.cs`, `Editor/HierarchyX/HierarchyXSettings.cs` (not in this
  package's `Editor/ProjectWindowX/` diff, but the direct consumer of `FolderIcons.TryResolve` and the
  `applyToHierarchy` field added by it) — basis for the confirmed `applyToHierarchy` dead-toggle bug.
- `Packages/com.aethernexus.gameenginecore/Editor/Surfaces/Project/ContentAreasProjectPanelSection.cs` — the
  sole real consumer of `IProjectPanelSection` project-wide; read in full to confirm single-registry
  ownership and that its own vocabulary/pattern (e.g. "Content Areas", "reg ✓"/"reg ✕", ping-to-select) is
  compliant. Out of this audit's scope otherwise (GameEngineCore's own audit).
- `Editor/PackageIntegration/HomamGecOrphanDefineCleaner.cs` — PlayerSettings-symbol cleanup utility whose
  own doc comment references the now-partially-stale "Gated FP … asmdefs use `defineConstraints` +
  `versionDefines`" story for this package's four assemblies.
