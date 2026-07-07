namespace FoundationPlatform.DebugX
{
    /// <summary>
    /// Compile-time constants for log channels/filters
    /// Provides IntelliSense and prevents typos
    /// </summary>
    public static class LogChannels
    {
        public static readonly LogChannel Default = new LogChannel("Default");
        public static readonly LogChannel Engine = new LogChannel("Engine");
        public static readonly LogChannel Simulation = new LogChannel("Simulation");
        public static readonly LogChannel DevTools = new LogChannel("DevTools");
        public static readonly LogChannel Framework = new LogChannel("Framework");
        public static readonly LogChannel Validation = new LogChannel("Validation");
        public static readonly LogChannel SceneTransition = new LogChannel("SceneTransition");
        // ReSharper disable once InconsistentNaming
        public static readonly LogChannel GAS = new LogChannel("GAS");
        public static readonly LogChannel Locomotion = new LogChannel("Locomotion");
        public static readonly LogChannel Inventory = new LogChannel("Inventory");
        public static readonly LogChannel Economy = new LogChannel("Economy");
        public static readonly LogChannel Quest = new LogChannel("Quest");
        public static readonly LogChannel Combat = new LogChannel("Combat");
        public static readonly LogChannel GameAction = new LogChannel("GameAction");
        public static readonly LogChannel AI = new LogChannel("AI");
        public static readonly LogChannel UI = new LogChannel("UI");
        public static readonly LogChannel Editor = new LogChannel("Editor");
    }
}
