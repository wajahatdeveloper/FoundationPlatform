# AetherInspector — Architecture Audit

## Status of this re-audit

Re-audited against `git diff d714482 HEAD -- Editor/AetherInspector/ Runtime/AetherInspector/ Editor/Drawers/`
(8 commits: `a4462b9`…`693603c`). **Result: of the 5 prioritized fixes from the original audit, all 5 are now
resolved at HEAD** — but not because of the reviewed diff. Inspecting `git show d714482` shows the
*same commit that added the original `AUDIT.md`* already contained fixes for the doc drift, the
`InspectorXSettingsProvider.cs` duplicate block, the `??` on `UnityEngine.Object`, the `ButtonAttribute.Stretch`
consolidation, the `FoldoutInSection`/`SectionFoldoutTitle` alias removal, and the `Editor/Drawers/` deletion —
all bundled into `d714482` alongside the audit text itself. This directly contradicts the original
document's closing line, *"None of these were applied — audit only, per instructions."* That line was
already false the moment the commit landed. The actual `d714482..HEAD` diff reviewed for this re-audit
contains none of those fixes (they predate the diff window) — it is a separate, unrelated mechanical
pass that strips optional/default parameters into explicit overloads across
`AetherInspectorEditor.cs`, `AetherInspectorTheme.cs`, `EngineDictionaryDrawer.cs`, `EngineListDrawer.cs`,
`GuiKit.cs`, `PocoInspector.cs`, `TableRenderer.cs` (see new Finding under Codebase Gotchas below). All
call sites for the affected APIs (internal and cross-package, e.g. `com.aethernexus.gameplayabilitysystem`,
`com.aethernexus.gameframework`) were checked and use only the retained overload shapes — no compile
breakage found.

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
- `Editor/Drawers/` (`LayerDrawer.cs`, `TagDrawer.cs`, `TooltipIconDrawer.cs`) — **removed**. This
  folder existed at the time of the original audit as a separate, unrelated classic-`PropertyDrawer`
  subsystem for `AetherNexus.FoundationPlatform.Attributes` (`[Layer]`, `[Tag]`, `[TooltipIcon]`). It
  was deleted wholesale in commit `d714482`; the folder no longer exists in this package. See Finding
  status below.

