# HierarchyX — Architecture Audit (re-audit after fixes)

## Context

Re-audit of the original `AUDIT.md` findings (written at commit `d714482`, "Audit") against the current
state of `Packages/com.aethernexus.foundationplatform/Editor/HierarchyX/` at `HEAD`. Scope and subsystem
description are unchanged from the original audit — see that document's Context for the full pipeline
description; this pass focuses on **what changed** (`git diff d714482 HEAD -- Editor/HierarchyX/`,
`Editor/ProjectWindowX/`, `Documentation~/ARCHITECTURE.md`) and re-verifies every original finding.

**Diff scope actually touched between `d714482` and `HEAD` under `Editor/HierarchyX/`:**
`HierarchyX.Editor.asmdef` (reference + `defineConstraints` change), `HierarchyX.cs` (one new call site),
`HierarchyXFolderIcons.cs` (**new**, 56 lines), `HierarchyXSettings.cs` (+1 field), `HierarchyXSettingsProvider.cs`
(+1 settings-extras hook, +2 search keywords). No other file in this package changed.

**Important process note discovered during this re-audit**: three of the original audit's findings —
`soloButtons` defaulting to `true`, the missing HierarchyX section in `ARCHITECTURE.md`, and the
`LayerColor`/`PanelChip` optional-parameter constructors — were **already fixed at commit `d714482` itself**
(verified via `git show d714482:<file>` vs. its parent `d714482^`: `soloButtons` changed `true`→`false`,
the "## Hierarchy tooling (HierarchyX)" section was added to `ARCHITECTURE.md`, and both types already had
explicit non-optional overloads, all in the same "Audit" commit that wrote the original `AUDIT.md`). The
original `AUDIT.md`'s prose was never updated to reflect its own commit's changes, so it described a
"before" state that no longer matched the tree it was committed alongside. This re-audit reports the
**current, HEAD** status of each finding — which for these three is "fixed," but not because of anything
in the `d714482..HEAD` window; that window left them untouched (i.e. still fixed).

## Status of original findings

| # | Original finding | Status at HEAD | Evidence |
|---|---|---|---|
| 1 | Designer Surface Priority — no findings | No longer applicable to re-check (nothing in this area changed) | No diff in `HierarchyX.cs`'s pipeline ordering except the one new `HierarchyXFolderIcons.Draw` call (see new finding below) |
| 2 | Engine Concept Chip System — no findings (GameEngineCore scope) | Unchanged / out of scope | No diff touches decorator/chip files |
| 3 | **Warning**: `HierarchyXRowControls.soloButtons` defaults `true`, duplicating Unity's Scene-Visibility hover icons | **Fixed** | `Editor/HierarchyX/HierarchyXSettings.cs:121` — `public bool soloButtons = false;` at `HEAD`. Confirmed already `false` at the audit's own base commit `d714482` (`git show d714482^:...HierarchyXSettings.cs` shows `true`; `git show d714482:...` shows `false` — the flip happened in the same commit that wrote the original audit). `Documentation~/ARCHITECTURE.md` (current, and already at `d714482`) states explicitly: "`HierarchyXRowControls.soloButtons` (hover visibility/pickability toggles) defaults **off**: Unity's own stock Hierarchy already shows equivalent hover icons... only `rowActiveToggle` (genuinely new) defaults on." No regression between `d714482` and `HEAD` — value unchanged, code in `HierarchyXRowControls.cs` untouched by this diff. |
| 4 | Designer Vocabulary Compliance — no findings | Unchanged | Only new strings added are the Folder Icons toggle/HelpBox (checked fresh below — compliant) |
| 5 | Ownership — no findings | Unchanged | Still exactly one `hierarchyWindowItemOnGUI` subscriber; new `HierarchyXFolderIcons.Draw` call is inline in the existing pass, not a second pipeline |
| 6 | Auto-Discovery Consistency — no findings | Unchanged | Not touched by this diff |
| 7 | **Info**: manual `Register`/`Unregister` escape hatches unused | Unchanged / still applicable | Not touched by this diff; still zero callers |
| 8 | **Info**: `LayerColor`/`PanelChip` optional-parameter constructors violate docs/00 §2 | **Fixed** | `HierarchyXSettings.cs:38,44` — `LayerColor(int,Color,TintMode)` and a separate `LayerColor(int,Color) : this(..., TintMode.GradientRightToLeft)`, no optional parameter. `Panel/IHierarchyPanelSection.cs:28,34` — `PanelChip(string,PanelChipStatus,string)` and `PanelChip(string,PanelChipStatus) : this(..., null)`, likewise two explicit overloads, no default parameter value. Both already in this exact shape at `d714482` (`git show d714482:<file>` matches `HEAD` byte-for-byte on these lines) — fixed before/at the original audit's own commit, not in the window since. |
| 9 | **Warning**: `Documentation~/ARCHITECTURE.md` never mentions HierarchyX | **Fixed** (as of `d714482`; still present at `HEAD`) | `git show HEAD:Documentation~/ARCHITECTURE.md` has a full "## Hierarchy tooling (HierarchyX)" section (lines 264-276 at HEAD): namespace, one-line pipeline summary, a table with both `IHierarchyRowDecorator`/`IHierarchyPanelSection` extension points and their real consumers, the `ProjectSettings/HierarchyXSettings.asset` location, and the `soloButtons`-defaults-off note. Also listed in the "Editor tooling" table (`| HierarchyX | Editor/HierarchyX/ | ... |`) and the namespace map. `git diff d714482 HEAD -- Documentation~/ARCHITECTURE.md` shows this section already existed at `d714482` and was only *extended* since (one sentence added about `HierarchyXSettingsExtras.Register`, matching the new Folder Icons settings hook — see below). |
| 10 | Codebase Gotchas — no findings | Unchanged for pre-existing files; re-checked fresh for the new file (see below) | — |

