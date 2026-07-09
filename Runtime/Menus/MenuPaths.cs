#if UNITY_EDITOR
namespace FoundationPlatform.Utilities.Menus
{
    /// <summary>
    /// Single source of truth for editor <c>[MenuItem]</c> paths. Replaces ~80 files' worth of
    /// hardcoded inline path strings. Grouped by menu area; nested classes mirror the on-screen
    /// submenu hierarchy so the taxonomy is readable at a glance and reorganizing a group is a
    /// one-line edit here rather than a hunt across frameworks.
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

        /// <summary>Tools/GAS/* — Gameplay Ability System authoring &amp; codegen.</summary>
        public static class Gas
        {
            private const string Root = Tools + "GAS/";
            public const string BootstrapConfig      = Root + "GAS Bootstrap Config";
            public const string NormalizeAttributeSetTags = Root + "Normalize AttributeSet Tags";
            public const string RebuildAbilityLogic  = Root + "Rebuild Ability Logic";
            public const string RebuildTagReferenceIndex = Root + "Rebuild Tag Reference Index";
            public const string SanitizeTagHashes    = Root + "Sanitize Tag Hashes";

            private const string WindowRoot = Window + "GAS/";
            public const string Debugger   = WindowRoot + "GAS Debugger...";
            public const string TagManager = WindowRoot + "Gameplay Tag Manager...";
        }

        /// <summary>Tools/AI/* and Window/AI/* — AI authoring, generators, debuggers.</summary>
        public static class Ai
        {
            private const string Root = Tools + "AI/";
            public const string GenerateCommanderBrain    = Root + "Generate Default Commander Brain";
            public const string GenerateDecisionSet       = Root + "Generate Default Decision Set";
            public const string GenerateBuiltInBehaviors  = Root + "Generate Built-in Behavior Assets";
            public const string GenerateBlackboardDatabase = Root + "Generate Blackboard Database";
            public const string SetupPawnOnSelection      = Root + "Setup AI Pawn on Selection";

            private const string WindowRoot = Window + "AI/";
            public const string Author   = WindowRoot + "AI Author...";
            public const string Debugger = WindowRoot + "AI Debugger...";

            public const string SetupPawnContext = GameObject + "GameEngineCore/AI/Setup AI Pawn";
        }

        /// <summary>Tools/Character/* and Window/Character/* — character setup &amp; debuggers.</summary>
        public static class Character
        {
            private const string Root = Tools + "Character/";
            public const string CreateDefaultStateProfile = Root + "Create Default Character State Profile";
            public const string ReconcileSubsystemHub     = Root + "Reconcile Subsystem Hub on Selection";

            private const string WindowRoot = Window + "Character/";
            public const string Debugger        = WindowRoot + "Character Debugger...";
            public const string LocomotionBlend = WindowRoot + "Locomotion Blend Debug...";
            public const string RagdollHelper   = WindowRoot + "Ragdoll Helper...";

            public const string ReconcileHubContext = GameObject + "GameEngineCore/Character/Reconcile Subsystem Hub";
        }

        /// <summary>Tools/GameEngineCore/Linting/* — validation &amp; rollout toggles.</summary>
        public static class Linting
        {
            private const string Root = Tools + "GameEngineCore/Linting/";
            public const string RunFullScan          = Root + "Run Full Scan";
            public const string PrintActiveConfigPath = Root + "Print Active Config Path";
            public const string RolloutWarningFirst  = Root + "Rollout Mode: Warning First";
            public const string RolloutStrict        = Root + "Rollout Mode: Strict";
            public const string ValidatePlayableScene = Root + "Validate Playable Scene";
            public const string ValidateDomainEntities = Root + "Validate Domain Entities";
        }

        /// <summary>Tools/GameEngineCore/Input/* — input provision authoring.</summary>
        public static class Input
        {
            private const string Root = Tools + "GameEngineCore/Input/";
            public const string CreateCharacterProvision = Root + "Create Character Input Provision";
            public const string CreateCombatProvision    = Root + "Create Combat Input Provision";
            public const string CreatePartyProvision     = Root + "Create Party Input Provision";
            public const string Integration              = Root + "Input Integration";
        }

        /// <summary>Window/UIWidgets/* — shared widget browser.</summary>
        public static class UIWidgets
        {
            private const string WindowRoot = Window + "UIWidgets/";
            public const string WidgetsWindow = WindowRoot + "UI Widgets...";
            public const string GameObjectOpen = GameObject + "UIWidgets/Open UI Widgets Window...";
        }

