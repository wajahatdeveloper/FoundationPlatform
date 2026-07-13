# Editor Theme Migration

Guide + backlog for moving hand-drawn IMGUI editor chrome onto the FrameworkInspector theme (`GuiKit` / `FrameworkInspectorTheme`) so every inspector, drawer, and window reads as one system (flat, compact, skin-aware).

See [FrameworkInspector.md](FrameworkInspector.md) for the theme engine itself.

## Recipe (mechanical mapping)

| Raw | Themed |
|-----|--------|
| `EditorGUILayout.HelpBox(msg, Info/Warning/Error)` | `GuiKit.InfoBox(msg, InfoMessageType.…)` / `GuiKit.ValidationBox(msg)` |
| `EditorStyles.boldLabel` **section** label | `GuiKit.Title("…")` / `FrameworkInspectorTheme.FlatHeaderLabel` |
| `EditorGUILayout.Foldout` / `EditorStyles.foldoutHeader` | `GuiKit.SectionFoldout(...)` / `FrameworkInspectorTheme.FlatFoldoutStyle` |
| hand-laid foldout header + trailing buttons (rect-based drawer) | `GuiKit.SectionHeaderRow(rect, label, expanded, n, out trailing)` |
| `EditorGUILayout.BeginVertical(EditorStyles.helpBox)` … `EndVertical` | `GuiKit.BeginBox()` … `GuiKit.EndBox()` |
| tag/chip pill (bg + accent + remove) | `GuiKit.TagPill(rect, content, accent, onRemove)` |
| color swatch outline (`Handles.DrawSolidRectangleWithOutline`) | `GuiKit.RectOutline(rect, GuiKit.ChipOutline)` |
| plain `Editor` base | `FrameworkEditor` (call `base.OnInspectorGUI()` first for field-editing inspectors; skip base for fully bespoke management UIs) |
| plain nested `[Serializable]` `PropertyDrawer` | `FrameworkReflectedDrawer` |
| ad-hoc `new GUIStyle(...)` | a theme style (`FlatHeaderLabel`, `HeaderButton`, `TagChipText`, …) |
| hardcoded colors / `GUI.color` tints | theme color tokens (`SectionRuleColor`, `BoxHeaderBackground`, `TagChip*`, `MenuSelectionBackground`) |

**Keep as-is** (semantic, no theme equivalent): red danger button tints, green/red validation label colors, blue confirm accents, opaque popup/window background fills (theme tokens are semi-transparent). Stock `linkLabel`/`miniLabel`/`centeredGreyMiniLabel` used for value rows or hint text are fine.

`InfoMessageType` lives in namespace `FoundationPlatform.FrameworkInspector`; `GuiKit`/`FrameworkInspectorTheme` in `FoundationPlatform.FrameworkInspector.Editor`. The consuming asmdef must reference the FoundationPlatform editor assembly.

## Done (reference implementation)

The **GameplayTag editor cluster** (`Assets/Frameworks/GameplayAbilitySystem/GameplayTagSystem/Editor/`) is fully migrated and is the canonical example:
- `PropertyDrawers/GameplayTagPropertyDrawer.cs` (GameplayTag / Container / List drawers + `DrawTagPill` → `GuiKit.TagPill`)
- `Components/GameplayTagComponentEditor.cs`
- `Inspectors/GameplayTagDatabaseInspector.cs`
- `Windows/{GameplayTagPickerWindow, TagManagerWindow, GameplayTagCreateWindow, GameplayTagFindReferencesWindow}.cs`

This pass also added the shared helpers to the package: `GuiKit.TagPill` / `SectionHeaderRow` / `RectOutline` / `HeaderButton` / chip color tokens (`TagChipBackground`/`TagChipOutline`/`TagChipAccentFallback`/`TagWarningAccent`) in `FrameworkInspectorTheme`.

## Backlog (not yet migrated)

Prioritized; each tier applies the recipe above.