Governing docs: `docs/02-Libraries.md` ("editor drawers and validation helpers" is explicitly listed
as something FoundationPlatform owns), `docs/09-EditorHub.md` / `docs/13-AuthoringStandards.md`
(designer surface priority, shared section order, severity/action vocabulary), `docs/00-AgentGuide.md`
§2/§3 (Framework.Inspector preference, struct rules, `??`/reflection/silent-fallback/optional-parameter
rules), `AGENTS.md` ("Framework.Inspector attributes for inspectors", "`[Required]`/`[ValidateInput]`
... for serialized dependency contracts").

**`Documentation~/ARCHITECTURE.md` and `Documentation~/AetherInspector.md` — FIXED.** The original audit's
single biggest finding was that these docs described a nonexistent "Framework Inspector" tool
(namespace `AetherNexus.FoundationPlatform.FrameworkInspector[.Editor]`, folders `Runtime/FrameworkInspector/`
/ `Editor/FrameworkInspector/`, classes `FrameworkEditor`/`FrameworkFallbackEditor`/etc.). As of HEAD, a
repo-wide grep for `FrameworkInspector`/`Framework Inspector`/`FrameworkEditor`/`FrameworkFallbackEditor`/
`FrameworkReflectedDrawer` inside this package returns **zero hits** outside audit documents that
describe the historical drift (this file, `Documentation~/AUDIT-CoreUtilities.md`,
`Editor/ProjectWindowX/AUDIT.md`) and changelog/readme mentions. `Documentation~/ARCHITECTURE.md:5,7,25-26,
283,300-306` and `Documentation~/AetherInspector.md` now consistently use `AetherInspector`/
`AetherInspectorEditor`/`AetherInspectorFallbackEditor`/`AetherInspectorRenderer`/`AetherInspectorTheme`/
`AetherInspectorReflectedDrawer`, folders `Runtime/AetherInspector/` / `Editor/AetherInspector/`; the
sample code in `AetherInspector.md` (`[CustomEditor(typeof(MyType))] class MyTypeEditor : AetherInspectorEditor`)
now compiles against the real base class (`Editor/AetherInspector/AetherInspectorEditor.cs:20`).
`Documentation~/index.md:10` now links to the real `AetherInspector.md`. No `../DOCS/FrameworkInspector.md`
link remains in `ARCHITECTURE.md`. See Doc/Architecture Drift below.

## Findings

### Execution Spine

No findings. Unchanged since the original audit — every file in scope is still `#if UNITY_EDITOR`-gated
and lives in the `FoundationPlatform.Editor` asmdef (`includePlatforms: [Editor]`). The `d714482..HEAD`
diff is a pure signature refactor (see Codebase Gotchas) and does not touch this.

### Data/Controller/View Boundary

No findings — unchanged, not applicable in the docs/06 sense (developer tooling, not a gameplay triad).

### Ownership

No findings. `InspectorXSettings` remains a single `ScriptableSingleton<InspectorXSettings>`
(`Editor/AetherInspector/InspectorXSettings.cs:13`), one persisted asset path. Unchanged.

### Designer Surface Priority

No findings against docs/09/docs/13's surface-priority ordering. Unchanged: `AetherInspectorDemoWindow`
remains an acceptable debug harness; `InspectorXSettingsProvider` still correctly uses a Project Settings
provider.

- `ObjectSelectorPopupX` (`Editor/AetherInspector/ObjectSelectorPopupX.cs`) — **Info, still open, not
  addressed by the reviewed diff** (file untouched between `d714482` and HEAD). Same low-risk note as
  before: a scoped, type-filtered, opt-in object picker, not a general second asset browser — worth
  keeping in mind against docs/13's "Unique-only custom UI" rule, but not urgent.

### Redundancy/Simplification

- **FIXED** — `Editor/AetherInspector/InspectorXSettingsProvider.cs`: the duplicate "Nested Drawers"
  block is gone. The file (98 lines total) now draws the section exactly once, at lines 57-60, entirely
  inside the `EditorGUI.BeginChangeCheck()` (line 43) / `EditorGUI.EndChangeCheck()` (line 62) scope that
  calls `ApplyModifiedPropertiesWithoutUndo()` + `SaveNow()` (lines 64-65). Verified via `git diff d714482
  HEAD -- Editor/AetherInspector/InspectorXSettingsProvider.cs` (empty — file unchanged since `d714482`)
  and by reading the full current file. **Caveat**: the fix was already present in commit `d714482`
  itself — `git show d714482 -- Editor/AetherInspector/InspectorXSettingsProvider.cs` shows the second
  copy (old lines 67-70) being deleted in that same commit that introduced the original audit text
  claiming "None of these were applied." `maxNestedDepth` edits now always persist correctly regardless
  of which control the user last touched.
- **FIXED** — `Runtime/AetherInspector/AetherInspectorInspectorAttributes.cs`: `ButtonAttribute.Stretch`
  (the self-documented "Legacy flag") no longer exists. Only `ButtonAlignment ButtonAlignment` remains
  (`:61`, defaulting to `ButtonAlignment.Stretch`, enum at `:85`), and the sole read site,
  `Editor/AetherInspector/AetherInspectorEditor.cs:2779`, now reads
  `if (b.ButtonAlignment == ButtonAlignment.Stretch)` — the old dual-flag (`b.Stretch &&
  b.ButtonAlignment == ButtonAlignment.Stretch`) branch is gone. Consolidation complete, no dead flag left.
- **FIXED** — `Editor/AetherInspector/AetherInspectorTheme.cs`: both `SectionFoldoutTitle` (back-compat
  alias for `FlatHeaderLabel`) and `FoldoutInSection` (identical forwarding wrapper for `SectionFoldout`)
  are gone — grepping the file for either name returns no matches. Both aliases removed cleanly; no
  remaining call sites reference the old names.
- **NO LONGER APPLICABLE** — `Editor/Drawers/LayerDrawer.cs` / `TagDrawer.cs` duplication. The entire
  `Editor/Drawers/` folder (`LayerDrawer.cs`, `TagDrawer.cs`, `TooltipIconDrawer.cs`, plus `.meta` files)
  was deleted in commit `d714482` (`git show d714482 --stat` shows `62 ----`/`61 ----`/`34 --` for the
  three files respectively). The folder does not exist at HEAD (`ls Editor/Drawers/` → no such
  directory). Whatever replaced this functionality (if anything) is out of this audit's scope to
  verify further; the specific duplication finding is moot since the code is gone.
- **NOT FIXED (Info, not urgent)** — the pervasive empty `catch { }` / `catch { return null; }` blocks
  at reflection call sites are still present in the same shape and roughly the same volume: 9 in
  `AetherInspectorEditor.cs`, 4 in `PocoInspector.cs`, 3 in `AetherInspectorReflectedDrawer.cs`, plus
  hits in `EngineListDrawer.cs`, `InspectorMemberResolver.cs`, `InspectorDropdown.cs`, `TableRenderer.cs`
  (24 occurrences across 7 files at HEAD). As before, this is defensible IMGUI-robustness rather than a
  hard violation of the project's "no silent fallback" rule (which targets simulation/authoritative-data
  paths), so it remains an Info-level note, not a required fix.

