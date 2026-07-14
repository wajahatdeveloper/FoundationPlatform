#if UNITY_EDITOR
namespace FoundationPlatform.Utilities.Menus
{
    /// <summary>
    /// Single source of truth for editor <c>[MenuItem]</c> paths. Grouped by menu area; nested
    /// classes mirror the on-screen submenu hierarchy so the taxonomy is readable at a glance and
    /// reorganizing a group is a one-line edit here rather than a hunt across frameworks.
    /// <para>GameEngineCore and every framework that depends on it (GAS, AI, Character, Combat,
    /// Item, Quest, Shop, Economy, PresetLibrary, Network, GameFeatures...) nest under a single
    /// <c>Tools/GameEngineCore/&lt;Module&gt;</c> / <c>Window/GameEngineCore/&lt;Module&gt;</c>
    /// umbrella — an Invector/Game Creator style ecosystem where every current and future
    /// extension package reads as part of one product instead of adding its own top-level menu
    /// folder. FoundationPlatform is a separate free, generic Unity-enhancement package and is
    /// intentionally NOT wrapped in a branded folder — its categories (Debug, Utilities,
    /// Diagnostics, Rebuild, Linting...) sit directly under <c>Tools/</c>/<c>Window/</c> as if
    /// native to the engine. UIWidgets is its own free package with its own root.</para>
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

        // Shared umbrella for GameEngineCore + every dependent extension framework.
        private const string Gec = Tools + "GameEngineCore/";
        private const string GecWindow = Window + "GameEngineCore/";

        // ============================================================================
        // GameEngineCore + extension frameworks — Tools/GameEngineCore/<Module>/*, Window/GameEngineCore/<Module>/*
        // ============================================================================

        /// <summary>Tools/GameEngineCore/GAS/* and Window/GameEngineCore/GAS/* — Gameplay Ability System authoring &amp; codegen.</summary>
        public static class Gas
        {
            private const string Root = Gec + "GAS/";
            public const string BootstrapConfig      = Root + "GAS Bootstrap Config";
            public const string NormalizeAttributeSetTags = Root + "Normalize AttributeSet Tags";
            public const string RebuildAbilityLogic  = Root + "Rebuild Ability Logic";
            public const string RebuildTagReferenceIndex = Root + "Rebuild Tag Reference Index";
            public const string SanitizeTagHashes    = Root + "Sanitize Tag Hashes";
            public const string MigrateEffectIdentityTags = Root + "Migrate Effect Identity Tags";

            private const string WindowRoot = GecWindow + "GAS/";
            public const string Debugger   = WindowRoot + "GAS Debugger...";
            public const string TagManager = WindowRoot + "Gameplay Tag Manager...";
        }

        /// <summary>Tools/GameEngineCore/AI/* and Window/GameEngineCore/AI/* — AI authoring, generators, debuggers.</summary>
        public static class Ai
        {
            private const string Root = Gec + "AI/";
            public const string GenerateCommanderBrain    = Root + "Generate Default Commander Brain";
            public const string GenerateDecisionSet       = Root + "Generate Default Decision Set";
            public const string GenerateBuiltInBehaviors  = Root + "Generate Built-in Behavior Assets";
            public const string GenerateBlackboardRegistry = Root + "Generate Blackboard Registry";
            public const string SetupPawnOnSelection      = Root + "Setup AI Pawn on Selection";

            private const string WindowRoot = GecWindow + "AI/";
            public const string Author   = WindowRoot + "AI Author...";
            public const string Debugger = WindowRoot + "AI Debugger...";

            public const string SetupPawnContext = GameObject + "GameEngineCore/AI/Setup AI Pawn";
        }

        /// <summary>Tools/GameEngineCore/Character/* and Window/GameEngineCore/Character/* — character setup &amp; debuggers.</summary>
        public static class Character
        {
            private const string Root = Gec + "Character/";
            public const string CreateDefaultStateProfile = Root + "Create Default Character State Profile";
            public const string ReconcileSubsystemHub     = Root + "Reconcile Subsystem Hub on Selection";

