# AetherInspector — Architecture Audit

## Context

AetherInspector is FoundationPlatform's in-house, attribute-driven Inspector engine (Odin-style
attribute surface, IMGUI renderer). It owns:

- The attribute set (`Runtime/AetherInspector/AetherInspectorInspectorAttributes.cs`): `[BoxGroup]`,
  `[ShowIf]`, `[Required]`, `[ValidateInput]`, `[Button]`, `[ListDrawerSettings]`, `[TableList]`,
  `[ValueDropdown]`, `[InlineEditor]`, etc. — pure attribute declarations, no logic, referenceable
  from runtime assemblies.
- The rendering engine (`Editor/AetherInspector/`): `AetherInspectorEditor` (base `Editor`),
  `AetherInspectorFallbackEditor` (global fallback via `isFallback = true`),
  `AetherInspectorRenderer` (reflection/metadata + IMGUI layout engine), `AetherInspectorReflectedDrawer`
  (nested `[Serializable]` `PropertyDrawer` base), `AetherInspectorTheme`/`GuiKit` (chrome), plus
  supporting drawers (`EngineListDrawer`, `EngineDictionaryDrawer`, `TableRenderer`,
  `InspectorDropdown`, `PocoInspector`, `ObjectFieldX`, `ObjectSelectorPopupX`,
  `MissingScriptFixer`, `PlayModeValuesSaver`, `ComponentContextMenus`, `UnityEventDropTarget`,
  `InspectorXSettings`/`InspectorXSettingsProvider`, `AetherInspectorDemoWindow`).
- `Editor/Drawers/` (`LayerDrawer.cs`, `TagDrawer.cs`, `TooltipIconDrawer.cs`) is a **separate**,
  unrelated subsystem — classic per-attribute `PropertyDrawer`s for
  `AetherNexus.FoundationPlatform.Attributes` (`[Layer]`, `[Tag]`, `[TooltipIcon]`, defined in
  `Runtime/Attributes/`), not AetherInspector's own attribute set. Confirmed unrelated; not audited
  further here (see Cross-references).

