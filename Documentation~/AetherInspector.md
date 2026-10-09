# AetherInspector

Attribute-driven inspector engine for AetherNexus (Odin-style surface, IMGUI renderer).

- **Runtime attributes:** `AetherNexus.FoundationPlatform.AetherInspector` — `Runtime/AetherInspector/AetherInspectorInspectorAttributes.cs`
- **Editor engine:** `AetherNexus.FoundationPlatform.AetherInspector.Editor` — `Editor/AetherInspector/`

Related: [Architecture](ARCHITECTURE.md) · Demo menu **Window → Diagnostics → AetherInspector Demo**

## Opt-in

**Components / ScriptableObjects** — global fallback applies automatically via `AetherInspectorFallbackEditor`. For explicit control or append UI:

```csharp
using AetherNexus.FoundationPlatform.AetherInspector.Editor;
using UnityEditor;

[CustomEditor(typeof(MyType))]
public sealed class MyTypeEditor : AetherInspectorEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GuiKit.InfoBox("Extra context.", InfoMessageType.Info);
    }
}
```

**Nested `[Serializable]` types** — the engine draws them itself (fields and list elements, at any depth), honoring groups, titles and conditionals. Register a reflected drawer only for surfaces that go through Unity's `PropertyField` outside the engine (e.g. UI Toolkit windows); the engine ignores it:

```csharp
using AetherNexus.FoundationPlatform.AetherInspector.Editor;
using UnityEditor;

[CustomPropertyDrawer(typeof(MyPayload))]
internal sealed class MyPayloadDrawer : AetherInspectorReflectedDrawer { }
```

**Editor windows** — use `GuiKit` / `AetherInspectorTheme` for boxes, titles, info callouts, and toolbars.

## Visual demo

**Window → Diagnostics → AetherInspector Demo** — exercises the attribute surface through an in-memory `AetherInspectorDemoData` object.

## Attribute support matrix

| Attribute | Status | Notes |
|-----------|--------|-------|
| `BoxGroup`, `FoldoutGroup`, `TitleGroup`, `TabGroup`, `HorizontalGroup`, `VerticalGroup`, `ToggleGroup`, `ButtonGroup` | Fully Supported | Single Theme chrome path; soft Unity-native skin |
| `Title`, `LabelText`, `LabelWidth`, `HideLabel`, `PropertyOrder`, `PropertySpace`, `Indent`, `GUIColor` | Fully Supported | `$member` / `@expression` string resolution; `HideLabel` overrides text |
| `ShowIf`, `HideIf`, `EnableIf`, `DisableIf` | Fully Supported | Animated visibility transitions (`Animate = true`) supported via fade groups |
| `HideInEditorMode`, `HideInPlayMode`, `ShowInPlayMode`, `DisableInEditorMode`, `DisableInPlayMode` | Fully Supported | |
| `ReadOnly`, `Required`, `NotEmpty`, `ValidateInput`, `InfoBox`, `DetailedInfoBox`, `TypeInfoBox` | Fully Supported | Theme validation / info boxes; `InfoBox.GUIAlwaysEnabled` scope handling supported |
| `ShowInInspector`, `Button`, `ButtonGroup`, `InlineButton`, `OnInspectorGUI`, `OnInspectorInit`, `OnValueChanged` | Fully Supported | `Button.Style`, `Icon`, `IconAlignment`, `ButtonAlignment`, parameterized invoke. `OnInspectorInit` / `OnValueChanged(InvokeOnInitialize)` run **once per Editor + target + member** until domain reload or **Force Rebuild AetherInspector Cache** |
| `ListDrawerSettings`, `Searchable`, `TableList`, `TableColumnWidth`, `OnCollectionChanged` | Fully Supported | `ListDisplayMode` and `Searchable.Recursive` fully implemented |
| `ValueDropdown`, `AssetSelector`, `AssetsOnly`, `SceneObjectsOnly` | Fully Supported | Theme field chrome + Handles caret; searchable popup |
| `DictionaryDrawerSettings` | Fully Supported | `DisplayMode` and `ShowInInspector` / `IDictionary` read-only grid |
| `HideReferenceObjectPicker` | Fully Supported | Nested serializable types + inline editors |
| `InlineProperty`, `HeaderMember`, `InlineEditor`, `PreviewField`, `DrawWithUnity` | Fully Supported | `InlineEditor.MaxHeight` scrolling; PreviewField height + texture preview |
| `PropertyRange`, `MinMaxSlider`, `ProgressBar`, `Knob`, `Percentage`, `Curve`, `Wrap`, `MinValue`, `MaxValue`, `EnumToggleButtons`, `ToggleLeft`, `MultiLineProperty` | Fully Supported | Styled sliders (hover min/cur/max); Knob arc fill + labels; Percentage drag-scrub |
| `DisplayAsString`, `RequireComponentButton` | Fully Supported | DisplayAsString: label left, value uses alignment; RequireComponent: disabled on non-GO |

## Labels and headers

