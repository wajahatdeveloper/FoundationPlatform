# FrameworkInspector

HOMAM's in-house inspector attribute engine (Odin-style surface, IMGUI renderer). Runtime attributes live in `Runtime/FrameworkInspector/FrameworkInspectorAttributes.cs`; editor implementation is under `Editor/FrameworkInspector/`.

## Opt-in

**Components / ScriptableObjects** — global fallback applies automatically via `FrameworkFallbackEditor`. For explicit control or append UI:

```csharp
[CustomEditor(typeof(MyType))]
public sealed class MyTypeEditor : FrameworkEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        // Optional summary using GuiKit (not raw HelpBox):
        GuiKit.InfoBox("Extra context.", InfoMessageType.Info);
    }
}
```

**Nested `[Serializable]` list elements** — register a 3-line drawer so `ShowIf` / `ReadOnly` / buttons work inside lists:

```csharp
[CustomPropertyDrawer(typeof(MyPayload))]
internal sealed class MyPayloadDrawer : FrameworkReflectedDrawer { }
```

See `FrameworkReflectedDrawers.cs` in each framework's Editor folder for registered payload types.

**Editor windows** — use `GuiKit` / `FrameworkInspectorTheme` for boxes, titles, info callouts, and toolbars.

## Visual regression harness

Menu: **Tools → HOMAM → Framework Inspector Demo**. Exercises the attribute surface through `FrameworkInspectorDemoData`.

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

### API-only (declared, not fully implemented)

- `ValueDropdown`: `AppendNextDrawer`, `IsUniqueList`, `DisableListAddButtonBehaviour`, `HideChildProperties`, `ExpandAllMenuItems`
- `AssetSelector`: `ExcludeExistingValuesInList`, `ExpandAllMenuItems`
- `ShowIf` / `HideIf`: `Animate`
- `ToggleGroup`: `CollapseOthersOnExpand`
- `InfoBox`: `GUIAlwaysEnabled` (member-level boxes already draw outside field `DisabledScope`)
- `ListDrawerSettings.DisplayMode`, `DictionaryDrawerSettings.DisplayMode`

## Editor extension guidelines

1. **Prefer attributes on runtime types** over custom IMGUI in editors.
2. **Extend `FrameworkEditor`** and call `base.OnInspectorGUI()` first.
3. **Append UI** with `GuiKit.Title`, `GuiKit.InfoBox`, `GuiKit.ValidationBox` — not `EditorGUILayout.HelpBox` / raw `EditorStyles.boldLabel`.
4. **Nested payloads in lists** need `FrameworkReflectedDrawer` unless a bespoke `PropertyDrawer` already exists (e.g. `AttackEntry`, `ConsiderationPayload`).
5. **Do not hand-edit generated code** — change sources and regenerate per project conventions.
6. **Cache busting**: context menu *Force Rebuild Framework Inspector Cache* on components if metadata looks stale.
7. **Foldouts / headers**: use the canonical flat widgets — `FrameworkInspectorTheme.SectionFoldout` (full-width, click-anywhere group header), or `EditorGUILayout/EditorGUI.Foldout(..., FrameworkInspectorTheme.FlatFoldoutStyle)` for layout foldouts (lists, nested structs). Header labels use `FrameworkInspectorTheme.FlatHeaderLabel` (bold 12pt); lead space before any header is `FrameworkInspectorTheme.HeaderSpacing`. Do **not** reintroduce bespoke `EditorStyles.foldoutHeader` bars or ad-hoc `Space(4)` — it breaks the flat/compact consistency.

## Key types

| Type | Role |
|------|------|
| `FrameworkEditor` | Base inspector |
| `FrameworkFallbackEditor` | Global fallback for all `UnityEngine.Object` |
| `FrameworkInspectorRenderer` | Attribute tree + field drawer engine |
| `FrameworkInspectorTheme` | Skin-aware colors, styles, layout helpers |
| `GuiKit` | Public facade for editor windows |
| `PocoInspector` | Reflection drawer for non-serialized members |
| `FrameworkReflectedDrawer` | PropertyDrawer base for nested serializable types |
| `EngineListDrawer` / `EngineDictionaryDrawer` / `TableRenderer` | Collection renderers |
