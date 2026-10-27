#if UNITY_EDITOR
namespace AetherNexus.FoundationPlatform.Utilities.Menus
{
    /// <summary>
    /// Single source of truth for editor <c>[MenuItem]</c> paths. Grouped by menu area; nested
    /// classes mirror the on-screen submenu hierarchy so the taxonomy is readable at a glance and
    /// reorganizing a group is a one-line edit here rather than a hunt across frameworks.
    /// <para>Create entries use domain roots under <c>Assets/Create</c> (see docs/NAMING.md §4).
    /// <c>Tools/</c> is batch-only: Rebuild, Linting, Utilities, Platform, Domain generators/validation.
    /// Object-scoped work is <c>GameObject/Domain</c> or <c>CONTEXT</c>. Toggles are Project Settings.
    /// <c>Window/Domain</c> holds designer windows; engine debuggers and editor-extension panels sit
    /// under <c>Window/Engine</c>. No submenu holds a single item.</para>
    /// <para>All members are <c>const string</c> (built by concatenating <c>const</c> roots) to
    /// satisfy the <c>[MenuItem]</c> attribute's compile-time-constant requirement.</para>
    /// </summary>
    public static class MenuPaths
    {
        // Top-level menu roots.
        public const string Tools = "Tools/";
        public const string Window = "Window/";
        public const string GameObject = "GameObject/";
        public const string Context = "CONTEXT/";
        public const string Assets = "Assets/";
        public const string Edit = "Edit/";

        // ============================================================================
        // Tools/* — project-wide batch work
        // ============================================================================

        /// <summary>Tools/Rebuild/* — constant/codegen regeneration. The per-generator entries (tags,
        /// animations, animation sets, navmesh areas, shaders) are deliberately not exposed:
        /// <c>Rebuild All Constants</c> runs every one of them and is cheap, so one button beats five.</summary>
        public static class Rebuild
        {
            private const string Root = Tools + "Rebuild/";
            public const string AllConstants        = Root + "Rebuild All Constants";
            public const string Registries          = Root + "Rebuild All Generated Registries";
            public const string PackageIntegrations = Root + "Rebuild Package Integrations";
            public const string ContentCatalog      = Root + "Rebuild Content Catalog";
            public const string AnimationRootMotion = Root + "Bake Animation Root Motion";
        }

        /// <summary>Tools/Linting/* — standalone scanners outside project validation.</summary>
        public static class Linting
        {
            private const string Root = Tools + "Linting/";
            public const string PrintActiveConfigPath = Root + "Print Active Config Path";
            public const string StaleComponentScanner = Root + "Stale Component Scanner";
        }

        /// <summary>Tools/Utilities/* — general-purpose editor utilities.
        /// Fit Anchors Alt+O lives here (not CONTEXT) because CONTEXT menu hotkeys never fire globally.</summary>
        public static class Utilities
        {
            private const string Root = Tools + "Utilities/";
            public const string TakeScreenshot      = Root + "Take Screenshot";
            public const string BakePrefabLightmaps = Root + "Bake Prefab Lightmaps";
            public const string FitAnchors          = Root + "Fit Anchors &o";
            public const string OpenLogsFolder      = Root + "Open Logs Folder";
            public const string OpenPersistentData  = Root + "Open Persistent Data Folder";
        }

        /// <summary>Tools/Platform/* — project setup and publisher coverage passes. Each coverage
        /// pass has one report: it writes the reviewable proposal and warns about the gaps.</summary>
        public static class Platform
        {
            private const string Root = Tools + "Platform/";
            public const string Setup                        = Root + "Project Setup...";
            public const string NetworkValidateSetup         = Root + "Validate Network Setup";
            public const string IconsReportCoverage          = Root + "Icons/Report Coverage";
            public const string IconsAuditSymbols            = Root + "Icons/Audit Symbols";
            public const string IconsStampSymbols            = Root + "Icons/Stamp Symbols Package...";
            public const string IconsGenerateAndStamp        = Root + "Icons/Generate + Stamp Package...";
            public const string ComponentMenusReportCoverage = Root + "Component Menus/Report Coverage";
            public const string ComponentMenusApplyPackage   = Root + "Component Menus/Apply Package...";
            public const string CreateMenusReportCoverage    = Root + "Create Menus/Report Coverage";
            public const string CreateMenusApplyPackage      = Root + "Create Menus/Apply Package...";
        }

