#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AetherNexus.FoundationPlatform.DebugX
{
    /// <summary>
    /// Reflection wrapper over Unity's internal <c>UnityEditor.LogEntries</c> / <c>LogEntry</c> so the
    /// DebugX Console can mirror the Editor Console (including rows that never reach
    /// <see cref="UnityEngine.Application.logMessageReceived"/>, e.g. some shader/native logs).
    ///
    /// Severity follows Unity's <c>ConsoleWindow.GetIconForErrorMode</c> mode masks.
    /// <see cref="ConsoleSource.Compiler"/> here is reserved for persistent, currently-present asset-import/
    /// graph compile ERRORS only — script compile errors are sourced elsewhere (see
    /// <see cref="ConsoleLogStore"/>'s <c>CompilationPipeline.assemblyCompilationFinished</c> subscription)
    /// since Unity exposes those via a public, non-reflection API. Compile warnings are intentionally NOT
    /// mirrored here at all — they already reach the console via
    /// <see cref="UnityEngine.Application.logMessageReceivedThreaded"/> (<see cref="ConsoleLogStore.OnUnityLog"/>),
    /// so mirroring them here too would just duplicate them as an unevictable row. All other mirrored rows
    /// are <see cref="ConsoleSource.Unity"/>.
    ///
    /// Lives in the runtime assembly (editor-guarded) so <see cref="ConsoleLogStore"/> can call it —
    /// the runtime asmdef cannot reference the editor asmdef. Every member lookup is cached and
    /// null-guarded; if Unity changes these internals the bridge silently disables itself.
    /// </summary>
    internal static class LogEntriesBridge
    {
        // ConsoleWindow.Mode bits — UnityCsReference Editor/Mono/ConsoleWindow.cs
        private const int ModeError = 1 << 0;
        private const int ModeAssert = 1 << 1;
        private const int ModeLog = 1 << 2;
        private const int ModeFatal = 1 << 4;
        private const int ModeAssetImportError = 1 << 6;
        private const int ModeAssetImportWarning = 1 << 7;
        private const int ModeScriptingError = 1 << 8;
        private const int ModeScriptingWarning = 1 << 9;
        private const int ModeScriptingLog = 1 << 10;
        private const int ModeScriptCompileError = 1 << 11;
        private const int ModeScriptCompileWarning = 1 << 12;

        // Unity's own "survives Clear / domain reload" bit. Not used for Compiler classification below —
        // a real compile error already carries its own specific bit (CompileErrorMask), and "present" is
        // guaranteed here regardless since this bridge polls the live native console every tick.
        private const int ModeStickyError = 1 << 13;
        private const int ModeScriptingException = 1 << 17;
        private const int ModeGraphCompileError = 1 << 20;
        private const int ModeScriptingAssertion = 1 << 21;
        private const int ModeVisualScriptingError = 1 << 22;

        /// <summary>
        /// Asset-import / graph compile ERRORS only — the persistent Compiler source mirrored here.
        /// Script compile errors are deliberately excluded: <see cref="ConsoleLogStore"/> sources those
        /// directly from <see cref="UnityEditor.Compilation.CompilationPipeline.assemblyCompilationFinished"/>
        /// instead (public API, exact file/line/message from the compiler — no reflection, no polling).
        /// </summary>
        private const int CompileErrorMask =
            ModeAssetImportError | ModeGraphCompileError;

        /// <summary>
        /// Compile warnings: not classified as Compiler (not a blocker) and not mirrored at all — they
        /// reach the console via <see cref="UnityEngine.Application.logMessageReceivedThreaded"/> instead.
        /// </summary>
        private const int CompileWarningMask = ModeAssetImportWarning | ModeScriptCompileWarning;

        // Matches ConsoleWindow.GetIconForErrorMode error branch (+ exception / visual-scripting).
        private const int ErrorMask =
            ModeFatal | ModeAssert | ModeError | ModeScriptingError |
            ModeAssetImportError | ModeScriptCompileError | ModeGraphCompileError |
            ModeScriptingAssertion | ModeScriptingException | ModeVisualScriptingError;

        // Matches ConsoleWindow.GetIconForErrorMode warning branch.
        private const int WarningMask =
            ModeScriptCompileWarning | ModeScriptingWarning | ModeAssetImportWarning;

        private static bool _resolved;
        private static bool _available;

        private static MethodInfo _startGetting;
        private static MethodInfo _endGetting;
        private static MethodInfo _getCount;
        private static MethodInfo _getEntryInternal;
        private static MethodInfo _clear;
        private static object _entryInstance;
        private static FieldInfo _fMessage;
        private static FieldInfo _fFile;
        private static FieldInfo _fLine;
        private static FieldInfo _fMode;

        // Change-detector snapshot of the last-seen native console content (mode + message per row, in
        // order). A plain count/first-last hash misses mid-list changes (e.g. one sticky error replaced
        // by another with the same total count); comparing every row is the only way to catch those, and
        // is cheap since the reflection walk below already visits every row regardless.
        private static readonly List<int> _prevModes = new List<int>();
        private static readonly List<string> _prevMessages = new List<string>();
        private static readonly List<int> _scratchModes = new List<int>();
        private static readonly List<string> _scratchMessages = new List<string>();
        private static readonly List<string> _scratchFiles = new List<string>();
        private static readonly List<int> _scratchLines = new List<int>();

        /// <summary>
        /// Rebuilds <paramref name="target"/> from the current Editor Console rows if they
        /// changed since the last call. Returns true when the list was modified.
        /// </summary>
        public static bool Refresh(List<ConsoleEntry> target)
        {
            if (!Resolve())
            {
                if (target.Count == 0) return false;
                target.Clear();
                _prevModes.Clear();
                _prevMessages.Clear();
                return true;
            }

            int count;
            try { count = (int)_getCount.Invoke(null, null); }
            catch { return false; }

            try
            {
                _startGetting.Invoke(null, null);
                try
                {
                    var args = new object[2];

                    _scratchModes.Clear();
                    _scratchMessages.Clear();
                    _scratchFiles.Clear();
                    _scratchLines.Clear();

                    for (int i = 0; i < count; i++)
                    {
                        args[0] = i;
                        args[1] = _entryInstance;
                        bool ok;
                        try { ok = (bool)_getEntryInternal.Invoke(null, args); }
                        catch { continue; }
                        if (!ok) continue;

                        int mode = _fMode != null ? Convert.ToInt32(_fMode.GetValue(_entryInstance)) : 0;

                        string message = _fMessage != null ? _fMessage.GetValue(_entryInstance) as string : null;
                        if (string.IsNullOrEmpty(message))
                            continue;

                        string file = _fFile != null ? _fFile.GetValue(_entryInstance) as string : null;
                        int line = _fLine != null ? Convert.ToInt32(_fLine.GetValue(_entryInstance)) : 0;

                        _scratchModes.Add(mode);
                        _scratchMessages.Add(message);
                        _scratchFiles.Add(file);
                        _scratchLines.Add(line);
                    }

                    if (!ContentChanged())
                        return false;

                    target.Clear();
                    for (int i = 0; i < _scratchModes.Count; i++)
                    {
                        int mode = _scratchModes[i];
                        string message = _scratchMessages[i];

                        bool isCompilerError = (mode & CompileErrorMask) != 0;
                        bool isDeclassifiedWarning = !isCompilerError && (mode & CompileWarningMask) != 0;
                        if (isDeclassifiedWarning)
                            continue; // reaches the console via Application.logMessageReceivedThreaded instead

                        bool isScriptCompileError = (mode & ModeScriptCompileError) != 0;
                        if (isScriptCompileError)
                            continue; // ConsoleLogStore mirrors these itself via CompilationPipeline instead

                        var level = LevelFromMode(mode, message);
                        var source = isCompilerError ? ConsoleSource.Compiler : ConsoleSource.Unity;

                        target.Add(new ConsoleEntry
                        {
                            Id = ConsoleLogStore.NextId(),
                            Timestamp = DateTime.Now,
                            Level = level,
                            Source = source,
                            Channel = isCompilerError ? "Compiler" : null,
                            Message = message,
                            CallerFilePath = _scratchFiles[i],
                            CallerLineNumber = _scratchLines[i],
                            CollapseKey = (isCompilerError ? "C|" : "U|") + mode + "|" + message
                        });
                    }

                    // Commit the scratch snapshot as the new "previous" for next call's comparison.
                    // Clear+AddRange reuses each list's backing array once capacity settles, so this
                    // does not allocate on steady-state ticks.
                    _prevModes.Clear();
                    _prevModes.AddRange(_scratchModes);
                    _prevMessages.Clear();
                    _prevMessages.AddRange(_scratchMessages);
                }
                finally
                {
                    _endGetting.Invoke(null, null);
                }
            }
            catch
            {
                // Something in the internal API changed mid-iteration; disable to avoid repeat throws.
                _available = false;
                _resolved = true;
            }

            return true;
        }

        /// <summary>True when the current scratch snapshot differs from the last-committed one.</summary>
        private static bool ContentChanged()
        {
            if (_scratchModes.Count != _prevModes.Count) return true;
            for (int i = 0; i < _scratchModes.Count; i++)
            {
                if (_scratchModes[i] != _prevModes[i]) return true;
                if (!string.Equals(_scratchMessages[i], _prevMessages[i], StringComparison.Ordinal)) return true;
            }
            return false;
        }

        /// <summary>
        /// Maps Unity Console mode bits to <see cref="LogLevel"/> using the same masks as
        /// <c>ConsoleWindow.GetIconForErrorMode</c>. Prefix fallback only when mode has no severity bits.
        /// </summary>
        private static LogLevel LevelFromMode(int mode, string message)
        {
            if ((mode & ErrorMask) != 0)
                return LogLevel.Error;
            if ((mode & WarningMask) != 0)
                return LogLevel.Warning;
            if ((mode & (ModeLog | ModeScriptingLog)) != 0)
                return LogLevel.Information;

            if (StartsWithOrdinalIgnoreCase(message, "Shader error") ||
                StartsWithOrdinalIgnoreCase(message, "Error:") ||
                message.StartsWith("Error-", StringComparison.Ordinal))
                return LogLevel.Error;

            if (StartsWithOrdinalIgnoreCase(message, "Shader warning") ||
                StartsWithOrdinalIgnoreCase(message, "Warning:") ||
                message.StartsWith("Warning-", StringComparison.Ordinal))
                return LogLevel.Warning;

            return LogLevel.Information;
        }

        private static bool StartsWithOrdinalIgnoreCase(string message, string prefix)
        {
            return message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Clears the native Unity Editor Console logs.
        /// </summary>
        public static void Clear()
        {
            if (!Resolve()) return;
            try
            {
                _clear?.Invoke(null, null);
                _prevModes.Clear();
                _prevMessages.Clear();
            }
            catch
            {
                // Silently ignore if internal API changes
            }
        }

        private static bool Resolve()
        {
            if (_resolved) return _available;
            _resolved = true;
            _available = false;

            try
            {
                var editorAsm = typeof(UnityEditor.Editor).Assembly;
                var logEntries = editorAsm.GetType("UnityEditor.LogEntries");
                var logEntry = editorAsm.GetType("UnityEditor.LogEntry");
                if (logEntries == null || logEntry == null)
                    return false;

                const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                const BindingFlags Inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                _startGetting = logEntries.GetMethod("StartGettingEntries", Static);
                _endGetting = logEntries.GetMethod("EndGettingEntries", Static);
                _getCount = logEntries.GetMethod("GetCount", Static);
                _getEntryInternal = logEntries.GetMethod("GetEntryInternal", Static);
                _clear = logEntries.GetMethod("Clear", Static);
                _fMessage = logEntry.GetField("message", Inst);
                _fFile = logEntry.GetField("file", Inst);
                _fLine = logEntry.GetField("line", Inst);
                _fMode = logEntry.GetField("mode", Inst);

                if (_startGetting == null || _endGetting == null || _getCount == null || _getEntryInternal == null)
                    return false;

                _entryInstance = Activator.CreateInstance(logEntry);
                _available = _entryInstance != null;
            }
            catch
            {
                _available = false;
            }

            return _available;
        }
    }
}
#endif