**Net result: every one of the original audit's three actionable findings (#3, #8, #9) is fixed at HEAD.**
None were fixed *by* the `d714482..HEAD` diff under review — they were already fixed in the commit that
introduced the audit document itself. The `d714482..HEAD` window's only substantive change to this
package is the new Folder Icons feature, audited fresh below, plus one related cross-package doc-drift
regression it exposed (asmdef `defineConstraints`).

## New findings (this diff window)

### `HierarchyXFolderIcons.cs` — what it does

New internal static class, hooked into the main draw pipeline at `HierarchyX.cs:97`
(`HierarchyXFolderIcons.Draw(rect, go, s);`, immediately after `HierarchyXBestIcon.Draw` and before
`HierarchyXMissingScript.Draw`). Gated by a new `HierarchyXSettings.folderIcons` bool (default `false`,
`HierarchyXSettings.cs:114`). When enabled, for every hierarchy row it resolves an asset path for the
row's `GameObject` (`ResolveAssetPath`: direct `AssetDatabase.GetAssetPath(go)`, then
`PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go)`, then a walk up `transform.parent` retrying
`AssetDatabase.GetAssetPath` on each ancestor), and if a path is found, calls the pre-existing
`ProjectWindowX.FolderIcons.TryResolve(path, ProjectWindowXSettings.instance, out icon, out matchedFolder)`
(`Editor/ProjectWindowX/Passes/FolderIcons.cs:49-83`) to look up a matching folder-icon rule and draws the
resulting icon at a fixed 16×16 rect on the row, plus an invisible tooltip label.

Enabling this required `HierarchyX.Editor.asmdef` to add a hard reference to `ProjectWindowX.Editor`
(previously `"references": []`) and to drop `"defineConstraints": ["HOMAM_GEC"]` entirely (see the asmdef
finding below) — this is the mechanism by which a HierarchyX row can now consult ProjectWindowX's rule
table without a runtime type-name lookup.

### Bug — `applyToHierarchy` opt-in flag is defined, surfaced in UI, and documented, but never enforced

**Severity: high (functional bug, contradicts its own doc comment and its own Settings UI copy).**

