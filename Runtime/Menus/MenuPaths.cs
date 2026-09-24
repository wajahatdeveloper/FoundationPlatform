#if UNITY_EDITOR
namespace AetherNexus.FoundationPlatform.Utilities.Menus
{
    /// <summary>
    /// Single source of truth for editor <c>[MenuItem]</c> paths. Grouped by menu area; nested
    /// classes mirror the on-screen submenu hierarchy so the taxonomy is readable at a glance and
    /// reorganizing a group is a one-line edit here rather than a hunt across frameworks.
    /// <para>Create entries use domain roots under <c>Assets/Create</c> (see docs/NAMING.md §4).
    /// <c>Tools/</c> is batch-only: Rebuild, Linting, Platform, leftover Domain generators/validation.
    /// Object-scoped work is <c>GameObject/Domain</c> or <c>CONTEXT</c>. Toggles are Project Settings.</para>
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
        // FoundationPlatform — unwrapped, flat Tools/<Category>/*, Window/<Category>/*
        // (no brand folder: reads as generic/native engine tooling, not a product)
        // ============================================================================

        /// <summary>Tools/Rebuild/* — constant/codegen regeneration. The per-generator entries (tags,
        /// animations, animation sets, navmesh areas, shaders) are deliberately not exposed:
        /// <c>Rebuild All Constants</c> runs every one of them and is cheap, so one button beats five.</summary>
        public static class Rebuild
        {
            private const string Root = Tools + "Rebuild/";
            public const string AllConstants     = Root + "Rebuild All Constants";
            public const string GasAbilityLogic  = Root + "GAS/Rebuild Ability Logic";
            public const string Registries       = Root + "Rebuild All Generated Registries";
            public const string PackageIntegrations = Root + "Rebuild Package Integrations";
        }

        /// <summary>Tools/Debug/* — debug filesystem/trace toggles &amp; monitors.</summary>
        public static class Debug
        {
            private const string Root = Tools + "Debug/";
            public const string OpenLogsFolder    = Root + "Open Logs Folder";
            public const string OpenPersistentData = Root + "Open Persistent Data Folder";
        }

        /// <summary>Tools/Utilities/* — general-purpose editor utilities.</summary>
        public static class Utilities
        {
            private const string Root = Tools + "Utilities/";
            public const string TakeScreenshot       = Root + "Take Screenshot";
            public const string ImageToStringConverter = Root + "Image To String Converter";
            public const string BakePrefabLightmaps  = Root + "Bake Prefab Lightmaps";
        }

        /// <summary>Tools/Diagnostics/* — dev/demo diagnostic tools.</summary>
        public static class Diagnostics
        {
            private const string Root = Window + "Diagnostics/";
            public const string AnimationTestBench   = Root + "Animation Test Bench";
            public const string AetherInspectorDemo = Root + "AetherInspector Demo";
        }

        /// <summary>Tools/Linting/* — validation &amp; rollout toggles (FoundationPlatform-owned half of the former shared Linting class).</summary>
        public static class Linting
        {
            private const string Root = Tools + "Linting/";
            public const string PrintActiveConfigPath = Root + "Print Active Config Path";
            public const string StaleComponentScanner = Root + "Stale Component Scanner";
            public const string MissingDesignerIcons = Root + "Report Missing Designer Icons";
            public const string MissingComponentMenus = Root + "Report Missing Component Menus";
            public const string MissingCreateMenus   = Root + "Report Missing Create Menus";
        }

        /// <summary>Window/Utilities/* — utility windows.</summary>
        public static class WindowUtilities
        {
            private const string Root = Window + "Utilities/";
            public const string SceneSwitcher    = Root + "Scene Switcher...";
            public const string ScriptGenerator  = Root + "Script Generator...";
            public const string PresetAutomation = Root + "Preset Automation Settings...";
            public const string AutoBinder       = Root + "Auto Binder...";
            public const string CreateEventChannel = Root + "Create Event Channel...";
        }

        /// <summary>Window/* — single-window classes stay bare leaves (no folder-of-one) rather than
        /// forcing a click through a submenu that will only ever hold this one item.</summary>
        public static class WindowDebugX
        {
            public const string DebugXConsole = Window + "DebugX Console...";
        }

        /// <summary>Window/* — event bus window (bare leaf, see WindowDebugX remark).</summary>
        public static class WindowEventBus
        {
            public const string EventBus = Window + "Event Bus...";
        }

