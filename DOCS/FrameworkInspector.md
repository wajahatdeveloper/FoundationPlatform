# Framework Inspector

Attribute-driven inspector engine for AetherNexus (Odin-style surface, IMGUI renderer).

- **Runtime attributes:** `AetherNexus.FoundationPlatform.FrameworkInspector` — `Runtime/FrameworkInspector/FrameworkInspectorAttributes.cs`
- **Editor engine:** `AetherNexus.FoundationPlatform.FrameworkInspector.Editor` — `Editor/FrameworkInspector/`

Related: [Architecture](../Documentation~/ARCHITECTURE.md) · Demo menu **Tools → Diagnostics → Framework Inspector Demo**

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
| `BoxGroup`, `FoldoutGroup`, `TitleGroup`, `TabGroup`, `HorizontalGroup`, `VerticalGroup`, `ToggleGroup`, `ButtonGroup` | Supported | `FoldoutGroup.VisibleIf` supported; `ToggleGroup.CollapseOthersOnExpand` API only |
| `Title`, `LabelText`, `LabelWidth`, `HideLabel`, `PropertyOrder`, `PropertySpace`, `Indent`, `GUIColor` | Supported | `$member` / `@expression` string resolution |
| `ShowIf`, `HideIf`, `EnableIf`, `DisableIf` | Supported | `Animate` API only (instant show/hide) |
| `HideInEditorMode`, `HideInPlayMode`, `ShowInPlayMode`, `DisableInEditorMode`, `DisableInPlayMode` | Supported | |
| `ReadOnly`, `Required`, `ValidateInput`, `InfoBox`, `DetailedInfoBox`, `TypeInfoBox` | Supported | Themed callouts via `FrameworkInspectorTheme` |
| `ShowInInspector`, `Button`, `ButtonGroup`, `InlineButton`, `OnInspectorGUI`, `OnInspectorInit`, `OnValueChanged` | Supported | `Button.Style`, `Icon`, `IconAlignment`, alignment, parameterized invoke |
| `ListDrawerSettings`, `Searchable`, `TableList`, `TableColumnWidth`, `OnCollectionChanged` | Supported | `Searchable.Recursive` API only |
| `ValueDropdown`, `AssetSelector`, `AssetsOnly`, `SceneObjectsOnly` | Supported | Several dropdown flags API only (see below) |
| `DictionaryDrawerSettings` | Supported | `ShowInInspector` / `IDictionary` read-only grid |
| `HideReferenceObjectPicker` | Supported | Nested serializable types + inline editors |
| `InlineProperty`, `InlineEditor`, `PreviewField`, `DrawWithUnity` | Supported | `InlineEditor.MaxHeight` API only |
| `PropertyRange`, `MinMaxSlider`, `ProgressBar`, `Wrap`, `MinValue`, `MaxValue`, `EnumToggleButtons`, `ToggleLeft`, `MultiLineProperty` | Supported | |
| `DisplayAsString`, `RequireComponentButton` | Supported | |

### Declared but not fully implemented (API-only)

- `ValueDropdown`: `AppendNextDrawer`, `IsUniqueList`, `DisableListAddButtonBehaviour`, `HideChildProperties`, `ExpandAllMenuItems`
- `AssetSelector`: `ExcludeExistingValuesInList`, `ExpandAllMenuItems`
- `ShowIf` / `HideIf`: `Animate`
- `ToggleGroup`: `CollapseOthersOnExpand`
- `InfoBox`: `GUIAlwaysEnabled` (member-level boxes already draw outside field `DisabledScope`)
- `ListDrawerSettings.DisplayMode`, `DictionaryDrawerSettings.DisplayMode`

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
