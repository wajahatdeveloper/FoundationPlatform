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
        // ---- Tools menu bands ----
        public const int Rebuild    = 1000; // Tools/Rebuild/*
        public const int ItemCreate = 1010; // Tools/Item/Create/*
        public const int Gas        = 1100; // Tools/GAS/*
        public const int Character  = 1120; // Tools/Character/*
        public const int Ai         = 1140; // Tools/AI/*
        public const int Linting    = 1200; // Tools/GameEngineCore/Linting/*
        public const int Packages   = 1250; // Tools/Packages/*
        public const int Debug      = 1300; // Tools/Debug/*
        public const int Input      = 1350; // Tools/GameEngineCore/Input/*
        public const int Utilities  = 1400; // Tools/Utilities/*
        public const int Diagnostics = 1450; // Tools/Diagnostics/* (former Tools/HOMAM)
        public const int GameEngineCore = 1500; // Tools/GameEngineCore/* (misc)

        // ---- Window menu bands ----
        public const int WindowHub     = 500;  // Window/Core/Central Authoring (primary hub)
        public const int WindowCore    = 1100; // Window/Core/* diagnostic + creator windows
        public const int WindowCreators = 1500; // Window/Core/Create * scaffolds
        public const int WindowDomain  = 1000; // Window/<Domain>/* (Combat, Character, Item, GAS...)

        // ---- Context / GameObject menu bands ----
        public const int ContextComponent = 50;  // CONTEXT/Component/*, CONTEXT/MonoBehaviour/*
        public const int GameObjectSetup  = 10;  // GameObject/GameEngineCore/*
    }
}
#endif