        /// <summary>Tools/GameEngineCore/* — misc GameEngineCore tools.</summary>
        public static class GameEngineCore
        {
            private const string Root = Tools + "GameEngineCore/";
            public const string GameActionMatrix = Root + "Game Action Matrix";
            public const string RebuildGeneratedCatalogs = Root + "Rebuild/Rebuild All Generated Catalogs";
            public const string RefreshCurrencyDatabases = Root + "Refresh/Currency Databases";
            public const string RuleExplorer = Root + "GameAction/Rule Explorer";
        }

        /// <summary>Tools/Packages/* — package integration tooling (was Tools/Core).</summary>
        public static class Packages
        {
            private const string Root = Tools + "Packages/";
            public const string RebuildIntegrations = Root + "Rebuild Package Integrations";
            public const string AuditManifestCoverage = Root + "Audit Package Manifest Coverage";
            public const string PopulateManifestDefaults = Root + "Populate Package Manifest Defaults";
        }

        /// <summary>Tools/Debug/* — debug filesystem/trace toggles &amp; monitors (was split with Tools/Core).</summary>
        public static class Debug
        {
            private const string Root = Tools + "Debug/";
            public const string OpenLogsFolder    = Root + "Open Logs Folder";
            public const string OpenPersistentData = Root + "Open Persistent Data Folder";
            public const string CaptureFullStackTraces = Root + "Capture Full Stack Traces";
            public const string SyncConsole        = Root + "Sync Console";
            public const string MonitorEventBus    = Root + "Monitor Event Bus (toggle)";
        }

        /// <summary>Tools/Utilities/* — general-purpose editor utilities (was Tools/Core).</summary>
        public static class Utilities
        {
            private const string Root = Tools + "Utilities/";
            public const string TakeScreenshot       = Root + "Take Screenshot";
            public const string DownloadSound        = Root + "Download Sound from Story Block";
            public const string ImageToStringConverter = Root + "Image To String Converter";
            public const string BakePrefabLightmaps  = Root + "Bake Prefab Lightmaps";
        }

        /// <summary>Tools/Diagnostics/* — dev/demo diagnostic tools (was Tools/HOMAM).</summary>
        public static class Diagnostics
        {
            private const string Root = Tools + "Diagnostics/";
            public const string AnimationTestBench   = Root + "Animation Test Bench";
            public const string FrameworkInspectorDemo = Root + "Framework Inspector Demo";
        }

        /// <summary>Window/Core/* — engine hub, diagnostic windows, and scaffold creators.</summary>
        public static class WindowCore
        {
            private const string Root = Window + "Core/";
            public const string CentralAuthoring   = Root + "Central Authoring...";
            public const string AutoBinder         = Root + "Auto Binder...";
            public const string AsyncFlowVisualizer = Root + "Async Flow Visualizer...";
            public const string Telemetry          = Root + "Telemetry...";
            public const string SessionStateAudit  = Root + "Session State Contributor Audit...";
            public const string CreateEventChannel = Root + "Create Event Channel...";
            public const string CreateNewDomain    = Root + "Create New Domain...";
            public const string CreateNewScene     = Root + "Create New Scene...";
            public const string CreateDomainEvent  = Root + "Create Domain Event...";
        }

        /// <summary>Window/Utilities/* — utility windows.</summary>
        public static class WindowUtilities
        {
            private const string Root = Window + "Utilities/";
            public const string SceneSwitcher    = Root + "Scene Switcher...";
            public const string ScriptGenerator  = Root + "Script Generator...";
            public const string PresetAutomation = Root + "Preset Automation Settings...";
        }

        /// <summary>Window/&lt;Domain&gt;/* — per-domain debugger/authoring windows.</summary>
        public static class WindowDomain
        {
            public const string CombatDebugger   = Window + "Combat/Combat Debugger...";
            public const string WeaponWizard     = Window + "Combat/Weapon Wizard...";
            public const string CombatPreview    = Window + "Combat/Combat Preview...";
            public const string ItemEquipmentRig = Window + "Item/Equipment Rig Setup...";
            public const string ItemIkPreview    = Window + "Item/Equipment IK Preview...";
            public const string DebugXConsole    = Window + "Debug/DebugX Console...";
            public const string EventBus         = Window + "EventBus/Event Bus...";
            public const string TweenDebugger    = Window + "TweenX/Tween Debugger...";
        }
    }
}
#endif