        /// <summary>Tools/Domain/* — designer authoring, generation, and validation actions; and the
        /// GameObject/Domain/* object-scoped creates that belong to the same systems.</summary>
        public static class Domain
        {
            private const string Root = Tools + "Domain/";
            private const string GameObjectRoot = GameObject + "Domain/";

            /// <summary>
            /// The single project-wide authoring check. Every validator in the project reports into it, so
            /// there is one entry rather than one per check; scoped runs happen by selecting an asset or
            /// folder, and the detail lives in the Validation window.
            /// </summary>
            public const string ValidateProject                    = Root + "Validate Project";
            public const string CreateNewScene                     = Root + "Create New Scene...";
            public const string GasCreateNewAbility                = Root + "Create New Ability...";
            public const string CharacterCreateDefaultStateProfile = Root + "Create Default Character State Profile";

            public const string InputCreateCharacterProvision = Root + "Input/Create Character Input Setup";
            public const string InputCreateCombatProvision    = Root + "Input/Create Combat Input Setup";
            public const string InputCreatePartyProvision     = Root + "Input/Create Party Input Setup";
            public const string InputIntegration              = Root + "Input/Input Integration";
            public const string InputIntegrateAll             = Root + "Input/Add All Missing Input Actions";

            public const string PlayerCreateInputActions = Root + "Player/Create Player Input Actions";
            public const string PlayerCaptureTouchLayout = Root + "Player/Capture Touch Layout Preset";

            public const string AiSetupPawnContext            = GameObjectRoot + "AI/Setup AI Pawn";
            public const string CharacterReconcileHubContext  = GameObjectRoot + "Character/Reconcile Subsystem Hub";
            public const string CharacterCreateFromArchetype  = GameObjectRoot + "Character/Character From Archetype...";
            public const string PlayerCreateSimplePawn        = GameObjectRoot + "Player/Simple Pawn";
            public const string PlayerCreatePlayerStart       = GameObjectRoot + "Player/Player Start";
            public const string PlayerCreateCameraOrbit       = GameObjectRoot + "Player/Pawn Camera/Third-Person Orbit";
            public const string PlayerCreateCameraTopDown     = GameObjectRoot + "Player/Pawn Camera/Top-Down";
            public const string PlayerCreateCamera2DSide      = GameObjectRoot + "Player/Pawn Camera/2D Side-Follow";
            public const string PlayerCreateTouchControls     = GameObjectRoot + "Player/Touch Controls HUD";
            public const string PlayerCreateTouchJoystick     = GameObjectRoot + "Player/Touch/Joystick";
            public const string PlayerCreateTouchDPad         = GameObjectRoot + "Player/Touch/D-Pad";
            public const string PlayerCreateTouchActionButton = GameObjectRoot + "Player/Touch/Action Button";
            public const string PlayerCreateTouchLookPad      = GameObjectRoot + "Player/Touch/Look Pad";
            public const string PlayerTouchQuickSetup         = GameObjectRoot + "Player/Set Up Touch Controls In Scene";
            public const string LevelCreateForInSceneRoot     = GameObjectRoot + "Level/Create Level For This GameObject";

            /// <summary>Assets/* — one-click LevelDefinition authoring for the GameObject loader kind.</summary>
            public const string LevelCreateForPrefab = Assets + "Create Level For This Prefab";
        }

        // ============================================================================
        // Window/* — editor windows
        // ============================================================================