        /// <summary>Window/* — tween debugger window (bare leaf, see WindowDebugX remark).</summary>
        public static class WindowTweenX
        {
            public const string TweenDebugger = Window + "Tween Debugger...";
        }

        /// <summary>Edit/*, Window/HierarchyX/*, GameObject/* — hierarchy window enhancer. The toggle
        /// carries its %h hotkey inside the path, so <c>Menu.SetChecked</c> must pass this same const.</summary>
        public static class HierarchyX
        {
            public const string Toggle       = Edit + "HierarchyX Enabled %h";
            public const string SetupPanel   = Window + "HierarchyX/Setup Panel";
            public const string CreateHeader = GameObject + "Header";
        }

        /// <summary>Window/ProjectWindowX/* — project window enhancer.</summary>
        public static class ProjectWindowX
        {
            public const string ContextPanel = Window + "ProjectWindowX/Context Panel";
        }

        /// <summary>GameObject/* — scene editing helpers (EditorEnhancerX). Not brand-wrapped; these are
        /// Unity-style GameObject menu entries, same convention as GameObject/3D Object, GameObject/UI.</summary>
        public static class EditorEnhancer
        {
            public const string DropToFloor    = GameObject + "Drop To Floor";
            public const string GroupSelection = GameObject + "Group Selection";
            public const string Ungroup        = GameObject + "Ungroup";
        }

        /// <summary>CONTEXT/Component/* — generic component context-menu utilities (AetherInspector).</summary>
        public static class ContextComponent
        {
            private const string Root = Context + "Component/";
            public const string MoveToTop        = Root + "Move To Top";
            public const string MoveToBottom     = Root + "Move To Bottom";
            public const string CopyValuesAsJson = Root + "Copy Values As JSON";
            public const string PasteValuesFromJson = Root + "Paste Values From JSON";
            public const string SaveValuesToJsonFile = Root + "Save Values To JSON File...";
            public const string LoadValuesFromJsonFile = Root + "Load Values From JSON File...";
            public const string SaveValuesWhenExitingPlayMode = Root + "Save Values When Exiting Play Mode";
            public const string ForceRebuildInspectorCache = Root + "Force Rebuild AetherInspector Cache";
            public const string FoldAllComponents   = Root + "Fold All Components";
            public const string ExpandAllComponents = Root + "Expand All Components";
        }

        /// <summary>CONTEXT/MonoBehaviour/* — script duplication utilities.</summary>
        public static class ScriptDuplicator
        {
            private const string Root = Context + "MonoBehaviour/";
            public const string Duplicate          = Root + "Duplicate";
            public const string DuplicateAndReplace = Root + "Duplicate and Replace";
            public const string ReplaceScript      = Root + "Replace Script...";
        }

        /// <summary>Assets/Create/From Clipboard/* — create text assets from the system clipboard.</summary>
        public static class AssetsCreate
        {
            private const string Root = Assets + "Create/From Clipboard/";
            public const string CSharpScript = Root + "C# Script";
            public const string Shader       = Root + "Shader";
            public const string TextFile     = Root + "Text File";
        }

        /// <summary>Assets/Create/Item/* — guided item scaffolding that a bare [CreateAssetMenu] cannot do.</summary>
        public static class Create
        {
            private const string Root = Assets + "Create/";
            public const string ItemEquippable   = Root + "Item/Equippable Item";
            public const string ItemConsumable   = Root + "Item/Consumable Item";
            public const string ItemEquipmentKit = Root + "Item/Equipment Kit";
        }

        /// <summary>Assets/Import Package/* — package-to-folder import helper.</summary>
        public static class AssetsImport
        {
            private const string Root = Assets + "Import Package/";
            public const string Here = Root + "Here...";
        }

		// ============================================================================
		// UIWidgets — Window/Tools branded; Hierarchy create items are flat under GameObject/UI (Canvas)/
		// ============================================================================

		/// <summary>Window/UIWidgets/* and GameObject/UI (Canvas)/* — widget browser + flat create entries.</summary>
		public static class UIWidgets
		{
			private const string WindowRoot = Window + "UIWidgets/";
			public const string WidgetsWindow = WindowRoot + "UI Widgets...";
			public const string GameObjectOpen = GameObject + "UI (Canvas)/Open UI Widgets Window...";
		}

