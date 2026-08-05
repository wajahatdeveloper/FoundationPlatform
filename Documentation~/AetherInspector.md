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

**Nested `[Serializable]` list elements** — register a drawer so `ShowIf` / `ReadOnly` / buttons work inside lists:

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
| `BoxGroup`, `FoldoutGroup`, `TitleGroup`, `TabGroup`, `HorizontalGroup`, `VerticalGroup`, `ToggleGroup`, `ButtonGroup` | Fully Supported | `ToggleGroup.CollapseOthersOnExpand` fully implemented |
| `Title`, `LabelText`, `LabelWidth`, `HideLabel`, `PropertyOrder`, `PropertySpace`, `Indent`, `GUIColor` | Fully Supported | `$member` / `@expression` string resolution; `HideLabel` overrides text |
| `ShowIf`, `HideIf`, `EnableIf`, `DisableIf` | Fully Supported | Animated visibility transitions (`Animate = true`) supported via fade groups |
| `HideInEditorMode`, `HideInPlayMode`, `ShowInPlayMode`, `DisableInEditorMode`, `DisableInPlayMode` | Fully Supported | |
| `ReadOnly`, `Required`, `ValidateInput`, `InfoBox`, `DetailedInfoBox`, `TypeInfoBox` | Fully Supported | `InfoBox.GUIAlwaysEnabled` scope handling supported |
| `ShowInInspector`, `Button`, `ButtonGroup`, `InlineButton`, `OnInspectorGUI`, `OnInspectorInit`, `OnValueChanged` | Fully Supported | `Button.Style`, `Icon`, `IconAlignment`, `ButtonAlignment`, parameterized invoke |
| `ListDrawerSettings`, `Searchable`, `TableList`, `TableColumnWidth`, `OnCollectionChanged` | Fully Supported | `ListDisplayMode` and `Searchable.Recursive` fully implemented |
| `ValueDropdown`, `AssetSelector`, `AssetsOnly`, `SceneObjectsOnly` | Fully Supported | `AppendNextDrawer`, `IsUniqueList`, `DisableListAddButtonBehaviour`, `HideChildProperties` implemented |
| `DictionaryDrawerSettings` | Fully Supported | `DisplayMode` and `ShowInInspector` / `IDictionary` read-only grid |
| `HideReferenceObjectPicker` | Fully Supported | Nested serializable types + inline editors |
| `InlineProperty`, `InlineEditor`, `PreviewField`, `DrawWithUnity` | Fully Supported | `InlineEditor.MaxHeight` scrolling supported |
| `PropertyRange`, `MinMaxSlider`, `ProgressBar`, `Wrap`, `MinValue`, `MaxValue`, `EnumToggleButtons`, `ToggleLeft`, `MultiLineProperty` | Fully Supported | |
| `DisplayAsString`, `RequireComponentButton` | Fully Supported | |

## Editor extension guidelines

1. Prefer attributes on runtime types over custom IMGUI in editors.
2. Extend `AetherInspectorEditor` and call `base.OnInspectorGUI()` first.
3. Append UI with `GuiKit.Title`, `GuiKit.InfoBox`, `GuiKit.ValidationBox` — not raw `EditorGUILayout.HelpBox`.
4. Nested payloads in lists need `AetherInspectorReflectedDrawer` unless a bespoke `PropertyDrawer` already exists.
5. Do not hand-edit generated code — change sources and regenerate.
6. Cache busting: context menu **Force Rebuild AetherInspector Cache** if metadata looks stale.
7. Foldouts / headers: use `AetherInspectorTheme.SectionFoldout` or `FlatFoldoutStyle` / `FlatHeaderLabel` — avoid bespoke `foldoutHeader` bars.

## Key types

| Type | Role |
|------|------|
| `AetherInspectorEditor` | Base inspector |
| `AetherInspectorFallbackEditor` | Global fallback for `UnityEngine.Object` |
| `AetherInspectorRenderer` | Attribute tree + field drawer engine |
| `AetherInspectorTheme` | Skin-aware colors, styles, layout helpers |
| `GuiKit` | Public facade for editor windows |
| `PocoInspector` | Reflection drawer for non-serialized members |
| `AetherInspectorReflectedDrawer` | PropertyDrawer base for nested serializable types |
| `EngineListDrawer` / `EngineDictionaryDrawer` / `TableRenderer` | Collection renderers |

## Implementation notes (audit closure)

**Empty `catch { }` at reflection/IMGUI sites:** intentional IMGUI robustness carve-out. Reflection-based member resolution and dynamic layout can throw on edge-case types or Unity version quirks; swallowing at the draw site keeps the Inspector usable. This is **not** the project's simulation fail-fast pattern — do not copy this style into authoritative data paths.

**`ObjectSelectorPopupX`:** scoped, type-filtered, opt-in object picker for inline editors — not a general second asset browser. Complies with designer-surface priority (docs/13): use ProjectWindowX/HierarchyX for browsing; use this only where an attribute-driven inline pick is required.