            private const string WindowRoot = GecWindow + "Character/";
            public const string Debugger        = WindowRoot + "Character Debugger...";
            public const string LocomotionBlend = WindowRoot + "Locomotion Blend Debug...";
            public const string RagdollHelper   = WindowRoot + "Ragdoll Helper...";

            public const string ReconcileHubContext = GameObject + "GameEngineCore/Character/Reconcile Subsystem Hub";
        }

        /// <summary>Window/GameEngineCore/Combat/* — combat authoring &amp; debug windows.</summary>
        public static class Combat
        {
            private const string WindowRoot = GecWindow + "Combat/";
            public const string CombatDebugger = WindowRoot + "Combat Debugger...";
            public const string WeaponWizard   = WindowRoot + "Weapon Wizard...";
            public const string CombatPreview  = WindowRoot + "Combat Preview...";
        }

        /// <summary>Tools/GameEngineCore/Item/* and Window/GameEngineCore/Item/* — item authoring, creation &amp; debug windows.</summary>
        public static class Item
        {
            private const string CreateRoot = Gec + "Item/Create/";
            public const string CreateEquippable   = CreateRoot + "Equippable Item";
            public const string CreateConsumable   = CreateRoot + "Consumable Item";
            public const string CreateCraftingRecipe = CreateRoot + "Crafting Recipe";
            public const string CreateItemDefinitionRegistry = CreateRoot + "Item Definition Registry";
            public const string CreateItemContainerDefinitionRegistry = CreateRoot + "Item Container Definition Registry";
            public const string CreateCraftingRecipeRegistry = CreateRoot + "Crafting Recipe Registry";
            public const string CreateInventoryBagContainer = CreateRoot + "Inventory Bag Container";
            public const string CreateEquipmentSlotSetProfileContainer = CreateRoot + "Equipment Slot Set + Profile + Container";

            private const string WindowRoot = GecWindow + "Item/";
            public const string EquipmentRig = WindowRoot + "Equipment Rig Setup...";
            public const string IkPreview    = WindowRoot + "Equipment IK Preview...";
            public const string Debugger     = WindowRoot + "Item Debugger...";
            public const string EquipmentKit = WindowRoot + "Character Equipment Kit...";
        }

        /// <summary>Window/GameEngineCore/Quest/* — quest debug window.</summary>
        public static class Quest
        {
            private const string WindowRoot = GecWindow + "Quest/";
            public const string Debugger = WindowRoot + "Quest Debugger...";
        }

        /// <summary>Window/GameEngineCore/Shop/* — shop &amp; economy debug window.</summary>
        public static class Shop
        {
            private const string WindowRoot = GecWindow + "Shop/";
            public const string Debugger = WindowRoot + "Shop & Economy Debugger...";
        }

        /// <summary>Tools/GameEngineCore/Economy/* — currency registry tooling.</summary>
        public static class Economy
        {
            private const string Root = Gec + "Economy/";
            public const string RefreshCurrencyRegistries = Root + "Refresh Currency Registries";
        }

        /// <summary>Tools/GameEngineCore/Input/* — input provision authoring.</summary>
        public static class Input
        {
            private const string Root = Gec + "Input/";
            public const string CreateCharacterProvision = Root + "Create Character Input Provision";
            public const string CreateCombatProvision    = Root + "Create Combat Input Provision";
            public const string CreatePartyProvision     = Root + "Create Party Input Provision";
            public const string Integration              = Root + "Input Integration";
        }

        /// <summary>Tools/GameEngineCore/PresetLibrary/* — preset asset generation.</summary>
        public static class PresetLibrary
        {
            private const string Root = Gec + "PresetLibrary/";
            public const string GenerateAll          = Root + "Generate All";
            public const string RegisterTagsOnly     = Root + "Register Tags Only";
            public const string GenerateGas          = Root + "Generate GAS (Attributes + Effects + Cues)";
            public const string GenerateCharactersItems = Root + "Generate Characters + Items";
            public const string GenerateArchetypesAi = Root + "Generate Archetypes + AI";
        }

