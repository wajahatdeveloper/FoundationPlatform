#if UNITY_EDITOR
namespace AetherNexus.FoundationPlatform.Utilities.Menus
{
    /// <summary>
    /// Single source of truth for editor <c>[MenuItem]</c> paths. Grouped by menu area; nested
    /// classes mirror the on-screen submenu hierarchy so the taxonomy is readable at a glance and
    /// reorganizing a group is a one-line edit here rather than a hunt across frameworks.
    /// <para>Product/framework tooling uses designer-facing <c>Tools/Domain/*</c>,
    /// <c>Window/Domain/*</c>, <c>Tools/Platform/*</c>, and <c>Window/Platform/*</c> only — no
    /// dual registration under legacy <c>Tools/GameEngineCore/*</c> / <c>Window/GameEngineCore/*</c>.
    /// FoundationPlatform stays unwrapped (Debug, Utilities, Diagnostics, Rebuild, Linting under
    /// <c>Tools/</c>/<c>Window/</c>). UIWidgets keeps its own root.</para>
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

        // ============================================================================
        // FoundationPlatform — unwrapped, flat Tools/<Category>/*, Window/<Category>/*
        // (no brand folder: reads as generic/native engine tooling, not a product)
        // ============================================================================

        /// <summary>Tools/Rebuild/* — constant/codegen regeneration.</summary>
        public static class Rebuild
        {
            private const string Root = Tools + "Rebuild/";
            public const string TagsLayersScenes = Root + "Rebuild Tags, Layers and Scenes";
            public const string AllConstants     = Root + "Rebuild All Constants";
            public const string Animations       = Root + "Rebuild Animations Constants";
            public const string NavMeshAreas     = Root + "Rebuild NavMesh Areas Constants";
            public const string Shaders          = Root + "Rebuild Shaders Constants";
            public const string AnimationSet     = Root + "Rebuild Animation Set Constants";
        }

        /// <summary>Tools/Debug/* — debug filesystem/trace toggles &amp; monitors.</summary>
        public static class Debug
        {
            private const string Root = Tools + "Debug/";
            public const string OpenLogsFolder    = Root + "Open Logs Folder";
            public const string OpenPersistentData = Root + "Open Persistent Data Folder";
            public const string CaptureFullStackTraces = Root + "Capture Full Stack Traces";
            public const string SyncConsole        = Root + "Sync Console";
            public const string MonitorEventBus    = Root + "Monitor Event Bus (toggle)";
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
            public const string RunFullScan          = Root + "Run Full Scan";
            public const string PrintActiveConfigPath = Root + "Print Active Config Path";
            public const string RolloutWarningFirst  = Root + "Rollout Mode: Warning First";
            public const string RolloutStrict        = Root + "Rollout Mode: Strict";
            public const string StaleComponentScanner = Root + "Stale Component Scanner";
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
            public const string ScenePickerEnabled = Root + "Scene Picker Enabled";
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

		// ============================================================================
		// Designer-facing menus — Domain / Platform only (no dual legacy registration).
		// Domain: reach a tool by *what it does*. Platform: project setup / package integration.
		// ============================================================================

		/// <summary>Tools/Domain/<System>/* — designer authoring, generation, and validation actions.</summary>
		public static class Domain
		{
			private const string Root = Tools + "Domain/";

			/// <summary>Tools/Domain/GAS/* — GAS authoring and codegen.</summary>
			public const string GasNormalizeAttributeSetTags    = Root + "GAS/Normalize AttributeSet Tags";
			public const string GasRebuildAbilityLogic          = Root + "GAS/Rebuild Ability Logic";
			public const string GasRebuildTagReferenceIndex     = Root + "GAS/Rebuild Tag Reference Index";
			public const string GasSanitizeTagHashes            = Root + "GAS/Sanitize Tag Hashes";
			public const string GasMigrateEffectIdentityTags    = Root + "GAS/Migrate Effect Identity Tags";
			public const string GasInstallGizmoIcons            = Root + "GAS/Install Gizmo Icons";
			public const string GasCreateNewAbility             = Root + "GAS/Create New Ability...";
			public const string GasFindDuplicateEffectIdentityTags = Root + "GAS/Find Duplicate Effect Identity Tags";

			/// <summary>Tools/Domain/AI/* — AI authoring and generators.</summary>
			public const string AiGenerateCommanderBrain        = Root + "AI/Generate Default Commander Brain";
			public const string AiGenerateDecisionSet           = Root + "AI/Generate Default Decision Set";
			public const string AiGenerateBuiltInBehaviors      = Root + "AI/Generate Built-in Behavior Assets";
			public const string AiGenerateBlackboardRegistry    = Root + "AI/Generate Blackboard Registry";
			public const string AiSetupPawnOnSelection          = Root + "AI/Setup AI Pawn on Selection";
			public const string AiSetupPawnContext              = GameObject + "Domain/AI/Setup AI Pawn";