`ProjectWindowXSettings.FolderIconRule.applyToHierarchy` (`Editor/ProjectWindowX/ProjectWindowXSettings.cs:30`)
is a new per-rule bool, editable via a checkbox in the Project Settings ▸ ProjectWindowX rule list
(`ProjectWindowXSettingsProvider.cs:182`, `EditorGUI.PropertyField(hierRect, element.FindPropertyRelative("applyToHierarchy"), ...)`).
`HierarchyXFolderIcons.cs`'s own XML doc comment says it draws icons "on hierarchy rows whose asset path
... matches a rule with `applyToHierarchy = true`," and the settings HelpBox it registers says: "Check
'Apply to Hierarchy' on a rule to include it here" (`HierarchyXSettingsProvider.cs`, new
`HierarchyXFolderIconsSettingsHook.DrawFolderIconsSection`).

But `FolderIcons.TryResolve` — the only method `HierarchyXFolderIcons.Draw` calls to resolve an icon
(`Editor/HierarchyX/HierarchyXFolderIcons.cs:20`) — never reads `applyToHierarchy` at all
(`Editor/ProjectWindowX/Passes/FolderIcons.cs:57-78`: the loop tests only `rule.folderPath`,
`rule.applyToChildren`, `rule.builtinIconName`, `rule.customIcon`). A project-wide grep for
`applyToHierarchy` (`grep -rn applyToHierarchy Editor/`) shows exactly three hits: the field declaration,
the settings-UI checkbox that writes it, and the doc-comment that claims it's read — **no code path reads
it**. Practical consequence: turning on `HierarchyXSettings.folderIcons` shows the icon for **every**
configured folder-icon rule whose path matches (with `applyToChildren` true) on Hierarchy rows, regardless
of whether that rule's "Apply to Hierarchy" checkbox is on or off — the opt-in the UI advertises does
nothing.

### Bug — folder icon draws directly on top of the best-component icon at the identical rect, with no opaque backing

**Severity: medium (visual regression when both `bestIcons` and `folderIcons` are enabled, the two most
directly comparable features in this file).**

`HierarchyXBestIcon.Draw` (`Editor/HierarchyX/HierarchyXBestIcon.cs:13-27`) computes
`var size = Mathf.Min(16f, rect.height); var iconRect = new Rect(rect.x, rect.yMin + (rect.height - size) * 0.5f, size, size);`
and — critically — paints an **opaque row-matched background** first
(`EditorGUI.DrawRect(iconRect, bg)`, where `bg = HierarchyX.ComposeRowBackground(...)`) specifically, per
its own doc comment, "so Unity's default icon is erased rather than showing through transparent/letterboxed
areas of the custom icon."

`HierarchyXFolderIcons.Draw` (`Editor/HierarchyX/HierarchyXFolderIcons.cs:16-24`) computes the **exact same
formula** — `var size = Mathf.Min(16f, rect.height); var iconRect = new Rect(rect.x, rect.yMin + (rect.height - size) * 0.5f, size, size);`
— and is called immediately after `HierarchyXBestIcon.Draw` in the pipeline (`HierarchyX.cs:96-97`), but
calls `GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit)` directly with **no background paint first**.
Two consequences, both unaddressed by any settings interaction between the two features:
1. On any row where both `bestIcons` and `folderIcons` are enabled and a folder-icon rule matches, the
   folder icon is painted **exactly on top of** the best-component icon at the same 16×16 slot — one
   silently replaces the other with no indication either happened, and no way to see both.