### NEW — Codebase Gotchas: optional-parameter cleanup (not in the original audit)

The original audit's "Codebase Gotchas" section checked `??` on `UnityEngine.Object`, `OnValidate`/
`OnAfterDeserialize` writes, struct-initializer rules, the `Debug` namespace collision, and runtime
reflection scoping — but never checked docs/00-AgentGuide.md's **"No optional parameters"** rule
(`docs/00-AgentGuide.md:70,181,199`), even though it applied and the scope had numerous violations at
the time (e.g. `TempContent(string text, string tooltip = null)`, `ContainerScope(GUIStyle style = null,
...)`, `BeginBox(string label = null)`, `SectionHeaderRow(..., float buttonSize = 20f)`, `DrawTitle(...,
TextAlignment textAlignment = TextAlignment.Left, ...)`, `DrawInfoBox(..., InfoMessageType type =
InfoMessageType.Info)`, `DrawValidationBox(..., InfoMessageType type = InfoMessageType.Error)`,
`TagPill(..., Action onRemove = null)`, `EngineDictionaryDrawer.Draw(..., string foldoutKey = null)`,
`LruCache(int capacity, Action<TKey,TValue> onEvict = null)`, `PocoInspector.DrawSingleMember(...,
HashSet<object> visited = null)`, `TableRenderer.DrawValueTable(..., string title = null)`). This is the
actual content of the reviewed `d714482..HEAD` diff.

**FIXED as of HEAD.** Every one of the above now has its default value removed and is split into an
explicit non-default overload that forwards to the full-argument method — e.g.
`AetherInspectorEditor.cs:9-18` (`TempContent(string text)` forwards to `TempContent(string text, string
tooltip)`), `AetherInspectorTheme.cs:58-59` / `76-77` / `95-97` / `108-109` / `127-128` / `141-142`
(`ContainerScope()`, `BeginBox()`, `SectionHeaderRow(...)` w/o `buttonSize`, `DrawTitle(title, subtitle)`,
`DrawInfoBox(message)`, `DrawValidationBox(message)`), `GuiKit.cs` (mirrors the `AetherInspectorTheme`
split for its facade methods), `EngineDictionaryDrawer.cs:26-28`, `EngineListDrawer.cs:31-33`
(`LruCache(int capacity)`), `PocoInspector.cs:41-44`, `TableRenderer.cs:113-115`.