        /// <summary>Tools/UIWidgets/* — items with no natural home in a native Unity menu.
        /// Fit Anchors Alt+O lives here (not CONTEXT) because CONTEXT menu hotkeys never fire globally.</summary>
        public static class UIWidgetsTools
        {
            private const string Root = Tools + "UIWidgets/";
            public const string FitAnchors         = Root + "Fit Anchors &o";
            public const string Settings           = Root + "Settings...";
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
		// Designer-facing menus — Domain / Platform only (no dual legacy registration).
		// Domain: reach a tool by *what it does*. Platform: project setup / package integration.
		// ============================================================================

		/// <summary>Tools/Domain/<System>/* — designer authoring, generation, and validation actions.</summary>
		public static class Domain
		{
			private const string Root = Tools + "Domain/";

			/// <summary>Tools/Domain/GAS/* — GAS authoring and codegen.</summary>
			public const string GasRebuildAbilityLogic          = Rebuild.GasAbilityLogic;
			public const string GasMigrateEffectIdentityTags    = Root + "GAS/Migrate Effect Identity Tags";
			public const string GasCreateNewAbility             = Root + "GAS/Create New Ability...";

			/// <summary>Tools/Domain/AI/* — AI authoring and generators.</summary>
			public const string AiGenerateCommanderBrain        = Root + "AI/Generate Default Commander Brain";
			public const string AiGenerateDecisionSet           = Root + "AI/Generate Default Decision Set";
			public const string AiGenerateBuiltInBehaviors      = Root + "AI/Generate Built-in Behavior Assets";
			public const string AiGenerateBlackboardRegistry    = Root + "AI/Generate Blackboard List";
			public const string AiSetupPawnContext              = GameObject + "Domain/AI/Setup AI Pawn";

			/// <summary>Tools/Domain/Character/* — character setup.</summary>
			public const string CharacterCreateDefaultStateProfile = Root + "Character/Create Default Character State Profile";
			public const string CharacterReconcileHubContext       = GameObject + "Domain/Character/Reconcile Subsystem Hub";
			public const string CharacterCreateFromArchetype       = GameObject + "Domain/Character/Character From Archetype...";

			/// <summary>Tools/Domain/Economy/* — currency registry tooling.</summary>
			public const string EconomyRefreshCurrencyRegistries = Root + "Economy/Refresh Currency Registries";

			/// <summary>Tools/Domain/Player/* and GameObject/Domain/Player/* — player &amp; pawn authoring.</summary>
			public const string PlayerCreateInputActions        = Root + "Player/Create Player Input Actions";
			public const string PlayerFillRosterPrefabs         = Root + "Player/Fill Roster Prefabs From Prefab Map";
			public const string PlayerCreateSimplePawn          = GameObject + "Domain/Player/Simple Pawn";
			public const string PlayerCreatePlayerStart         = GameObject + "Domain/Player/Player Start";
			public const string PlayerCreateTouchControls       = GameObject + "Domain/Player/Touch Controls HUD";
			public const string PlayerCreateTouchJoystick       = GameObject + "Domain/Player/Touch/Joystick";
			public const string PlayerCreateTouchDPad           = GameObject + "Domain/Player/Touch/D-Pad";
			public const string PlayerCreateTouchActionButton   = GameObject + "Domain/Player/Touch/Action Button";
			public const string PlayerCreateTouchLookPad        = GameObject + "Domain/Player/Touch/Look Pad";
			public const string PlayerTouchQuickSetup           = GameObject + "Domain/Player/Set Up Touch Controls In Scene";
			public const string PlayerCaptureTouchLayout        = Root + "Player/Capture Touch Layout Preset";

			/// <summary>Assets/* and GameObject/Domain/Level/* — one-click LevelDefinition authoring for the GameObject loader kind.</summary>
			public const string LevelCreateForPrefab            = Assets + "Create Level For This Prefab";
			public const string LevelCreateForInSceneRoot       = GameObject + "Domain/Level/Create Level For This GameObject";

			/// <summary>Tools/Domain/Input/* — input provision authoring.</summary>
			public const string InputCreateCharacterProvision     = Root + "Input/Create Character Input Setup";
			public const string InputCreateCombatProvision        = Root + "Input/Create Combat Input Setup";
			public const string InputCreatePartyProvision         = Root + "Input/Create Party Input Setup";
			public const string InputIntegration                  = Root + "Input/Input Integration";

			/// <summary>Tools/Domain/PresetLibrary/* — preset asset generation.</summary>
			public const string PresetLibraryGenerateAll          = Root + "PresetLibrary/Generate All";
			public const string PresetLibraryRegisterTagsOnly     = Root + "PresetLibrary/Register Tags Only";
			public const string PresetLibraryGenerateGas          = Root + "PresetLibrary/Generate Gameplay Abilities (Attributes + Effects + Cues)";
			public const string PresetLibraryGenerateCharactersItems = Root + "PresetLibrary/Generate Characters + Items";
			public const string PresetLibraryGenerateArchetypesAi = Root + "PresetLibrary/Generate Archetypes + AI";