2. Because there's no opaque backing paint (unlike its sibling `HierarchyXBestIcon`), any folder icon with
   transparent regions (most of Unity's built-in folder/asset icons have transparent corners) will let
   whatever was drawn underneath (Unity's stock default icon, or the best-component icon) show through —
   the exact visual artifact `HierarchyXBestIcon`'s own doc comment explains it added the background paint
   to avoid.

### Efficiency — no per-row caching, unlike the sibling feature it sits next to

**Severity: low/info.** `HierarchyXRowCache` (`Editor/HierarchyX/HierarchyXRowCache.cs:6-9`) exists
specifically to keep `GetComponents`-class work "out of the per-repaint path," invalidated only on
`hierarchyChanged`/undo/`ObjectChangeEvents`. `HierarchyXBestIcon.Draw` uses it
(`HierarchyXRowCache.Get(go).icon`). `HierarchyXFolderIcons.Draw` does the opposite: on **every Repaint,
for every visible row**, when `folderIcons` is enabled, it calls `AssetDatabase.GetAssetPath` (up to twice),
conditionally `PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot`, then a `while` loop up the transform
ancestry each iteration calling `AssetDatabase.GetAssetPath` again, then a linear scan of
`ProjectWindowXSettings.instance.folderIconRules` inside `FolderIcons.TryResolve` — none of it cached. In a
scene with many rows and/or many configured rules this reintroduces exactly the per-repaint cost pattern
`HierarchyXRowCache`'s own doc comment says this package deliberately avoids elsewhere.

### Redundancy check — does this duplicate an existing folder-icon mechanism?

No duplicate *implementation* — `HierarchyXFolderIcons.Draw` correctly delegates to the pre-existing
`ProjectWindowX.FolderIcons.TryResolve` (`Editor/ProjectWindowX/Passes/FolderIcons.cs`) rather than
re-parsing rules or re-resolving icons itself, so the single source of truth (`ProjectWindowXSettings.folderIconRules`)
is preserved and there is no second, competing rule table. This is the right shape for reuse. The problem
is narrower than duplication: it's that the *cross-cutting opt-in* (`applyToHierarchy`) meant to let a
designer choose "this rule's icon should also show in the Hierarchy, not just the Project window" is wired
into the UI and the doc-comment but not into the shared `TryResolve` (or a Hierarchy-specific overload of
it) — see the bug above.

### Assembly-boundary drift — `defineConstraints: ["HOMAM_GEC"]` silently removed from four assemblies, `ARCHITECTURE.md` now factually wrong

**Severity: warning — this is a new, currently-live doc/architecture drift, distinct from and in addition
to the original audit's now-fixed one.**

`git diff d714482 HEAD -- Editor/HierarchyX/HierarchyX.Editor.asmdef` shows two changes together:
`"references": []` → `"references": ["ProjectWindowX.Editor"]`, and `"defineConstraints": ["HOMAM_GEC"]` →
`"defineConstraints": []`. The same `defineConstraints` removal (with `versionDefines` on
`com.aethernexus.gameenginecore` left in place, still producing the `HOMAM_GEC` symbol) happened in the
same diff window to three sibling assemblies: `Editor/ProjectWindowX/ProjectWindowX.Editor.asmdef`,
`Editor/EditorEnhancerX/EditorEnhancerX.Editor.asmdef`, and
`Editor/StaleComponentGuard/FoundationPlatform.StaleComponentGuard.Editor.asmdef` (verified via
`git diff d714482 HEAD` on each file). A repo-wide grep for `HOMAM_GEC` outside `.asmdef`/`.meta`/`AUDIT.md`
files finds only `Editor/PackageIntegration/HomamGecOrphanDefineCleaner.cs` (a `PlayerSettings`
scripting-define cleanup utility, unrelated to these four asmdefs' compile gating) — no `#if HOMAM_GEC`
guard exists anywhere in the four assemblies' own code, so this was a pure `defineConstraints` removal,
not a shift to finer-grained conditional compilation inside the files.

`Documentation~/ARCHITECTURE.md` at `HEAD` still asserts the opposite of the current state:
line 53 — "`ProjectWindowX.Editor`, `HierarchyX.Editor` ..., `EditorEnhancerX.Editor`, and
`FoundationPlatform.StaleComponentGuard.Editor` additionally carry `defineConstraints: ["HOMAM_GEC"]` +
`versionDefines` on `com.aethernexus.gameenginecore` — they only compile when GameEngineCore is present as
a UPM package... these four designer-facing subsystems silently don't exist at all in a build without
GameEngineCore installed." This sentence is now **false** for all four named assemblies: none of them
carry `defineConstraints` any more, so all four always compile regardless of whether GameEngineCore is
installed. This is a direct, in-scope contradiction (`HierarchyX.Editor` is explicitly named in the
now-stale sentence), and it was introduced in the exact diff window this re-audit covers, so — unlike the
three original findings above — this one was **not** present at `d714482` and is a genuinely new
regression for this re-audit to report. It also means the sibling `Editor/ProjectWindowX/AUDIT.md`'s
Context section ("The entire `ProjectWindowX.Editor` assembly is gated...") is now stale for the same
reason, though fixing that file is out of scope here (only this package's `HierarchyX/AUDIT.md` was
edited, per instructions).

Practical effect of the reference + constraint change together: previously, `HierarchyX.Editor` had zero
assembly references and only existed when GameEngineCore was installed; `ProjectWindowX.Editor` was the
same (zero references, same gating). Now both compile unconditionally *and* `HierarchyX.Editor` has a real,
undocumented hard reference on `ProjectWindowX.Editor` — a new coupling between what
`Documentation~/ARCHITECTURE.md`'s Namespace map still describes as two independent "bare namespace"
subsystems, with no mention of the new dependency anywhere in the doc.

### Codebase Gotchas (docs/00 §3) — checked fresh for `HierarchyXFolderIcons.cs`

No findings for the new file specifically:
- No `??`/`??=` on a `UnityEngine.Object`-typed operand — `ResolveAssetPath`'s `if (go == null) return null;`
  (line 30) uses the correct Unity-null idiom; all other null checks are on `string` (`string.IsNullOrEmpty`).
- No `struct` declarations added.
- No `OnValidate`/`OnAfterDeserialize` added; the new `HierarchyXSettings.folderIcons` field is a plain
  bool with a `[Tooltip]`, handled by the pre-existing `OnValidate` (untouched by this diff) which does not
  reference it.
- No optional parameters introduced (`Draw(Rect, GameObject, HierarchyXSettings)` and
  `ResolveAssetPath(GameObject)` both take required arguments only).

### Designer Vocabulary Compliance — checked fresh

No findings. "Enable Folder Icons" / "Folder Icons" / the HelpBox text use plain designer-facing language;
no docs/14 stale-jargon term ("Drift", bare "Registry"/"Definition", etc.) appears in the new strings
(`HierarchyXSettingsProvider.cs`'s new `DrawFolderIconsSection`, `HierarchyXSettings.cs`'s new `[Tooltip]`).
The new settings-search keywords added (`"folder"`, `"icon"`) are consistent with the existing
plain-vocabulary keyword list.

## Fixes

No files were edited under this task — per instructions, only `AUDIT.md` was rewritten; no source changes
were made. Suggested fixes, in priority order, for a follow-up task:

1. **`applyToHierarchy` not enforced (bug)** — Either (a) add a `bool forHierarchy` parameter to
   `FolderIcons.TryResolve`/`Resolve` (or a new overload) that also checks `rule.applyToHierarchy` when
   called from `HierarchyXFolderIcons.Draw`, or (b) filter the rule list in `HierarchyXFolderIcons.Draw`
   itself before calling `TryResolve`. Needed so the "Apply to Hierarchy" checkbox the Settings UI already
   exposes actually does what its own HelpBox claims.
2. **Best-icon / folder-icon collision (bug)** — Give `HierarchyXFolderIcons.Draw` an opaque background
   paint before `GUI.DrawTexture` (matching `HierarchyXBestIcon.Draw`'s `ComposeRowBackground` pattern), and
   decide product intent for the case where both features would draw at the same slot for the same row —
   e.g. offset the folder icon to a different position, or make the two features mutually exclusive per row
   (folder icon wins / best icon wins / side-by-side), and document the decision.
3. **No per-row caching (efficiency)** — Route `HierarchyXFolderIcons.Draw`'s path resolution and rule
   match through `HierarchyXRowCache` (or a similarly invalidated per-GameObject cache), consistent with
   how `HierarchyXBestIcon` already avoids repeated per-repaint work.
4. **`ARCHITECTURE.md` `defineConstraints` claim now false (doc drift)** — Update
   `Documentation~/ARCHITECTURE.md`'s Assembly-definitions paragraph (currently around the "additionally
   carry `defineConstraints: [\"HOMAM_GEC\"]`" sentence) to reflect that `ProjectWindowX.Editor`,
   `HierarchyX.Editor`, `EditorEnhancerX.Editor`, and `FoundationPlatform.StaleComponentGuard.Editor` no
   longer gate compilation on GameEngineCore's presence (only `versionDefines` remains, unused by any
   `#if HOMAM_GEC` in-code guard) — or, if the constraint removal was accidental, restore it. Also add a
   line noting `HierarchyX.Editor`'s new hard reference on `ProjectWindowX.Editor` to the Namespace-map /
   Assembly-definitions section, since both are currently documented as independent.
5. Carry over the original audit's already-fixed items — no action needed; listed here only for
   completeness: `soloButtons` default, `LayerColor`/`PanelChip` optional parameters, and the
   HierarchyX section in `ARCHITECTURE.md` all remain fixed at `HEAD`.

## Cross-references

- Original audit re-verified: this package's own prior `AUDIT.md` content (at `d714482`), superseded by
  this document.
- `git diff d714482 HEAD -- Editor/HierarchyX/` (162 lines) and
  `git diff d714482 HEAD -- Documentation~/ARCHITECTURE.md` — the two diffs this re-audit is based on.
- Also read for the assembly-boundary finding (out of this package's directory but load-bearing for a
  `Editor/HierarchyX/` finding): `Editor/HierarchyX/HierarchyX.Editor.asmdef`,
  `Editor/ProjectWindowX/ProjectWindowX.Editor.asmdef`, `Editor/EditorEnhancerX/EditorEnhancerX.Editor.asmdef`,
  `Editor/StaleComponentGuard/FoundationPlatform.StaleComponentGuard.Editor.asmdef`,
  `Editor/PackageIntegration/HomamGecOrphanDefineCleaner.cs`, and the existing
  `Editor/ProjectWindowX/AUDIT.md` (read for context only — not edited; its Context section is now stale
  on the same `defineConstraints` point but fixing it is out of this task's scope).
- Files read in full for the new-code audit: `Editor/HierarchyX/HierarchyXFolderIcons.cs` (new, 56 lines),
  `Editor/HierarchyX/HierarchyXBestIcon.cs`, `Editor/HierarchyX/HierarchyXRowCache.cs`,
  `Editor/HierarchyX/HierarchyX.cs` (call-site context), `Editor/HierarchyX/HierarchyXSettings.cs`,
  `Editor/HierarchyX/HierarchyXSettingsProvider.cs`, `Editor/ProjectWindowX/Passes/FolderIcons.cs`,
  `Editor/ProjectWindowX/ProjectWindowXSettings.cs` (for `FolderIconRule`/`applyToHierarchy`),
  `Editor/ProjectWindowX/ProjectWindowXSettingsProvider.cs` (for the `applyToHierarchy` checkbox call site).
- Docs read/re-checked: `docs/00-AgentGuide.md` §3 "Codebase-specific gotchas" (line 85 onward),
  `docs/09-EditorHub.md` (HierarchyX designer-surface ranking, line 10),
  `docs/13-AuthoringStandards.md` (line 94, "must not re-show Unity Project/Inspector defaults... Show
  only authoring-unique status and actions" — relevant context for whether a Hierarchy-side folder icon is
  additive vs. redundant; judged additive since it surfaces Project-window metadata at a point of use
  (prefab rows) Unity's own Hierarchy never shows), `docs/14-DesignerVocabulary.md` (stale-jargon list,
  none present in new strings).
- Not re-read in full this pass (unchanged by the diff, already covered by the original audit and not
  re-verified beyond the table above): `HierarchyXDropCopy.cs`, `HierarchyXHeaders.cs`,
  `HierarchyXHoverHighlight.cs`, `HierarchyXMiniLabels.cs`, `HierarchyXMissingScript.cs`,
  `HierarchyXRowControls.cs`, `HierarchyXRowDecorator.cs`, `HierarchyXStyles.cs`, `HierarchyXUtility.cs`,
  `Panel/HierarchyPanelHost.cs`, `Panel/HierarchyPanelWidgets.cs`, `Panel/HierarchyPanelWindow.cs`,
  `Panel/HierarchyXPanelRegistry.cs`, `Panel/IHierarchyPanelSection.cs`.