        /// <summary>Window/Domain/* — designer-facing authoring, preview, and per-system debugger windows.</summary>
        public static class DomainWindow
        {
            private const string Root = Window + "Domain/";
            public const string FindFeature       = Root + "Find Feature...";
            public const string ValidationWindow  = Root + "Validation...";
            public const string GasDebugger       = Root + "GAS/Ability System Debugger...";
            public const string GasTagManager     = Root + "GAS/Gameplay Tag Manager...";
            public const string AiAuthor          = Root + "AI/AI Author...";
            public const string AiBrainGraph      = Root + "AI/AI Brain Graph...";
            public const string AiDebugger        = Root + "AI/AI Debugger...";
            public const string CharacterDebugger = Root + "Character/Character Debugger...";
            public const string CharacterRagdoll  = Root + "Character/Ragdoll Helper...";
            public const string CombatDebugger    = Root + "Combat/Combat Debugger...";
            public const string WeaponWizard      = Root + "Combat/Weapon Wizard...";
            public const string CombatPreview     = Root + "Combat/Combat Preview...";
            // Labels are intent-first ("what the designer wants to do"), not implementation-first,
            // so both the menu and the Feature Finder match the words a designer searches for.
            public const string ItemIkPreview     = Root + "Item/Weapon Grip & Hand IK...";
            public const string ItemIkBake        = Root + "Item/Bake Weapon Grip Into Clips...";
            public const string ItemDebugger      = Root + "Item/Item Debugger...";
            public const string QuestDebugger     = Root + "Quest Debugger...";
            public const string ShopEconomy       = Root + "Shop & Economy Debugger...";
        }

        /// <summary>Window/Engine/* — engine-wide debuggers, diagnostics, and editor-extension panels.</summary>
        public static class EngineWindow
        {
            private const string Root = Window + "Engine/";
            public const string GameState           = Root + "Game State...";
            public const string GameActionMatrix    = Root + "Game Actions...";
            public const string Telemetry           = Root + "Telemetry...";
            public const string AsyncFlowVisualizer = Root + "Async Flow Visualizer...";
            public const string SessionStateAudit   = Root + "Session State Contributor Audit...";
            public const string DebugXConsole       = Root + "DebugX Console...";
            public const string EventBus            = Root + "Event Bus...";
            public const string TweenDebugger       = Root + "Tween Debugger...";
            public const string HierarchySetupPanel = Root + "Hierarchy Setup Panel";
            public const string ProjectContextPanel = Root + "Project Context Panel";
            public const string AnimationTestBench  = Root + "Animation Test Bench";
            public const string AetherInspectorDemo = Root + "AetherInspector Demo";
        }

        /// <summary>Window/Utilities/* — utility windows.</summary>
        public static class WindowUtilities
        {
            private const string Root = Window + "Utilities/";
            public const string SceneSwitcher      = Root + "Scene Switcher...";
            public const string ScriptGenerator    = Root + "Script Generator...";
            public const string PresetAutomation   = Root + "Preset Automation Settings...";
            public const string AutoBinder         = Root + "Auto Binder...";
            public const string CreateEventChannel = Root + "Create Event Channel...";
        }

        /// <summary>Window/UI Widgets... — widget browser (bare leaf: a folder of one is a wasted click).</summary>
        public static class UIWidgets
        {
            public const string WidgetsWindow = Window + "UI Widgets...";
        }

        // ============================================================================
        // GameObject/* — scene creation and editing helpers
        // ============================================================================

        /// <summary>Edit/* and GameObject/* — hierarchy window enhancer. The toggle carries its %h
        /// hotkey inside the path, so <c>Menu.SetChecked</c> must pass this same const.</summary>
        public static class HierarchyX
        {
            public const string Toggle       = Edit + "HierarchyX Enabled %h";
            public const string CreateHeader = GameObject + "Header";
        }

        /// <summary>GameObject/* — scene editing helpers (EditorEnhancerX). Not brand-wrapped; these are
        /// Unity-style GameObject menu entries, same convention as GameObject/3D Object, GameObject/UI.</summary>
        public static class EditorEnhancer
        {
            public const string DropToFloor    = GameObject + "Drop To Floor";
            public const string GroupSelection = GameObject + "Group Selection";
            public const string Ungroup        = GameObject + "Ungroup";
        }

        /// <summary>GameObject/UI (Canvas)/* — flat UI create entries beside Unity's own.</summary>
        public static class UICanvas
        {
            public const string Root                   = GameObject + "UI (Canvas)/";
            public const string UpgradeTextToTmp       = Root + "Upgrade Text to TextMeshProUGUI";
            public const string TutorialSpotlight      = Root + "Tutorial Spotlight Overlay";
        }

        // ============================================================================
        // CONTEXT/* — component right-click actions
        // ============================================================================