### Tier 1 — residual chrome in inspectors already on `FrameworkEditor`
Lowest risk (base class already correct; just body chrome). ~15 files. Representative:
`GAS/Editor/AbilitySystemComponentEditor.cs`, `GAS/Editor/Attributes/AttributeSetInspector.cs`, `AttributeContainerEditor.cs`, `GameEngineCore/Editor/Bootstrap/ServiceManifestEditor.cs`, `AISystem/Editor/AIBehaviorComponentEditor.cs`, `AIArchetypeEditor.cs`, `CombatSystem/Editor/Drawers/WeaponCombatProfileDrawer.cs`, `CombatComponentEditor.cs`, `GameEngineCore/Editor/Registries/GeneratedRegistryEditorBase.cs` + `LevelRegistryEditor`, `GameEngineCore/Editor/Player/{HumanIntentDriverEditor,GameSessionConfigEditor}.cs`, `PackageIntegrationManifestEditor.cs`.

### Tier 2 — plain `Editor`s needing base swap → `FrameworkEditor`
`GameEngineCore/Editor/Registries/…LevelDefinitionEditor`, `GameEngineCore/Editor/Network/NetworkConfigEditor.cs`, `GameEngineCore/Editor/Bootstrap/GameBootstrapEditor.cs`, `CharacterSystem/Editor/…/KinematicCharacterMotorEditor.cs`, `CharacterColliderDefinitionEditor.cs`; package inspectors `com.homam.foundationplatform/Editor/Utilities/{CommentEditor,InspectorSeparatorEditor}.cs`, `Gizmos/*Editor.cs`, `CoroutineX/CoroutineXOwnerEditor.cs`, `Identity/IdentityComponentEditor.cs`, `Tools/PrefabLightmapGenerator/…/PrefabLightmapDataEditor.cs`; `com.homam.uiwidgets/Editor/{AutoUIRefsEditor,PanelBaseEditor,Layout/LayoutXEditor}.cs` and slider editors (`BoxSlider`/`RangeSlider`/`MinMaxSlider` — derive from `SelectableEditor`; compose, don't drop the Selectable transitions UI).

### Tier 3 — plain `PropertyDrawer`s → `FrameworkReflectedDrawer` / GuiKit
`CombatSystem/Editor/Drawers/AttackEntryDrawer.cs`, `AISystem/Editor/ConsiderationPayloadDrawer.cs`, `GAS/Editor/Attributes/AttributeOverridePropertyDrawer.cs`, `com.homam.foundationplatform/Editor/AnimGraph/{AnimationSetLinkPropertyDrawer, AnimationSet*Drawer}.cs`.

### Tier 4 — IMGUI windows + their presenter/host/section delegates (largest volume)
`GameEngineCore/Editor/CentralAuthoring/*` hosts + presenters, `ItemSystem/Editor/IKPreview/*` panels, `com.homam.foundationplatform/Editor/Messaging/EventBus/*Window.cs`, tools windows (`AutoBinderWindow`, `SceneSwitcherWindow`, `ScriptGeneratorWindow`, `AnimationTestBenchWindow`, `TweenDebuggerWindow`, …), `AnimationSetEditor.cs` (half-migrated), and the `*SettingsProvider` classes (`OnGUI` section headers + HelpBox). `FrameworkDebuggerWindow<T>`-derived windows are GOOD except residual body chrome.

### Out of scope
UITK / UIElements windows (DebugX Console, HierarchyX docked panel, UIWidgets window, ScenePicker, `EntityDebuggerOverlay` UITK parts) and `GameplayTagRenameWindow` — these use `CreateGUI`/`rootVisualElement`, not IMGUI, so the IMGUI theme doesn't apply.

### Deferred / low-priority
`GameplayTagTreeView.RowGUI` package-tag yellow accent (`new Color(1f,0.97f,0f)`) — could route through a theme accent token; cosmetic, tree-row only.