Governing docs: `docs/02-Libraries.md` ("editor drawers and validation helpers" is explicitly listed
as something FoundationPlatform owns), `docs/09-EditorHub.md` / `docs/13-AuthoringStandards.md`
(designer surface priority, shared section order, severity/action vocabulary), `docs/00-AgentGuide.md`
§2/§3 (Framework.Inspector preference, struct rules, `??`/reflection/silent-fallback rules),
`AGENTS.md` ("Framework.Inspector attributes for inspectors", "`[Required]`/`[ValidateInput]`
... for serialized dependency contracts").

**`Documentation~/ARCHITECTURE.md` exists and mentions this subsystem — but is not accurate.**
It describes a "Framework Inspector" tool under namespace
`AetherNexus.FoundationPlatform.FrameworkInspector[.Editor]`, folders `Runtime/FrameworkInspector/` /
`Editor/FrameworkInspector/`, and classes `FrameworkEditor`, `FrameworkFallbackEditor`,
`FrameworkInspectorRenderer`, `FrameworkInspectorTheme`, `FrameworkReflectedDrawer`. None of these
folders/namespaces/classes exist. The real code is `Runtime/AetherInspector/` /
`Editor/AetherInspector/`, namespace `AetherNexus.FoundationPlatform.AetherInspector[.Editor]`,
classes `AetherInspectorEditor`, `AetherInspectorFallbackEditor`, `AetherInspectorRenderer`,
`AetherInspectorTheme`, `AetherInspectorReflectedDrawer`. See Doc/Architecture Drift below for the
full breakdown — this is the single biggest finding in this audit.

## Findings

### Execution Spine

No findings. Every file in scope is `#if UNITY_EDITOR`-gated and lives in the `FoundationPlatform.Editor`
asmdef (`includePlatforms: [Editor]`). Nothing here writes gameplay/simulation state; the engine only
reads/writes `SerializedObject`/`SerializedProperty` and POCO instances for editor presentation. Fully
compliant with the single-execution-spine rule (docs/01, docs/00 §1).

### Data/Controller/View Boundary

No findings — not applicable in the docs/06 sense. This subsystem is developer tooling (an inspector
*renderer*), not a gameplay Data/Controller/View triad. `Runtime/AetherInspector/` is correctly kept to
attribute declarations only (safe for any runtime assembly to reference); all reflection/IMGUI logic is
correctly isolated to the `Editor` assembly. This split is itself a correct application of the
data/logic separation principle at the tooling level.

### Ownership

No findings. `InspectorXSettings` is a single `ScriptableSingleton<InspectorXSettings>`
(`Editor/AetherInspector/InspectorXSettings.cs:13`) with one persisted asset path
(`ProjectSettings/AetherInspectorXSettings.asset`) — no duplicate settings singleton found.
`AetherInspectorEditor` (concrete, per-type) and `AetherInspectorFallbackEditor` (`isFallback = true`,
global) are a deliberate one-base/one-fallback composition, not two competing owners — Unity's own
priority rules keep a hand-written `[CustomEditor]` in charge when one exists
(`Editor/AetherInspector/AetherInspectorFallbackEditor.cs:13-15`).

### Designer Surface Priority

No findings against docs/09/docs/13's surface-priority ordering. AetherInspector is *infrastructure*
that other surfaces (ProjectWindowX, HierarchyX — separate asmdefs, not in this scope) build on top
of; it does not itself try to reimplement Project/Hierarchy authoring flows.

- `AetherInspectorDemoWindow` (`Tools ▸ Diagnostics ▸ AetherInspector Demo`,
  `Editor/AetherInspector/AetherInspectorDemoWindow.cs:25`) is an `EditorWindow`, but it is exactly
  the kind the docs carve out as acceptable — a debug/visual-regression harness, last resort by
  design, not a designer daily-use surface.
- `InspectorXSettingsProvider` correctly uses a Project Settings provider
  (`SettingsProvider("Project/AetherInspector", ...)`,
  `Editor/AetherInspector/InspectorXSettingsProvider.cs:16`) rather than inventing a bespoke
  `EditorWindow` for its own configuration — matches Unity convention and docs/13's "prefer
  Project/Hierarchy/Inspector over a new EditorWindow" spirit as closely as project-settings UI can.
- `ObjectSelectorPopupX` (`Editor/AetherInspector/ObjectSelectorPopupX.cs`) is a right-click,
  type-filtered object picker (search field + Scene/Assets sections) opened from a single field.
  **Info** — docs/13's "Unique-only custom UI" rule explicitly bans "a second asset browser." This
  popup is scoped (one field, type-filtered, opt-in via `InspectorXSettings.objectFieldSelector`)
  rather than a general browser, and it duplicates functionality Unity's own object-picker ("o"
  button) already provides. Low risk, but worth a note since it's the closest thing in this scope to
  the exact pattern the doc calls out.

### Redundancy/Simplification

- **Warning** — `Editor/AetherInspector/InspectorXSettingsProvider.cs:57-59` and `:67-70`: the
  "Nested Drawers" section (label + `maxNestedDepth` `PropertyField` + explanatory `HelpBox`) is
  drawn **twice** in the same `OnGUI`, once inside the `EditorGUI.BeginChangeCheck()/EndChangeCheck()`
  block that calls `ApplyModifiedPropertiesWithoutUndo()` + `SaveNow()`, and once again immediately
  after that block closes (with a slightly reworded tooltip and an extra `HelpBox`). This reads as a
  copy-paste leftover, not an intentional double control. **Why this matters**: because the second
  copy is outside the `BeginChangeCheck/EndChangeCheck` scope, editing `maxNestedDepth` through the
  *second* slider updates the `SerializedProperty` in memory but never triggers
  `ApplyModifiedPropertiesWithoutUndo()`/`SaveNow()` for that edit in the same repaint — the change is
  effectively lost until some other field on the page is also touched. It is also a plain duplicate
  in the rendered UI (docs/13 shared workflow section order expects one Advanced/settings section, not
  a repeated one).
