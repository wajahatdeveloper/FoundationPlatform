#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.DebugX
{
    /// <summary>
    /// Always-on, editor-only backing store for the DebugX Console. Producers (the
    /// <see cref="EditorConsoleSink"/> on the DebugX pipeline, and Unity's
    /// <see cref="Application.logMessageReceivedThreaded"/>) push entries onto a lock-free ingest
    /// queue from any thread. A single main-thread pump (driven from EditorApplication.update, so it
    /// runs even when the window is closed) drains that queue into a fixed-capacity ring buffer that
    /// only the main thread ever touches — the window reads it without locking.
    ///
    /// This mirrors the DiagnosticSignalBus -> TelemetryRecorder -> TelemetryWindow layering already
    /// used by the engine telemetry tooling.
    /// </summary>
    [InitializeOnLoad]
    public static class ConsoleLogStore
    {
        private const int Capacity = 16384;

        // Producer side (any thread).
        private static readonly ConcurrentQueue<ConsoleEntry> _ingest = new ConcurrentQueue<ConsoleEntry>();
        private static long _nextId;

        // Consumer side (main thread only).
        private static readonly ConsoleEntry[] _ring = new ConsoleEntry[Capacity];
        private static int _head;   // index of oldest
        private static int _count;

        private static int _logCount;
        private static int _warningCount;
        private static int _errorCount;

        // Cached compiler-entry category counts from the last pump so we can track delta and fold
        // them into the main ring-buffer counts above.
        private static int _compilerErrorCount;
        private static int _compilerWarningCount;
        private static int _compilerLogCount;

        // Watch table (main thread only).
        private static readonly Dictionary<string, WatchEntry> _watches = new Dictionary<string, WatchEntry>();
        private static readonly List<WatchEntry> _watchList = new List<WatchEntry>();

        // Editor Console mirror exposed to the window: merge of _bridgeCompilerEntries (asset-import /
        // graph compile errors, from LogEntriesBridge) and the script compile errors tracked per-assembly
        // below (from CompilationPipeline). Rebuilt in Pump() whenever either source changes.
        private static readonly List<ConsoleEntry> _compilerEntries = new List<ConsoleEntry>();

        // Raw LogEntries mirror (asset-import / graph compile errors + other native-console-only rows
        // such as shader/native logs). Compile WARNINGS are deliberately excluded from this mirror by
        // LogEntriesBridge — they still reach the console via OnUnityLog below, as ordinary Source=Unity
        // rows, so they behave like any other clearable/evictable log entry.
        private static readonly List<ConsoleEntry> _bridgeCompilerEntries = new List<ConsoleEntry>();

        // Script compile errors, keyed by assembly path — from CompilationPipeline.assemblyCompilationFinished.
        // Replaced wholesale per assembly on each compile pass; removed once that assembly compiles clean.
        private static readonly Dictionary<string, List<ConsoleEntry>> _scriptCompileErrorsByAssembly =
            new Dictionary<string, List<ConsoleEntry>>();

        // Set by OnAssemblyCompilationFinished (main thread); tells Pump() the merged mirror needs a
        // rebuild even when the native LogEntries list itself hasn't changed.
        private static bool _scriptCompilerDirty;

        // Fast-lookup set of mirrored compiler-error messages for dedup in OnUnityLog (main thread only).
        private static readonly HashSet<string> _compilerMessages = new HashSet<string>();

        /// <summary>Bumped whenever visible state changes; the window compares this to decide when to rebuild.</summary>
        public static int Version { get; private set; }

        /// <summary>Id of the most recent Error/Fatal entry appended to the ring (for pause-on-error).</summary>
        public static long LastErrorId { get; private set; }

        // --- Incremental-rebuild bookkeeping (main thread only). The filter model uses these to detect
        // what changed since its last pass: appends are cheap, everything else forces a full rebuild. ---

        /// <summary>Total entries ever appended to the ring (monotonic).</summary>
        public static long AppendedTotal { get; private set; }

        /// <summary>Total entries ever evicted from the ring (monotonic).</summary>
        public static long EvictedTotal { get; private set; }

        /// <summary>Bumped on every Clear().</summary>
        public static int ClearCount { get; private set; }

        /// <summary>Bumped whenever the compiler-diagnostics mirror actually changes.</summary>
        public static int CompilerVersion { get; private set; }

        /// <summary>Id of the oldest entry currently in the ring, or 0 when empty.</summary>
        public static long FirstId => _count > 0 ? _ring[_head].Id : 0;

        // --- Options (persisted by the window via EditorPrefs; store just reads the fields) ---
        public static bool ClearOnPlay = true;
        public static bool CaptureCompilerErrors = true;

        public static int Count => _count;
        public static int LogCount => _logCount;
        public static int WarningCount => _warningCount;
        public static int ErrorCount => _errorCount;
        public static int TotalErrorCount => _errorCount + _compilerErrorCount;
        public static IReadOnlyList<ConsoleEntry> CompilerEntries => _compilerEntries;
        public static IReadOnlyList<WatchEntry> Watches => _watchList;

        static ConsoleLogStore()
        {
            // Static ctor runs on the main thread at editor load — capture it so frame stamping and
            // the sync-console check work in edit mode, before any runtime initializer runs.
            MainThreadDispatcher.CaptureMainThread();

            Application.logMessageReceivedThreaded += OnUnityLog;
            EditorApplication.update += Pump;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            // Force one reconciliation the moment compilation ends, independent of whether the console
            // window is open — closes the tail where a duplicate could otherwise linger until the next
            // tick happens to see a change (or, before this fix, sometimes never).
            CompilationPipeline.compilationFinished += _ => Pump();

            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        }

        /// <summary>
        /// Authoritative script-compile-error feed: exact file/line/message straight from the compiler,
        /// no reflection or native-console polling involved. Replaces whatever this assembly's error list
        /// held last pass — an empty list here means the assembly now compiles clean.
        /// </summary>
        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            List<ConsoleEntry> errors = null;
            for (int i = 0; i < messages.Length; i++)
            {
                var m = messages[i];
                if (m.type != CompilerMessageType.Error)
                    continue;

                errors ??= new List<ConsoleEntry>();
                errors.Add(new ConsoleEntry
                {
                    Id = NextId(),
                    Timestamp = DateTime.Now,
                    Level = LogLevel.Error,
                    Source = ConsoleSource.Compiler,
                    Channel = "Compiler",
                    Message = m.message,
                    CallerFilePath = m.file,
                    CallerLineNumber = m.line,
                    CollapseKey = "C|" + assemblyPath + "|" + m.file + "|" + m.line + "|" + m.message
                });
            }

            if (errors != null)
                _scriptCompileErrorsByAssembly[assemblyPath] = errors;
            else
                _scriptCompileErrorsByAssembly.Remove(assemblyPath);

            _scriptCompilerDirty = true;
        }

        /// <summary>Oldest-to-newest indexed access into the ring. Main thread only.</summary>
        public static ConsoleEntry Get(int index)
        {
            if (index < 0 || index >= _count) return null;
            return _ring[(_head + index) % Capacity];
        }

        /// <summary>Assigns a monotonic id from any thread.</summary>
        public static long NextId() => Interlocked.Increment(ref _nextId);

        public static void Enqueue(ConsoleEntry entry)
        {
            if (entry == null) return;
            _ingest.Enqueue(entry);
        }

        public static void Clear()
        {
            Array.Clear(_ring, 0, Capacity);
            _head = 0;
            _count = 0;
            _logCount = _warningCount = _errorCount = 0;
            _compilerEntries.Clear();
            _bridgeCompilerEntries.Clear();
            _compilerMessages.Clear();
            _compilerErrorCount = _compilerWarningCount = _compilerLogCount = 0;
            ClearCount++;
            LogEntriesBridge.Clear();
            _scriptCompilerDirty = true; // still-broken assemblies reappear next pump, same as bridge-sourced errors
            Version++;
        }

        /// <summary>
        /// Clears the ring buffer and the native Unity Editor Console (same as Unity's Clear).
        /// Next pump rebuilds any sticky LogEntries Unity still keeps (e.g. compile errors).
        /// </summary>
        public static void ClearRuntime()
        {
            Array.Clear(_ring, 0, Capacity);
            _head = 0;
            _count = 0;
            _logCount = _warningCount = _errorCount = 0;
            _compilerEntries.Clear();
            _bridgeCompilerEntries.Clear();
            _compilerMessages.Clear();
            _compilerErrorCount = _compilerWarningCount = _compilerLogCount = 0;
            ClearCount++;
            LogEntriesBridge.Clear();
            _scriptCompilerDirty = true; // still-broken assemblies reappear next pump, same as bridge-sourced errors
            CompilerVersion++;
            Version++;
        }

        public static void ClearWatches()
        {
            _watches.Clear();
            _watchList.Clear();
            Version++;
        }

        /// <summary>Called by DebugX.Watch on the main thread (editor).</summary>
        public static void SetWatch(string name, string value)
        {
            if (string.IsNullOrEmpty(name)) return;

            if (!_watches.TryGetValue(name, out var w))
            {
                w = new WatchEntry { Name = name };
                _watches[name] = w;
                _watchList.Add(w);
            }
            w.Value = value;
            w.LastUpdate = DateTime.Now;
            w.UpdateCount++;
            Version++;
        }

        private const string PlayModeEnterTimestampKey = "DebugX.Console.PlayModeEnterTicks";

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                SessionState.SetString(PlayModeEnterTimestampKey, DateTime.Now.Ticks.ToString());
                return;
            }

            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                if (ClearOnPlay) Clear();
                DrainIngest();
                var stamp = ResolvePlayModeEnterTimestamp();
                PrependMarker("――  Entered Play Mode  " + stamp.ToString("HH:mm:ss") + "  ――", stamp);
                return;
            }

            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                DrainIngest();
                AppendMarker("――  Exited Play Mode  " + DateTime.Now.ToString("HH:mm:ss") + "  ――");
            }
        }

        private static DateTime ResolvePlayModeEnterTimestamp()
        {
            var raw = SessionState.GetString(PlayModeEnterTimestampKey, string.Empty);
            SessionState.EraseString(PlayModeEnterTimestampKey);
            if (!string.IsNullOrEmpty(raw) && long.TryParse(raw, out var ticks))
            {
                return new DateTime(ticks, DateTimeKind.Local);
            }

            return DateTime.Now;
        }

        /// <summary>Drains the ingest queue into the ring so markers are not overtaken by queued lines.</summary>
        private static void DrainIngest()
        {
            while (_ingest.TryDequeue(out var entry))
            {
                Append(entry);
            }
        }

        /// <summary>Appends a synthetic divider row (main thread). Unique collapse key so it never collapses.</summary>
        private static void AppendMarker(string text)
        {
            AppendMarker(text, DateTime.Now);
        }

        private static void AppendMarker(string text, DateTime timestamp)
        {
            long id = NextId();
            Append(new ConsoleEntry
            {
                Id = id,
                Timestamp = timestamp,
                Level = LogLevel.Information,
                Source = ConsoleSource.Marker,
                Message = text,
                CollapseKey = "M|" + id
            });
            Version++;
        }

        /// <summary>
        ///  Inserts a marker as the oldest visible row (session start bracket) using <paramref name="timestamp"/>.
        /// </summary>
        private static void PrependMarker(string text, DateTime timestamp)
        {
            long id = NextId();
            var marker = new ConsoleEntry
            {
                Id = id,
                Timestamp = timestamp,
                Level = LogLevel.Information,
                Source = ConsoleSource.Marker,
                Message = text,
                CollapseKey = "M|" + id
            };

            if (_count == Capacity)
            {
                var evicted = _ring[_head];
                if (evicted != null) Decrement(evicted.Category);
                _head = (_head + 1) % Capacity;
                _count--;
                EvictedTotal++;
            }

            _head = (_head - 1 + Capacity) % Capacity;
            _ring[_head] = marker;
            _count++;
            AppendedTotal++;
            Increment(marker.Category);
            Version++;
        }

        private static void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            // DebugX logs do not reach Unity's log system in the editor (the native relay is dropped),
            // so this feed only carries plain Debug.Log calls, uncaught exceptions and third-party logs.
            // Whether this duplicates a row already mirrored via LogEntries is decided later, in Pump's
            // drain loop (main thread only) against a freshly-refreshed _compilerMessages — not here,
            // since this callback can run off the main thread and _compilerMessages is not thread-safe.
            var level = UnityTypeToLevel(type);
            var entry = new ConsoleEntry
            {
                Id = NextId(),
                Timestamp = DateTime.Now,
                Level = level,
                Source = ConsoleSource.Unity,
                Channel = null,
                Message = condition ?? string.Empty,
                RawStackTrace = stackTrace,
                FrameCount = MainThreadDispatcher.IsMainThread ? Time.frameCount : -1,
                CollapseKey = "U|" + (int)level + "|" + condition
            };
            _ingest.Enqueue(entry);
        }

        private static void Pump()
        {
            bool changed = false;

            if (CaptureCompilerErrors)
            {
                bool bridgeChanged = LogEntriesBridge.Refresh(_bridgeCompilerEntries);
                bool scriptChanged = _scriptCompilerDirty;
                _scriptCompilerDirty = false;

                if (bridgeChanged || scriptChanged)
                {
                    _compilerEntries.Clear();
                    _compilerEntries.AddRange(_bridgeCompilerEntries);
                    foreach (var kvp in _scriptCompileErrorsByAssembly)
                        _compilerEntries.AddRange(kvp.Value);

                    int compilerErrors = 0, compilerWarnings = 0, compilerLogs = 0;
                    _compilerMessages.Clear();
                    for (int i = 0; i < _compilerEntries.Count; i++)
                    {
                        var ce = _compilerEntries[i];
                        _compilerMessages.Add(ce.Message ?? string.Empty);
                        switch (ce.Category)
                        {
                            case ConsoleCategory.Error:   compilerErrors++; break;
                            case ConsoleCategory.Warning: compilerWarnings++; break;
                            default:                      compilerLogs++;    break;
                        }
                    }

                    if (_compilerEntries.Count > 0)
                    {
                        int removed = 0;
                        int write = 0;
                        for (int read = 0; read < _count; read++)
                        {
                            var ringEntry = _ring[(_head + read) % Capacity];
                            bool match = ringEntry != null &&
                                _compilerMessages.Contains(ringEntry.Message ?? string.Empty) &&
                                ringEntry.Source == ConsoleSource.Unity;
                            if (match)
                            {
                                Decrement(ringEntry.Category);
                                removed++;
                                continue;
                            }
                            if (write != read)
                                _ring[(_head + write) % Capacity] = ringEntry;
                            write++;
                        }
                        for (int i = write; i < _count; i++)
                            _ring[(_head + i) % Capacity] = null;
                        _count = write;
                    }

                    _errorCount   += compilerErrors   - _compilerErrorCount;
                    _warningCount += compilerWarnings - _compilerWarningCount;
                    _logCount     += compilerLogs     - _compilerLogCount;
                    _compilerErrorCount   = compilerErrors;
                    _compilerWarningCount = compilerWarnings;
                    _compilerLogCount    = compilerLogs;
                    CompilerVersion++;
                    changed = true;
                }
            }
            else if (_compilerErrorCount + _compilerWarningCount + _compilerLogCount > 0)
            {
                _errorCount   -= _compilerErrorCount;
                _warningCount -= _compilerWarningCount;
                _logCount     -= _compilerLogCount;
                _compilerErrorCount = _compilerWarningCount = _compilerLogCount = 0;
                CompilerVersion++;
                changed = true;
            }

            // Drain after refreshing the mirror above, so this tick's dedup check sees the freshest
            // possible _compilerMessages — closes the race where a message reaches OnUnityLog before the
            // native LogEntries mirror has caught up to it on the same tick.
            while (_ingest.TryDequeue(out var entry))
            {
                if (CaptureCompilerErrors &&
                    entry.Source == ConsoleSource.Unity &&
                    _compilerMessages.Contains(entry.Message ?? string.Empty))
                    continue;

                Append(entry);
                changed = true;
            }

            if (changed)
                Version++;
        }

        private static void Append(ConsoleEntry entry)
        {
            if (_count == Capacity)
            {
                // Evict oldest and adjust running counts.
                var evicted = _ring[_head];
                if (evicted != null) Decrement(evicted.Category);
                _ring[_head] = entry;
                _head = (_head + 1) % Capacity;
                EvictedTotal++;
            }
            else
            {
                _ring[(_head + _count) % Capacity] = entry;
                _count++;
            }
            AppendedTotal++;
            Increment(entry.Category);

            if (entry.Category == ConsoleCategory.Error)
                LastErrorId = entry.Id;
        }

        private static void Increment(ConsoleCategory c)
        {
            switch (c)
            {
                case ConsoleCategory.Warning: _warningCount++; break;
                case ConsoleCategory.Error: _errorCount++; break;
                default: _logCount++; break;
            }
        }

        private static void Decrement(ConsoleCategory c)
        {
            switch (c)
            {
                case ConsoleCategory.Warning: if (_warningCount > 0) _warningCount--; break;
                case ConsoleCategory.Error: if (_errorCount > 0) _errorCount--; break;
                default: if (_logCount > 0) _logCount--; break;
            }
        }

        private static LogLevel UnityTypeToLevel(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return LogLevel.Error;
                case LogType.Warning:
                    return LogLevel.Warning;
                default:
                    return LogLevel.Information;
            }
        }
    }
}
#endif
