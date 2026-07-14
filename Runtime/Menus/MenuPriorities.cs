#if UNITY_EDITOR
namespace FoundationPlatform.Utilities.Menus
{
    /// <summary>
    /// Centralized <c>[MenuItem]</c> priority bands. Replaces the ad-hoc magic numbers that were
    /// scattered inline across ~80 editor files. Each band leaves headroom (items add small
    /// offsets like <c>Gas + 1</c>) and a gap of ~100 between bands so whole groups can be
    /// reordered without renumbering neighbours. Unity inserts a menu separator when adjacent
    /// item priorities differ by 11+, so keep intra-group offsets small (0..9).
    /// <para>These are <c>const int</c> so they satisfy the <c>[MenuItem]</c> attribute's
    /// compile-time-constant requirement.</para>
    /// </summary>
    public static class MenuPriorities
    {
        // ---- Tools/GameEngineCore/<Module>/* bands ----
        public const int Gas         = 1100; // Tools/GameEngineCore/GAS/*
        public const int Character   = 1120; // Tools/GameEngineCore/Character/*
        public const int Ai          = 1140; // Tools/GameEngineCore/AI/*
        public const int ItemCreate  = 1160; // Tools/GameEngineCore/Item/Create/*
        public const int Economy     = 1180; // Tools/GameEngineCore/Economy/*
        public const int PresetLibrary = 1190; // Tools/GameEngineCore/PresetLibrary/*
        public const int Network     = 1195; // Tools/GameEngineCore/Network/*
        public const int Validation  = 1200; // Tools/GameEngineCore/Validation/* (GEC-owned half of the former shared Linting)
        public const int Packages    = 1250; // Tools/GameEngineCore/Packages/*
        public const int Input       = 1350; // Tools/GameEngineCore/Input/*
        public const int GameEngineCore = 1500; // Tools/GameEngineCore/* (misc)

        // ---- Tools/<Category>/* bands (FoundationPlatform, unwrapped) ----
        public const int Rebuild    = 1000; // Tools/Rebuild/*
        public const int Linting    = 1200; // Tools/Linting/* (FoundationPlatform-owned half of the former shared Linting)
        public const int Debug      = 1300; // Tools/Debug/*
        public const int Utilities  = 1400; // Tools/Utilities/*
        public const int Diagnostics = 1450; // Tools/Diagnostics/*

        // ---- Window/GameEngineCore/<Module>/* and Window/GameEngineCore/* bands ----
        public const int WindowHub  = 500;  // Window/GameEngineCore/Central Authoring... (primary hub)
        public const int WindowCore = 1100; // Window/GameEngineCore/* diagnostic + creator windows
        public const int WindowCreators = 1500; // Window/GameEngineCore/Create * scaffolds
        public const int WindowGas  = 1120; // Window/GameEngineCore/GAS/*

        // ---- Window/<Category>/* bands (FoundationPlatform, unwrapped) ----
        public const int WindowUtilities = 1100; // Window/Utilities/*
        public const int WindowDebugX    = 1150; // Window/DebugX Console... (bare leaf, single window)
        public const int WindowEventBus  = 1160; // Window/Event Bus... (bare leaf, single window)
        public const int WindowTweenX    = 1170; // Window/Tween Debugger... (bare leaf, single window)

        // ---- Context / GameObject menu bands ----
        public const int ContextComponent = 50;  // CONTEXT/Component/*, CONTEXT/MonoBehaviour/*
        public const int GameObjectSetup  = 10;  // GameObject/GameEngineCore/*
    }
}
#endif