- **Info** — `Runtime/AetherInspector/AetherInspectorInspectorAttributes.cs:62-63`:
  `ButtonAttribute.Stretch` is annotated `/// Legacy flag; when true the button stretches to fill its
  container.` in its own XML doc, yet it is still read and branched on in
  `Editor/AetherInspector/AetherInspectorEditor.cs:2690` (`if (b.Stretch && b.ButtonAlignment ==
  ButtonAlignment.Stretch)`). A self-documented legacy flag that is still load-bearing is worth
  consolidating into `ButtonAlignment` alone rather than carrying both.
- **Info** — `Editor/AetherInspector/AetherInspectorTheme.cs:334-335`:
  `SectionFoldoutTitle` is an explicit "Back-compat alias" for `FlatHeaderLabel`. Documented shim,
  functioning as intended, but a candidate for removal once call sites are confirmed migrated.
- **Info** — `Editor/AetherInspector/AetherInspectorTheme.cs:798-806`: `FoldoutInSection(bool/GUIContent)`
  now just forwards to `SectionFoldout(...)` with no behavioral difference — the two APIs are
  currently identical; one is a dead alias (its own doc comment implies they used to differ:
  "`EditorStyles.foldoutHeader` overlaps box borders; use this instead").
- **Info** — `Editor/Drawers/LayerDrawer.cs` and `Editor/Drawers/TagDrawer.cs` are near line-for-line
  duplicate `PropertyDrawer`s (only `UnityEditorInternal.InternalEditorUtility.layers` vs `.tags`, and
  `LayerAttribute` vs `TagAttribute`, differ) — a single generic "pick from a string array" drawer
  would remove the duplication. Out of primary scope (belongs to the `Attributes` subsystem, not
  AetherInspector), noted for completeness per the audit instructions to check `Editor/Drawers/`.
- Widespread empty `catch { }` / `catch { return null; }` blocks throughout the reflection call sites
  (e.g. `AetherInspectorEditor.cs` `SafeGet` (~line 1900-1903), `RunInitHooks`'s inline invoke
  (~line 1563), `SetMemberValue` (~line 1257), `IsEntryReferenceMissing` (~line 1301-1303);
  `PocoInspector.cs` `ReadValue`/property-set (~lines 413, 466); `AetherInspectorReflectedDrawer.cs`
  `Read()`/`SafeBoxed()` (~lines 202, 238)). **Info, not Error** — this is defensible IMGUI-robustness
  (one bad reflected member must not corrupt the GUILayout stack for the rest of the inspector), and
  the project's "no silent fallback" rule is aimed at simulation/authoritative-data paths, not editor
  presentation robustness. Flagged only because it is pervasive enough that a broken `[Button]`/
  `[OnValueChanged]` hook can fail with zero console output through some of these specific call sites,
  unlike the sibling paths that do log (`InvokeAction`, `InvokeButton`, `InvokeChangeAction`,
  `CustomAddFunction` hooks, etc., which all call `Debug.LogWarning`/`Debug.LogError`).

### Determinism

No findings. Grepped the full scope for `UnityEngine.Random`, `WaitForSeconds`, `Time.deltaTime`,
`Time.time` — zero hits. Correct: this subsystem is presentation/tooling only and correctly sits
outside the simulation spine; determinism rules don't apply here and nothing leaks in.

### Doc/Architecture Drift

**Warning (would actively mislead a developer/agent who follows it as written).**

1. `Documentation~/ARCHITECTURE.md:5,7,220-238` — describes this subsystem as "Framework Inspector"
   under namespace `AetherNexus.FoundationPlatform.FrameworkInspector[.Editor]`, folders
   `Runtime/FrameworkInspector/` / `Editor/FrameworkInspector/`, and classes `FrameworkEditor`,
   `FrameworkFallbackEditor`, `FrameworkInspectorRenderer`, `FrameworkInspectorTheme`,
   `FrameworkReflectedDrawer`. **None of these exist.** The sample code at
   `ARCHITECTURE.md:220-238` (`[CustomEditor(typeof(MyType))] class MyTypeEditor : FrameworkEditor`)
   would not compile — the actual base class is `AetherInspectorEditor`
   (`Editor/AetherInspector/AetherInspectorEditor.cs:20`).
