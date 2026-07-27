# Framework Inspector

Attribute-driven inspector engine for AetherNexus (Odin-style surface, IMGUI renderer).

- **Runtime attributes:** `AetherNexus.FoundationPlatform.FrameworkInspector` — `Runtime/FrameworkInspector/FrameworkInspectorAttributes.cs`
- **Editor engine:** `AetherNexus.FoundationPlatform.FrameworkInspector.Editor` — `Editor/FrameworkInspector/`

Related: [Architecture](ARCHITECTURE.md) · Demo menu **Tools → Diagnostics → Framework Inspector Demo**

## Opt-in

**Components / ScriptableObjects** — global fallback applies automatically via `FrameworkFallbackEditor`. For explicit control or append UI:

```csharp
using AetherNexus.FoundationPlatform.FrameworkInspector.Editor;
using UnityEditor;

[CustomEditor(typeof(MyType))]
public sealed class MyTypeEditor : FrameworkEditor
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
using AetherNexus.FoundationPlatform.FrameworkInspector.Editor;
using UnityEditor;

[CustomPropertyDrawer(typeof(MyPayload))]
internal sealed class MyPayloadDrawer : FrameworkReflectedDrawer { }
```

**Editor windows** — use `GuiKit` / `FrameworkInspectorTheme` for boxes, titles, info callouts, and toolbars.

## Visual demo

**Tools → Diagnostics → Framework Inspector Demo** — exercises the attribute surface through an in-memory `FrameworkInspectorDemoData` object.

## Attribute support matrix

| Attribute | Status | Notes |
|-----------|--------|-------|
| `BoxGroup`, `FoldoutGroup`, `TitleGroup`, `TabGroup`, `HorizontalGroup`, `VerticalGroup`, `ToggleGroup`, `ButtonGroup` | Fully Supported | `ToggleGroup.CollapseOthersOnExpand` fully implemented |
| `Title`, `LabelText`, `LabelWidth`, `HideLabel`, `PropertyOrder`, `PropertySpace`, `Indent`, `GUIColor` | Fully Supported | `$member` / `@expression` string resolution; `HideLabel` overrides text |
| `ShowIf`, `HideIf`, `EnableIf`, `DisableIf` | Fully Supported | Animated visibility transitions (`Animate = true`) supported via fade groups |
| `HideInEditorMode`, `HideInPlayMode`, `ShowInPlayMode`, `DisableInEditorMode`, `DisableInPlayMode` | Fully Supported | |
| `ReadOnly`, `Required`, `ValidateInput`, `InfoBox`, `DetailedInfoBox`, `TypeInfoBox` | Fully Supported | `InfoBox.GUIAlwaysEnabled` scope handling supported |
| `ShowInInspector`, `Button`, `ButtonGroup`, `InlineButton`, `OnInspectorGUI`, `OnInspectorInit`, `OnValueChanged` | Fully Supported | `Button.Style`, `Icon`, `IconAlignment`, alignment, parameterized invoke |
| `ListDrawerSettings`, `Searchable`, `TableList`, `TableColumnWidth`, `OnCollectionChanged` | Fully Supported | `ListDisplayMode` and `Searchable.Recursive` fully implemented |
| `ValueDropdown`, `AssetSelector`, `AssetsOnly`, `SceneObjectsOnly` | Fully Supported | `AppendNextDrawer`, `IsUniqueList`, `DisableListAddButtonBehaviour`, `HideChildProperties` implemented |
| `DictionaryDrawerSettings` | Fully Supported | `DisplayMode` and `ShowInInspector` / `IDictionary` read-only grid |
| `HideReferenceObjectPicker` | Fully Supported | Nested serializable types + inline editors |
| `InlineProperty`, `InlineEditor`, `PreviewField`, `DrawWithUnity` | Fully Supported | `InlineEditor.MaxHeight` scrolling supported |
| `PropertyRange`, `MinMaxSlider`, `ProgressBar`, `Wrap`, `MinValue`, `MaxValue`, `EnumToggleButtons`, `ToggleLeft`, `MultiLineProperty` | Fully Supported | |
| `DisplayAsString`, `RequireComponentButton` | Fully Supported | |

## Editor extension guidelines

1. Prefer attributes on runtime types over custom IMGUI in editors.
2. Extend `FrameworkEditor` and call `base.OnInspectorGUI()` first.
3. Append UI with `GuiKit.Title`, `GuiKit.InfoBox`, `GuiKit.ValidationBox` — not raw `EditorGUILayout.HelpBox`.
4. Nested payloads in lists need `FrameworkReflectedDrawer` unless a bespoke `PropertyDrawer` already exists.
5. Do not hand-edit generated code — change sources and regenerate.
6. Cache busting: context menu **Force Rebuild Framework Inspector Cache** if metadata looks stale.
7. Foldouts / headers: use `FrameworkInspectorTheme.SectionFoldout` or `FlatFoldoutStyle` / `FlatHeaderLabel` — avoid bespoke `foldoutHeader` bars.

## Key types

| Type | Role |
|------|------|
| `FrameworkEditor` | Base inspector |
| `FrameworkFallbackEditor` | Global fallback for `UnityEngine.Object` |
| `FrameworkInspectorRenderer` | Attribute tree + field drawer engine |
| `FrameworkInspectorTheme` | Skin-aware colors, styles, layout helpers |
| `GuiKit` | Public facade for editor windows |
| `PocoInspector` | Reflection drawer for non-serialized members |
| `FrameworkReflectedDrawer` | PropertyDrawer base for nested serializable types |
| `EngineListDrawer` / `EngineDictionaryDrawer` / `TableRenderer` | Collection renderers |