        /// <summary>CONTEXT/Component/* — generic component context-menu utilities (AetherInspector).
        /// Value transfer sits in one Values/ submenu so the shared component menu stays short.</summary>
        public static class ContextComponent
        {
            private const string Root = Context + "Component/";
            private const string Values = Root + "Values/";
            public const string MoveToTop           = Root + "Move To Top";
            public const string MoveToBottom        = Root + "Move To Bottom";
            public const string FoldAllComponents   = Root + "Fold All Components";
            public const string ExpandAllComponents = Root + "Expand All Components";
            public const string CopyValuesAsJson              = Values + "Copy As JSON";
            public const string PasteValuesFromJson           = Values + "Paste From JSON";
            public const string SaveValuesToJsonFile          = Values + "Save To JSON File...";
            public const string LoadValuesFromJsonFile        = Values + "Load From JSON File...";
            public const string SaveValuesWhenExitingPlayMode = Values + "Save When Exiting Play Mode";
        }

        /// <summary>CONTEXT/MonoBehaviour/* — script duplication utilities.</summary>
        public static class ScriptDuplicator
        {
            private const string Root = Context + "MonoBehaviour/";
            public const string Duplicate           = Root + "Duplicate";
            public const string DuplicateAndReplace = Root + "Duplicate and Replace";
            public const string ReplaceScript       = Root + "Replace Script...";
        }

        /// <summary>CONTEXT/Button/* — UGUI button upgrade.</summary>
        public static class ButtonContext
        {
            public const string UpgradeToButtonX = Context + "Button/Upgrade To ButtonX";
        }

        /// <summary>CONTEXT/RectTransform/Fit Anchors — right-click only (no hotkey).</summary>
        public static class UIWidgetsContext
        {
            public const string FitAnchors = Context + "RectTransform/Fit Anchors";
        }

        /// <summary>CONTEXT/&lt;character component&gt;/* — component right-click actions on a character rig.</summary>
        public static class CharacterContext
        {
            public const string ReconcileSubsystemHub = Context + "CharacterSubsystemHub/Reconcile Subsystem Hub";
            public const string OpenRagdollHelper     = Context + "BoneMapper/Open Ragdoll Helper";
        }

        /// <summary>CONTEXT/CombatComponent/* — right-click actions on the fighter that owns the attack.</summary>
        public static class CombatContext
        {
            public const string OpenCombatPreview = Context + "CombatComponent/Open Combat Preview";
        }

        /// <summary>CONTEXT/&lt;equipment component&gt;/* — right-click actions on the character that carries the item.</summary>
        public static class ItemContext
        {
            public const string OpenIkPreview         = Context + "EquipmentIKRuntime/Open Weapon Grip & Hand IK";
            public const string OpenEquipmentBindings = Context + "InventoryComponent/Open Weapon Attach Points & Holsters";
        }

        // ============================================================================
        // Assets/* — Project window
        // ============================================================================

        /// <summary>Assets/Create/* — guided scaffolding that a bare [CreateAssetMenu] cannot do.</summary>
        public static class Create
        {
            private const string Root = Assets + "Create/";
            public const string ContentArea      = Root + "Content Area";
            public const string ItemEquippable   = Root + "Item/Equippable Item";
            public const string ItemConsumable   = Root + "Item/Consumable Item";
            public const string ItemEquipmentKit = Root + "Item/Equipment Kit";
        }

        /// <summary>Assets/* — content-area actions on the selected asset or folder.</summary>
        public static class AssetsContent
        {
            public const string CreateDomainEvent = Assets + "Create Domain Event";
            public const string FixOutOfSync      = Assets + "Fix Out of Sync";
        }

        /// <summary>Assets/Create/From Clipboard/* — create text assets from the system clipboard.</summary>
        public static class AssetsCreate
        {
            private const string Root = Assets + "Create/From Clipboard/";
            public const string CSharpScript = Root + "C# Script";
            public const string Shader       = Root + "Shader";
            public const string TextFile     = Root + "Text File";
        }

        /// <summary>Assets/Import Package/* — package-to-folder import helper.</summary>
        public static class AssetsImport
        {
            private const string Root = Assets + "Import Package/";
            public const string Here = Root + "Here...";
        }
    }
}
#endif
