#if UNITY_EDITOR
namespace AetherNexus.FoundationPlatform.Utilities.Menus
{
    /// <summary>
    /// Centralized <c>[MenuItem]</c> priority bands. Replaces the ad-hoc magic numbers that were
    /// scattered inline across ~80 editor files. Each band leaves headroom (items add small
    /// offsets like <c>DomainGas + 1</c>) and a gap of ~100 between bands so whole groups can be
    /// reordered without renumbering neighbours. Unity inserts a menu separator when adjacent
    /// item priorities differ by 11+, so keep intra-group offsets small (0..9).
    /// <para>These are <c>const int</c> so they satisfy the <c>[MenuItem]</c> attribute's
    /// compile-time-constant requirement.</para>
    /// </summary>
    public static class MenuPriorities
    {
        // ---- Tools/<Category>/* bands (FoundationPlatform, unwrapped) ----
        public const int Rebuild    = 1000; // Tools/Rebuild/*
        public const int Linting    = 1200; // Tools/Linting/*
        public const int Debug      = 1300; // Tools/Debug/*
        public const int Utilities  = 1400; // Tools/Utilities/*
        public const int Diagnostics = 1450; // Tools/Diagnostics/*

        // ---- Window/<Category>/* bands (FoundationPlatform, unwrapped) ----
        public const int WindowUtilities = 1100; // Window/Utilities/*
        public const int WindowDebugX    = 1150; // Window/DebugX Console...
        public const int WindowEventBus  = 1160; // Window/Event Bus...
        public const int WindowTweenX    = 1170; // Window/Tween Debugger...

        // ---- Context / GameObject menu bands ----
        public const int ContextComponent = 50;  // CONTEXT/Component/*, CONTEXT/MonoBehaviour/*
        public const int GameObjectSetup  = 10;  // GameObject/Domain/*

        // ---- Tools/Domain/<Name>/* — designer-facing authoring bands ----
        public const int DomainGas         = 1000; // Tools/Domain/GAS/*
        public const int DomainCharacter   = 1020; // Tools/Domain/Character/*
        public const int DomainAi          = 1040; // Tools/Domain/AI/*
        public const int DomainItemCreate  = 1060; // Tools/Domain/Item/Create/*
        public const int DomainEcon        = 1080; // Tools/Domain/Economy/*
        public const int DomainPresetLib   = 1090; // Tools/Domain/PresetLibrary/*
        public const int DomainNetwork     = 1095; // Tools/Domain/Network/*
        public const int DomainValidation  = 1100; // Tools/Domain/Validation/*
        public const int DomainPackages    = 1140; // Tools/Domain/Packages/*
        public const int DomainInput       = 1160; // Tools/Domain/Input/*
        public const int DomainCoreCreate  = 1180; // Tools/Domain/Create * scaffolds
        public const int DomainGec         = 1200; // Tools/Domain/* (fallback misc)

        // ---- Tools/Platform/* — infrastructure / admin ----
        public const int Platform           = 1500; // Tools/Platform/*

        // ---- Window/Domain/* and Window/Platform/* ----
        public const int WindowDomainGas   = 1120; // Window/Domain/GAS/*
        public const int WindowDomainCore  = 1100; // Window/Domain/* (debuggers, central window)
        public const int WindowPlatform    = 1600; // Window/Platform/*
    }
}
#endif