2. `Documentation~/AetherInspector.md` — the doc file whose *name* matches the real subsystem still
   opens with `# Framework Inspector` and repeats the same stale namespace/folder/class names
   throughout (`AetherNexus.FoundationPlatform.FrameworkInspector.Editor`,
   `Editor/FrameworkInspector/`, `FrameworkEditor`, `FrameworkFallbackEditor`, `FrameworkReflectedDrawer`).
   Its attribute support matrix (spot-checked against `AetherInspectorInspectorAttributes.cs` and
   `AetherInspectorEditor.cs`) is largely accurate content-wise — only the surrounding
   namespace/class/folder prose is stale, so the fix is a rename pass, not a rewrite.
3. `Documentation~/index.md:10` — links to `[FrameworkInspector.md](FrameworkInspector.md)`, a dead
   link; the real file is `AetherInspector.md` in the same folder.
4. `Documentation~/ARCHITECTURE.md:238` — links to `[FrameworkInspector.md](../DOCS/FrameworkInspector.md)`,
   doubly wrong: no `DOCS/` folder exists anywhere in the package (docs live in `Documentation~/`), and
   the filename is also wrong.
5. `Editor/AetherInspector/AetherInspectorDemoWindow.cs:427` — a runtime string shown in the demo data
   (`"see DOCS/AetherInspector.md"`) repeats the same nonexistent `DOCS/` path; should read
   `Documentation~/AetherInspector.md`.

**Why this matters**: `docs/00-AgentGuide.md` §2 and the pre-flight checklist require updating the
matching `docs/NN-*.md` or package `ARCHITECTURE.md` in the same task as any contract/naming change —
this rename was never propagated. Any agent or developer following "read `ARCHITECTURE.md` first"
(as this very audit's brief instructs) will look for symbols that don't exist and, if they copy the
sample code, will get a compile error.

### Codebase Gotchas (docs/00 §3)

- **`??` on `UnityEngine.Object`-typed operands**: grepped every `??`/`??=` in scope. One arguable hit:
  `Editor/AetherInspector/AetherInspectorEditor.cs:2069` —
  `var tex = AssetPreview.GetAssetPreview(obj) ?? AssetPreview.GetMiniThumbnail(obj);` — both sides
  return `Texture2D` (a `UnityEngine.Object` subtype). **Info** — `??`/`?.` do not route through
  `UnityEngine.Object`'s overloaded `==`, so this is technically the forbidden pattern; practical risk
  is low here because `AssetPreview` returns either a live texture or a genuine C# `null` (not a
  destroyed-but-alive "fake null" object), so the two representations of "no value" coincide in
  practice. Still worth a rewrite to an explicit `!= null` check for consistency with the rule. All
  other `??` occurrences in scope are on `string`, `GUIContent`, plain attribute objects, or
  null-conditional (`?.`) chains on non-`UnityEngine.Object` types — no other hits.
- **`OnValidate`/`OnAfterDeserialize` unconditional writes**: none found. Grepped the full scope —
  zero occurrences of either callback. Clean (expected: this scope has no `MonoBehaviour`/
  `ScriptableObject` runtime components with serialized-field auto-wiring; `AetherInspectorDemoData`
  in the demo window is a plain `ScriptableObject` with no lifecycle callbacks).