			/// <summary>Tools/Domain/Character/* — character setup.</summary>
			public const string CharacterCreateDefaultStateProfile = Root + "Character/Create Default Character State Profile";
			public const string CharacterReconcileSubsystemHub     = Root + "Character/Reconcile Subsystem Hub on Selection";
			public const string CharacterReconcileHubContext       = GameObject + "Domain/Character/Reconcile Subsystem Hub";
			public const string CharacterCreateFromArchetype       = GameObject + "Domain/Character/Character From Archetype...";

			/// <summary>Tools/Domain/Economy/* — currency registry tooling.</summary>
			public const string EconomyRefreshCurrencyRegistries = Root + "Economy/Refresh Currency Registries";

			/// <summary>Tools/Domain/Item/Create/* — item asset scaffolding.</summary>
			public const string ItemCreateEquippable              = Root + "Item/Create/Equippable Item";
			public const string ItemCreateConsumable              = Root + "Item/Create/Consumable Item";
			public const string ItemCreateCraftingRecipe          = Root + "Item/Create/Crafting Recipe";
			public const string ItemCreateItemDefRegistry         = Root + "Item/Create/Item Definition Registry";
			public const string ItemCreateItemContainerDefRegistry = Root + "Item/Create/Item Container Definition Registry";
			public const string ItemCreateCraftingRecipeRegistry   = Root + "Item/Create/Crafting Recipe Registry";
			public const string ItemCreateInventoryBagContainer    = Root + "Item/Create/Inventory Bag Container";
			public const string ItemCreateEquipmentSlotSetProfile  = Root + "Item/Create/Equipment Slot Set + Profile + Container";

			/// <summary>Tools/Domain/Player/* and GameObject/Domain/Player/* — player &amp; pawn authoring.</summary>
			public const string PlayerCreateInputActions        = Root + "Player/Create Player Input Actions";
			public const string PlayerFillRosterPrefabs         = Root + "Player/Fill Roster Prefabs From Prefab Map";
			public const string PlayerCreateSimplePawn          = GameObject + "Domain/Player/Simple Pawn";
			public const string PlayerCreatePlayerStart         = GameObject + "Domain/Player/Player Start";

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

			/// <summary>Tools/Domain/Validation/* — scene/entity validation.</summary>
			public const string ValidationPlayableScene           = Root + "Validation/Validate Playable Scene";
			public const string ValidationDomainEntities          = Root + "Validation/Validate Domain Entities";
			public const string ValidationDeterministicRandom     = Root + "Validation/Find Non-Deterministic Random Usage";

			/// <summary>Tools/Domain/Network/* — network layer setup.</summary>
			public const string NetworkCreateConfig               = Root + "Network/Create Network Config";
			public const string NetworkValidateSetup               = Root + "Network/Validate Setup";

			/// <summary>Tools/Domain/* — one-click scaffolding.</summary>
			public const string CreateNewDomain                   = Root + "Create New Domain...";
			public const string CreateNewScene                    = Root + "Create New Scene...";
			public const string CreateDomainEvent                = Root + "Create Domain Event...";
		}

		/// <summary>Window/Domain/<System>/* — designer-facing debugger and preview windows.</summary>
		public static class DomainWindow
		{
			private const string Root = Window + "Domain/";
			public const string GasDebugger        = Root + "GAS/GAS Debugger...";
			public const string GasTagManager      = Root + "GAS/Gameplay Tag Manager...";
			public const string AiAuthor           = Root + "AI/AI Author...";
			public const string AiDebugger         = Root + "AI/AI Debugger...";
			public const string CharacterDebugger  = Root + "Character/Character Debugger...";
			public const string CharacterLocomotion = Root + "Character/Locomotion Blend Debug...";
			public const string CharacterRagdoll   = Root + "Character/Ragdoll Helper...";
			public const string CombatDebugger     = Root + "Combat/Combat Debugger...";
			public const string WeaponWizard       = Root + "Combat/Weapon Wizard...";
			public const string CombatPreview      = Root + "Combat/Combat Preview...";
			public const string ItemRig            = Root + "Item/Equipment Rig Setup...";
			public const string ItemIkPreview      = Root + "Item/Equipment IK Preview...";
			public const string ItemDebugger       = Root + "Item/Item Debugger...";
			public const string ItemEquipKit       = Root + "Item/Character Equipment Kit...";
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
			public const string InstallGizmoIcons   = Root + "Install Gizmo Icons";
			public const string RegistryRefresh     = Root + "Rebuild All Generated Registries";
			public const string PackageRebuild      = Root + "Rebuild Package Integrations";
			public const string NetworkCreateConfig = Root + "Network/Create Network Config";
			public const string NetworkValidateSetup = Root + "Network/Validate Setup";
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