- **Lists:** every list or array with a resolvable element type uses `EngineListDrawer` (Unity's stock list is never used). Row label: `ListDrawerSettings.ListElementLabelName` → a `name` / `displayName` / `label` / `id` string member → the first non-empty string member → the first referenced Object's name → the `[SerializeReference]` concrete type name → `#index`. Object-reference, enum and primitive rows draw no label prefix (the field already shows the value). `ShowElementLabels = false` hides all labels.
- **Nested objects:** any concrete `[Serializable]` class/struct without an IMGUI `PropertyDrawer` recurses through the engine. Types whose only drawer is UI Toolkit (`CreatePropertyGUI` without `OnGUI`) recurse too instead of showing "No GUI Implemented".
- **Repeated headers:** a nested-object or list header whose text equals the enclosing `BoxGroup` / `FoldoutGroup` / `TitleGroup` / `[Title]` / parent nested-object header is suppressed: the outer title stays, a nested object draws flush, a list keeps only `(count)` and its buttons.
- **`[HeaderMember]`** (class/struct): named serialized fields and parameterless `[Button]` methods are drawn right-aligned in the nested object's own header row. `FragmentData<,>` uses it for the Shared / Inline Custom selector and **Promote to Shared**. With `[HideLabel]` on the host field there is no header and the members stay in the body.

## Layout settings

Project-wide, opt-in layout features under **Project Settings > AetherInspector > Layout** (`InspectorXSettings`). All default off; each one changes only engine-drawn inspectors.

| Toggle | Effect |
|--------|--------|
| **Card Groups** | Top-level `[FoldoutGroup]`, every `[TitleGroup]` and `[ToggleGroup]` draw as a card: darker header bar (arrow / title + subtitle / toggle switch) over a padded, bordered body. Nested foldouts stay flat; `[BoxGroup]` is unchanged. |
| **Inline Validation** | `Warning` / `Info` results from `[Required]`, `[NotEmpty]`, `[ValidateInput]` outline the field and add a trailing severity icon; messages show on hover. `Error` results keep the full-width box above the field. |
| **Info Badges** | `[InfoBox]` becomes an icon right after the field label (message on hover); buttons get it at the row's right edge, `[HideLabel]` members keep the box. `[TypeInfoBox]` becomes an icon on the script row. `[DetailedInfoBox]` is unchanged. |
| **Label Column** | Label width = inspector width × fraction (default 35%), clamped to min/max px. `[LabelWidth]` / `[InlineProperty(LabelWidth)]` still override. A titled `[HorizontalGroup]` puts its title in the label column and its cells in the control column; `[EnumToggleButtons]` already aligns via `PrefixLabel`. |

**Knob vs slider:** use `[Knob]` for angular or radial values (e.g. 0–360°); use `[PropertyRange]` (slider + numeric field, standard row height) for plain numeric ranges.

## Theme and performance

- **Theme:** soft Unity-native tokens in `AetherInspectorTheme` (Pro/Personal). Foldouts/boxes/tabs/info use Theme/GuiKit only — no raw `HelpBox` / `foldoutHeader` in engine draw paths.
- **Info / validation boxes:** dim skin-aware fills with rounded corners (low visual clutter).
- **Ordering:** root children sort by `PropertyOrder`, then `Sequence` (declaration / SerializedObject iterator). Groups use group-attr `Order` and **min Sequence of members** so a `[BoxGroup]` appears where its first field is declared—not at Dictionary iteration order. Later demo fields with `[PropertyOrder(100+)]` intentionally sort after earlier Order-0 content.
- **Perf:** cached GUIStyles (skin-invalidated); pooled render group tree (no per-frame `CloneGroupNode`); Handles discs for rounded chrome; Handles AA caret for dropdowns; optional 1×1 tint `Texture2D` dictionary when a style background is required.
- **GUIColor:** multiplies Theme chrome colors via `GuiTint` so custom drawers honor the tint.
- **RequireComponentButton:** on ScriptableObject / non-GameObject targets shows a disabled button plus “Requires a GameObject target.”
- Cache busting: context menu **Force Rebuild AetherInspector Cache** if metadata looks stale.

## Editor extension guidelines

1. Prefer attributes on runtime types over custom IMGUI in editors.
2. Extend `AetherInspectorEditor` and call `base.OnInspectorGUI()` first.
3. Append UI with `GuiKit.Title`, `GuiKit.InfoBox`, `GuiKit.ValidationBox` — not raw `EditorGUILayout.HelpBox`.
4. Nested payloads need no drawer inside engine inspectors. A bespoke IMGUI `PropertyDrawer` takes over from the engine for its type; `AetherInspectorReflectedDrawer` does not.
5. Do not hand-edit generated code — change sources and regenerate.
6. Cache busting: context menu **Force Rebuild AetherInspector Cache** if metadata looks stale.
7. Foldouts / headers: use `AetherInspectorTheme.SectionFoldout` or `FlatFoldoutStyle` / `FlatHeaderLabel` — avoid bespoke `foldoutHeader` bars.
8. Prefer Theme draw helpers (`DrawStyledSlider`, `DrawToggleSwitch`, `DrawRoundedRect`, `DrawDropdownCaret`) over ad-hoc IMGUI chrome.

## Key types

| Type | Role |
|------|------|
| `AetherInspectorEditor` | Base inspector |
| `AetherInspectorFallbackEditor` | Global fallback for `UnityEngine.Object` |
| `AetherInspectorRenderer` | Attribute tree + field drawer engine |
| `AetherInspectorTheme` | Skin-aware colors, styles, layout helpers |
| `GuiKit` | Public facade for editor windows |
| `PocoInspector` | Reflection drawer for non-serialized members |
| `AetherInspectorReflectedDrawer` | PropertyDrawer base for nested serializable types on Unity `PropertyField` surfaces outside the engine |
| `EngineListDrawer` / `EngineDictionaryDrawer` / `TableRenderer` | Collection renderers |

## Implementation notes (audit closure)

**Empty `catch { }` at reflection/IMGUI sites:** intentional IMGUI robustness carve-out. Reflection-based member resolution and dynamic layout can throw on edge-case types or Unity version quirks; swallowing at the draw site keeps the Inspector usable. This is **not** the project's simulation fail-fast pattern — do not copy this style into authoritative data paths.

**`ObjectSelectorPopupX`:** scoped, type-filtered, opt-in object picker for inline editors — not a general second asset browser. Complies with designer-surface priority (docs/13): use ProjectWindowX/HierarchyX for browsing; use this only where an attribute-driven inline pick is required.