Verified no breakage: grepped every call site of the affected public/internal APIs both inside this
package and across the wider HOMAM project (`com.aethernexus.gameplayabilitysystem`,
`com.aethernexus.gameframework`, `Editor/Windows/`, `Editor/AnimGraph/`, `Editor/Messaging/EventBus/`,
`Editor/StaleComponentGuard/`, `Editor/Debugging/`, `Editor/Tools/PresetAutomation/`). All call sites use
either the fully-defaulted-away shape (0/1 args) or the fully-specified shape (all args) — none rely on
a partial arg count that only existed via C# default parameters, so nothing fails to compile. The one
multi-optional case, `AetherInspectorRenderer.DrawNestedObject` (previously 4 required + 4 optional
params), was split into exactly two shapes — a 7-arg (`e, targets, foldouts, tabs, inline, maxDepth,
visited`) and the full 9-arg form — and both existing call-site patterns
(`AetherInspectorEditor.cs:1848,1859` use the 7-arg form; `EngineListDrawer.cs:423-425` uses the full
9-arg form with `labelOverride`/`preResolvedTargets`) match one of the two exactly. No new dead code or
unused overloads found from this pass.

### Determinism

No findings. Unchanged — no `UnityEngine.Random`/`WaitForSeconds`/`Time.deltaTime`/`Time.time` in scope.

### Doc/Architecture Drift

**FIXED.** All 5 original sub-findings resolved:

1. `Documentation~/ARCHITECTURE.md` — no longer describes "Framework Inspector"; uses the real
   namespace/folders/classes throughout (see Context section above for verification detail).
2. `Documentation~/AetherInspector.md` — opens with `# AetherInspector`, uses the real names, and its
   sample code (`class MyTypeEditor : AetherInspectorEditor`) compiles against the actual base class.
3. `Documentation~/index.md:10` — links to `[AetherInspector.md](AetherInspector.md)`; no longer a dead
   link.
4. `Documentation~/ARCHITECTURE.md` — no `../DOCS/FrameworkInspector.md` link remains.
5. `Editor/AetherInspector/AetherInspectorDemoWindow.cs:427` — `unsupportedApiNote` now reads
   `"see Documentation~/AetherInspector.md"`.

**Caveat, same as the Redundancy section above**: this fix predates the reviewed `d714482..HEAD` diff —
it was bundled into commit `d714482` alongside the original audit text, contradicting that document's
"audit only" framing. It has not regressed since; still correct at HEAD.

### Codebase Gotchas (docs/00 §3)

- **`??` on `UnityEngine.Object`-typed operands** — **FIXED.**
  `Editor/AetherInspector/AetherInspectorEditor.cs:2151-2152` now reads:
  ```csharp
  var assetPreview = AssetPreview.GetAssetPreview(obj);
  var tex = assetPreview != null ? assetPreview : AssetPreview.GetMiniThumbnail(obj);
  ```
  replacing the old `AssetPreview.GetAssetPreview(obj) ?? AssetPreview.GetMiniThumbnail(obj)`. Explicit
  `!= null` check as the original audit's Fix #3 recommended. Re-grepped the full scope for `??`/`??=`
  on `UnityEngine.Object`-typed operands — no other hits; all remaining `??`/`?.` usages in
  `AetherInspectorEditor.cs` are on `GUIContent`, `string`, `GUIStyle`, `Type`, or plain object
  references, not `UnityEngine.Object` subtypes. Same caveat as above: this fix landed in `d714482`
  itself, not in the reviewed diff, but remains correct at HEAD.
- **Optional parameters** — see the new finding above. Previously unflagged by the original audit
  despite the rule existing (`docs/00-AgentGuide.md:70`); now fixed by the reviewed diff.
- **`OnValidate`/`OnAfterDeserialize` unconditional writes**: none found. Unchanged — zero occurrences
  in scope.