			/// <summary>
			/// The single project-wide authoring check. Every validator in the project reports into it, so
			/// there is one entry rather than one per check; scoped runs happen by selecting an asset or
			/// folder, and the detail lives in Central Validation.
			/// </summary>
			public const string ValidateProject                   = Root + "Validate Project";

			/// <summary>Tools/Domain/* — one-click scaffolding.</summary>
			public const string CreateNewDomain                   = Root + "Create New Content Area...";
			public const string CreateNewScene                    = Root + "Create New Scene...";
			public const string CreateBootScene                   = Root + "Set Up Boot Scene";
			public const string CreateDomainEvent                = Root + "Create Domain Event...";
		}

		/// <summary>Window/Domain/<System>/* — designer-facing debugger and preview windows.</summary>
		public static class DomainWindow
		{
			private const string Root = Window + "Domain/";
			public const string FindFeature        = Root + "Find Feature...";
			public const string GasDebugger        = Root + "GAS/Ability System Debugger...";
			public const string GasTagManager      = Root + "GAS/Gameplay Tag Manager...";
			public const string AiAuthor           = Root + "AI/AI Author...";
			public const string AiDebugger         = Root + "AI/AI Debugger...";
			public const string CharacterDebugger  = Root + "Character/Character Debugger...";
			public const string CharacterLocomotion = Root + "Character/Locomotion Blend Debug...";
			public const string CharacterRagdoll   = Root + "Character/Ragdoll Helper...";
			public const string CombatDebugger     = Root + "Combat/Combat Debugger...";
			public const string WeaponWizard       = Root + "Combat/Weapon Wizard...";
			public const string CombatPreview      = Root + "Combat/Combat Preview...";
			// Labels are intent-first ("what the designer wants to do"), not implementation-first,
			// so both the menu and the Feature Finder match the words a designer searches for.
			public const string ItemRig            = Root + "Item/Weapon Attach Points & Holsters...";
			public const string ItemIkPreview      = Root + "Item/Weapon Grip & Hand IK...";
			public const string ItemDebugger       = Root + "Item/Item Debugger...";
			public const string ItemEquipKit       = Root + "Item/Equip Weapon On Character...";
			public const string QuestDebugger      = Root + "Quest/Quest Debugger...";
			public const string ShopEconomy        = Root + "Shop/Shop & Economy Debugger...";
			public const string CentralWindow      = Root + "Central Validation...";
			public const string GameState         = Root + "Game State...";
			public const string GameActionMatrix   = Root + "Game Actions...";
			public const string AsyncFlowVisualizer = Root + "Async Flow Visualizer...";
			public const string Telemetry          = Root + "Telemetry...";
			public const string SessionStateAudit  = Root + "Session State Contributor Audit...";
		}

		/// <summary>Tools/Platform/* — project setup, package integration, registry refresh.</summary>
		public static class Platform
		{
			private const string Root = Tools + "Platform/";
            public const string Setup               = Root + "Project Setup...";
            public const string RegistryRefresh     = Rebuild.Registries;
            public const string PackageRebuild      = Rebuild.PackageIntegrations;
            public const string NetworkValidateSetup = Root + "Network/Validate Setup";
			public const string AgentToolsExportParams = Root + "Agent Tools/Export MCP Params";
			public const string IconsReportCoverage = Root + "Icons/Report Coverage";
			public const string IconsAuditSymbols   = Root + "Icons/Audit Symbols";
			public const string IconsStampSymbols   = Root + "Icons/Stamp Symbols Package...";
			public const string IconsGenerateAndStamp = Root + "Icons/Generate + Stamp Package...";
            public const string ComponentMenusReportCoverage = Root + "Component Menus/Report Coverage";
            public const string ComponentMenusApplyPackage = Root + "Component Menus/Apply Package...";
            public const string CreateMenusReportCoverage = Root + "Create Menus/Report Coverage";
            public const string CreateMenusApplyPackage = Root + "Create Menus/Apply Package...";
		}

		/// <summary>Window/Platform/* — admin / integration windows.</summary>
		public static class PlatformWindow
		{
			private const string Root = Window + "Platform/";
			public const string PackageIntegration  = Root + "Package Integration...";
		}
	}
}
#endif