- **Struct instance-field initializers / missing ctor assignments (C# 9)**: only one `struct`
  declared in scope — `ValueDropdownItem<T>`
  (`Runtime/AetherInspector/AetherInspectorInspectorAttributes.cs:492-502`), `readonly struct` with no
  field initializers and both fields assigned in its single explicit constructor. Compliant.
- **`Debug` namespace collision in `GameEngineCore.*`**: not applicable — no namespace in this scope
  starts with `GameEngineCore.` (FoundationPlatform cannot reference `GameEngineCore.Runtime` per its
  own `ARCHITECTURE.md`), and every `Debug.Log*`/`Debug.LogWarning`/`Debug.LogError` call in scope
  correctly resolves to `UnityEngine.Debug`.
- **Runtime reflection outside editor tools**: all reflection in scope (`InspectorMemberResolver`,
  `AetherInspectorRenderer`, `PocoInspector`, `TableRenderer`, etc.) lives in the `FoundationPlatform.Editor`
  asmdef (`includePlatforms: [Editor]`) and is further gated by `#if UNITY_EDITOR`. Correctly scoped;
  no violation.

## Fixes

Priority order:

1. **(High)** Fix the doc/architecture drift: rewrite `Documentation~/ARCHITECTURE.md`'s
   "Framework Inspector" section (lines ~5, 7, 216-238) and the entire content of
   `Documentation~/AetherInspector.md` to use the real names (`AetherInspector`/`AetherInspectorEditor`/
   `AetherInspectorFallbackEditor`/`AetherInspectorRenderer`/`AetherInspectorTheme`/
   `AetherInspectorReflectedDrawer`, folders `Runtime/AetherInspector/` / `Editor/AetherInspector/`).
   Fix the dead link in `Documentation~/index.md:10` and the `../DOCS/FrameworkInspector.md` link in
   `ARCHITECTURE.md:238` to point at `AetherInspector.md`. Fix the stray `DOCS/AetherInspector.md`
   string in `AetherInspectorDemoWindow.cs:427` to `Documentation~/AetherInspector.md`. Run
   `graphify update .` afterward per project convention.
2. **(Medium)** Fix the duplicate "Nested Drawers" block in `InspectorXSettingsProvider.cs` (delete
   the second copy at lines 67-70, or move the whole section after the `EndChangeCheck` and wrap it in
   its own change-check) so `maxNestedDepth` edits always persist.
3. **(Low)** Replace the `??` at `AetherInspectorEditor.cs:2069` with an explicit
   `AssetPreview.GetAssetPreview(obj) != null ? AssetPreview.GetAssetPreview(obj) :
   AssetPreview.GetMiniThumbnail(obj)`-style check (or a small null-safe helper) for consistency with
   the project's `UnityEngine.Object` null-check rule.
4. **(Low, optional)** Consolidate `ButtonAttribute.Stretch` into `ButtonAlignment` (drop the legacy
   flag) and collapse `FoldoutInSection`/`SectionFoldoutTitle` into their non-alias equivalents once
   call sites are confirmed migrated — cosmetic cleanup, not urgent.
5. **(Low, optional, out of this package's authoring flow)** Consider merging `LayerDrawer.cs` and
   `TagDrawer.cs` in `Editor/Drawers/` into one generic string-array-popup drawer — belongs to the
   `Attributes` subsystem's own cleanup, not AetherInspector's.

None of these were applied — audit only, per instructions.

## Cross-references

- `Editor/Drawers/LayerDrawer.cs`, `TagDrawer.cs`, `TooltipIconDrawer.cs` belong to the
  `AetherNexus.FoundationPlatform.Attributes` subsystem (`Runtime/Attributes/`), not AetherInspector —
  a separate, older, one-`PropertyDrawer`-per-attribute mechanism that predates/parallels
  AetherInspector's own richer attribute engine. Two parallel "attribute → editor UI" mechanisms exist
  in the same package; not a hard ownership violation (they don't manage the same fact), but a
  candidate for a future consolidation review of that subsystem specifically.
  `TooltipIconDrawer.cs` also depends on `AuthoringUxShared.DrawTooltipIcon`, which lives outside this
  scope.
- `Editor/EditorEnhancerX/`, `Editor/HierarchyX/`, `Editor/ProjectWindowX/` (separate asmdefs found
  alongside this package) are the actual Project-window/Hierarchy designer-surface tooling that
  docs/09/docs/13 rank above Inspector-level tooling. AetherInspector correctly does not duplicate
  their responsibilities — worth naming as confirmation the surface-priority separation is respected
  at the package-architecture level, not just this folder.
- `Documentation~/ARCHITECTURE.md` also references "UI Validation" (`Editor/Validation/UI/`), "Preset
  Automation" (`Editor/Tools/PresetAutomation/`), and "Entity Debugger Overlay"/"Game State window"
  (`Editor/Debugging/`) as separate FoundationPlatform-owned editor tools — outside this audit's scope,
  not reviewed here.
- `MenuPaths`/`MenuPriorities` (`AetherNexus.FoundationPlatform.Utilities.Menus`, `Runtime/Menus/`) is
  a shared menu-path registry consumed by `ComponentContextMenus.cs` and
  `AetherInspectorDemoWindow.cs` — belongs to a different subsystem; only the dependency is noted here.