- **Struct instance-field initializers / missing ctor assignments (C# 9)**: unchanged. Only
  `ValueDropdownItem<T>` (`Runtime/AetherInspector/AetherInspectorInspectorAttributes.cs`), still
  compliant.
- **`Debug` namespace collision in `GameEngineCore.*`**: not applicable, unchanged.
- **Runtime reflection outside editor tools**: unchanged, correctly scoped to the `Editor` asmdef +
  `#if UNITY_EDITOR`.

## Fixes

Priority order, current status:

1. ~~**(High)** Fix the doc/architecture drift...~~ **DONE** (landed in `d714482`, verified still
   correct at HEAD). No remaining action.
2. ~~**(Medium)** Fix the duplicate "Nested Drawers" block in `InspectorXSettingsProvider.cs`...~~
   **DONE** (landed in `d714482`, verified still correct at HEAD). No remaining action.
3. ~~**(Low)** Replace the `??` at `AetherInspectorEditor.cs:2069`...~~ **DONE** (now at
   `:2151-2152`, landed in `d714482`, verified still correct at HEAD). No remaining action.
4. ~~**(Low, optional)** Consolidate `ButtonAttribute.Stretch`... and collapse `FoldoutInSection`/
   `SectionFoldoutTitle`...~~ **DONE** (both landed in `d714482`, verified still correct at HEAD). No
   remaining action.
5. ~~**(Low, optional, out of this package's authoring flow)** Consider merging `LayerDrawer.cs` and
   `TagDrawer.cs`...~~ **MOOT** — `Editor/Drawers/` was deleted wholesale in `d714482`; nothing left to
   merge.

Remaining open items (none from the original priority list — these are Info-level notes that were
never on the required-fix list):

6. **(Info, optional)** The pervasive empty `catch { }` blocks across reflection call sites
   (`AetherInspectorEditor.cs`, `PocoInspector.cs`, `AetherInspectorReflectedDrawer.cs`,
   `EngineListDrawer.cs`, `InspectorMemberResolver.cs`, `InspectorDropdown.cs`, `TableRenderer.cs`) are
   unchanged. Still defensible IMGUI-robustness, not a required fix.
7. **(Info, optional)** `ObjectSelectorPopupX` remains a scoped, opt-in object picker that is the
   closest thing in this scope to docs/13's "no second asset browser" pattern. Unchanged, still low risk.
8. **(Housekeeping)** This document's own "None of these were applied — audit only, per instructions"
   closing line (previously at the end of the Fixes section) was inaccurate at the moment `d714482`
   was committed — the fixes were bundled into the same commit. Future audit commits should either
   apply fixes in a clearly separate follow-up commit, or update the audit text to match what the
   commit actually contains, so the document doesn't misstate its own change's scope.

## Cross-references

- `Editor/Drawers/` (`LayerDrawer.cs`, `TagDrawer.cs`, `TooltipIconDrawer.cs`) — **removed**, no longer
  a cross-reference. The `AetherNexus.FoundationPlatform.Attributes` subsystem's classic-`PropertyDrawer`
  mechanism this folder implemented no longer exists in this package as of `d714482`; if that
  functionality still exists it has moved elsewhere and is outside this audit's scope to trace further.
- `Editor/EditorEnhancerX/`, `Editor/HierarchyX/`, `Editor/ProjectWindowX/` (separate asmdefs found
  alongside this package) remain the actual Project-window/Hierarchy designer-surface tooling that
  docs/09/docs/13 rank above Inspector-level tooling. Unchanged; AetherInspector still correctly does
  not duplicate their responsibilities. Note: `Documentation~/ARCHITECTURE.md`'s ProjectWindowX/HierarchyX
  sections themselves picked up unrelated doc updates in this diff window (settings-extras registration
  notes) — outside this audit's AetherInspector scope, not reviewed further here.
- `Documentation~/ARCHITECTURE.md` also references "UI Validation" (`Editor/Validation/UI/`), "Preset
  Automation" (`Editor/Tools/PresetAutomation/`), and "Entity Debugger Overlay"/"Game State window"
  (`Editor/Debugging/`) as separate FoundationPlatform-owned editor tools — outside this audit's scope,
  not reviewed here.
- `MenuPaths`/`MenuPriorities` (`AetherNexus.FoundationPlatform.Utilities.Menus`, `Runtime/Menus/`) is
  a shared menu-path registry consumed by `ComponentContextMenus.cs` and
  `AetherInspectorDemoWindow.cs` — belongs to a different subsystem; only the dependency is noted here.
