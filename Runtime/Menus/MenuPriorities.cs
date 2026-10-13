#if UNITY_EDITOR
namespace AetherNexus.FoundationPlatform.Utilities.Menus
{
    /// <summary>
    /// Centralized <c>[MenuItem]</c> priority bands, one per <see cref="MenuPaths"/> group. Each band
    /// leaves headroom (items add small offsets like <c>DomainGas + 1</c>) and a gap between bands so
    /// whole groups can be reordered without renumbering neighbours. Unity inserts a menu separator
    /// when adjacent item priorities differ by 11+, so keep intra-group offsets small (0..9).
    /// <para>These are <c>const int</c> so they satisfy the <c>[MenuItem]</c> attribute's
    /// compile-time-constant requirement.</para>
    /// </summary>
    public static class MenuPriorities
    {
        // ---- Tools/<Category>/* ----
        public const int Rebuild   = 1000; // Tools/Rebuild/*
        public const int Linting   = 1200; // Tools/Linting/*
        public const int Utilities = 1400; // Tools/Utilities/*
        public const int Platform  = 1500; // Tools/Platform/*

        // ---- Tools/Domain/* ----
        public const int DomainGas        = 1000; // Tools/Domain/Create New Ability...
        public const int DomainCharacter  = 1020; // Tools/Domain/Create Default Character State Profile
        public const int DomainValidation = 1100; // Tools/Domain/Validate Project
        public const int DomainInput      = 1160; // Tools/Domain/Input/*
        public const int DomainPlayer     = 1170; // Tools/Domain/Player/*
        public const int DomainCoreCreate = 1180; // Tools/Domain/Create New Scene...

        // ---- Window/* ----
        public const int WindowDomainFinder = 1080; // Window/Domain/Find Feature... (sits above the debuggers)
        public const int WindowDomainCore   = 1100; // Window/Domain/*
        public const int WindowDomainGas    = 1120; // Window/Domain/GAS/*
        public const int WindowUtilities    = 1100; // Window/Utilities/*
        public const int WindowUIWidgets    = 1180; // Window/UI Widgets...
        public const int WindowEngine       = 1200; // Window/Engine/* runtime debuggers
        public const int WindowEngineTools  = 1220; // Window/Engine/* console, bus, tweens
        public const int WindowEnginePanels = 1240; // Window/Engine/* editor-extension panels
        public const int WindowEngineDemo   = 1260; // Window/Engine/* test bench, demos

        // ---- GameObject/* ----
        public const int GameObjectEdit   = 0;  // GameObject/Header, Group Selection, Ungroup, Drop To Floor
        public const int GameObjectSetup  = 10; // GameObject/Domain/*
        public const int GameObjectCanvas = 2100; // GameObject/UI (Canvas)/* widget creates (offsets 0..49)
        public const int GameObjectCanvasExtras = 2160; // GameObject/UI (Canvas)/* tutorial spotlight, Text upgrade

        // ---- CONTEXT/* ----
        public const int ContextComponent = 500; // CONTEXT/Component/*
        public const int ContextValues    = 520; // CONTEXT/Component/Values/*
        public const int ContextScript    = 600; // CONTEXT/MonoBehaviour/*
        public const int ContextCharacter = 150; // CONTEXT/<character component>/* (rig actions)
        public const int ContextWidget    = 900; // CONTEXT/RectTransform, CONTEXT/Button

        // ---- Assets/* ----
        public const int AssetsImport   = 10;   // Assets/Import Package/Here...
        public const int AssetsContent  = 21;   // Assets/Create/Content Area
        public const int AssetsActions  = 2000; // Assets/Create Domain Event, Fix Out of Sync, Create Level For This Prefab
        public const int DomainItemCreate = 1060; // Assets/Create/Item/*
    }
}
#endif