        /// <summary>Tools/GameEngineCore/Network/* — network layer setup.</summary>
        public static class Network
        {
            private const string Root = Gec + "Network/";
            public const string CreateConfig  = Root + "Create Network Config";
            public const string ValidateSetup = Root + "Validate Setup";
        }

        /// <summary>Tools/GameEngineCore/Validation/* — scene/entity validation (GameEngineCore-owned half of the former shared Linting class).</summary>
        public static class Validation
        {
            private const string Root = Gec + "Validation/";
            public const string ValidatePlayableScene  = Root + "Validate Playable Scene";
            public const string ValidateDomainEntities = Root + "Validate Domain Entities";
        }

        /// <summary>Tools/GameEngineCore/Packages/* — package integration tooling.</summary>
        public static class Packages
        {
            private const string Root = Gec + "Packages/";
            public const string RebuildIntegrations = Root + "Rebuild Package Integrations";
            public const string AuditManifestCoverage = Root + "Audit Package Manifest Coverage";
            public const string PopulateManifestDefaults = Root + "Populate Package Manifest Defaults";
        }

        /// <summary>Tools/GameEngineCore/* — misc GameEngineCore tools with no dedicated module bucket.</summary>
        public static class GameEngineCore
        {
            private const string Root = Gec;
            public const string RebuildGeneratedRegistries = Root + "Rebuild/Rebuild All Generated Registries";
        }

        /// <summary>Window/GameEngineCore/* — engine hub, diagnostic windows, and scaffold creators.</summary>
        public static class WindowCore
        {
            private const string Root = GecWindow;
            public const string CentralAuthoring    = Root + "Central Authoring...";
            public const string GameActionMatrix    = Root + "Game Action Matrix...";
            public const string AsyncFlowVisualizer = Root + "Async Flow Visualizer...";
            public const string Telemetry           = Root + "Telemetry...";
            public const string SessionStateAudit   = Root + "Session State Contributor Audit...";
            public const string CreateNewDomain     = Root + "Create New Domain...";
            public const string CreateNewScene      = Root + "Create New Scene...";
            public const string CreateDomainEvent   = Root + "Create Domain Event...";
        }

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
            public const string DownloadSound        = Root + "Download Sound from Story Block";
            public const string ImageToStringConverter = Root + "Image To String Converter";
            public const string BakePrefabLightmaps  = Root + "Bake Prefab Lightmaps";
        }

        /// <summary>Tools/Diagnostics/* — dev/demo diagnostic tools.</summary>
        public static class Diagnostics
        {
            private const string Root = Tools + "Diagnostics/";
            public const string AnimationTestBench   = Root + "Animation Test Bench";
            public const string FrameworkInspectorDemo = Root + "Framework Inspector Demo";
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

        /// <summary>CONTEXT/Component/* — generic component context-menu utilities (FrameworkInspector).</summary>
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
            public const string ForceRebuildInspectorCache = Root + "Force Rebuild Framework Inspector Cache";
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
        // UIWidgets — own root (free package, positioned to feel native to Unity's UI tooling)
        // ============================================================================

        /// <summary>Window/UIWidgets/* and GameObject/UIWidgets/* — shared widget browser.</summary>
        public static class UIWidgets
        {
            private const string WindowRoot = Window + "UIWidgets/";
            public const string WidgetsWindow = WindowRoot + "UI Widgets...";
            public const string GameObjectOpen = GameObject + "UIWidgets/Open UI Widgets Window...";
        }

        /// <summary>Tools/UIWidgets/* — items with no natural home in a native Unity menu.</summary>
        public static class UIWidgetsTools
        {
            private const string Root = Tools + "UIWidgets/";
            public const string ScenePickerEnabled = Root + "Scene Picker Enabled";
            public const string Settings           = Root + "Settings...";
        }

        /// <summary>CONTEXT/RectTransform/* — Fit Anchors integrates into Unity's own RectTransform context menu
        /// instead of adding a UIWidgets-branded Tools entry.</summary>
        public static class UIWidgetsContext
        {
            public const string FitAnchors = Context + "RectTransform/Fit Anchors &o";
        }
    }
}
#endif
