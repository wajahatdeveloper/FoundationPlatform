#if UNITY_EDITOR
using System;

namespace DebugXLogging.ConsoleView
{
    /// <summary>
    /// Where a console row originated. DebugX = structured pipeline, Unity = plain Debug.Log /
    /// exceptions / third-party captured via Application.logMessageReceived, Compiler = editor-time
    /// compile/import diagnostics mirrored from UnityEditor.LogEntries.
    /// </summary>
    public enum ConsoleSource
    {
        DebugX,
        Unity,
        Compiler,
        /// <summary>Synthetic divider row (play-mode transitions). Bypasses filters, never collapses.</summary>
        Marker
    }

    /// <summary>
    /// Broad category used for the toolbar toggles and counts (mirrors Unity's Log/Warning/Error split).
    /// </summary>
    public enum ConsoleCategory
    {
        Log = 0,
        Warning = 1,
        Error = 2
    }

    /// <summary>
    /// One row in the console. Populated on whatever thread produced the log (worker or main); all
    /// fields are plain data or object references only — no UnityEngine.Object members are touched off
    /// the main thread. Derived strings (property text, filtered stack, source snippet) are computed
    /// lazily on the main thread by the window and cached back onto the entry.
    /// </summary>
    public sealed class ConsoleEntry
    {
        // --- Captured at log time (any thread) ---
        public long Id;
        public DateTime Timestamp;
        public LogLevel Level;
        public ConsoleSource Source;
        public string Channel;
        public string SourceContext;
        public string Message;
        public string RawStackTrace;
        public string ExceptionText;
        public LogProperty[] Properties;
        public string CallerFilePath;
        public int CallerLineNumber;
        public string CallerMember;
        public UnityEngine.Object UnityContext;

        /// <summary>Time.frameCount at log time (main-thread logs only), otherwise -1.</summary>
        public int FrameCount = -1;

        /// <summary>Identity used for collapse-identical grouping. String-only so it is safe to build off-thread.</summary>
        public string CollapseKey;

        // --- Derived lazily on the main thread (see window) ---
        public string PropertiesText;   // "k=v, k2=v2"
        public string DisplayStack;     // filtered stack for the detail pane
        public bool DerivedBuilt;
        public string CallerSummary;    // "Assets/Foo.cs:42 Method()" for the two-line row mode
        public bool CallerSummaryBuilt;

        public ConsoleCategory Category => LevelToCategory(Level);

        public static ConsoleCategory LevelToCategory(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Warning: return ConsoleCategory.Warning;
                case LogLevel.Error:
                case LogLevel.Fatal: return ConsoleCategory.Error;
                default: return ConsoleCategory.Log;
            }
        }
    }

    /// <summary>A single tracked variable in the Watch panel. Updated in place, never spams the stream.</summary>
    public sealed class WatchEntry
    {
        public string Name;
        public string Value;
        public DateTime LastUpdate;
        public int UpdateCount;
    }
}
#endif
