#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
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

        // Compiler/import diagnostics, rebuilt from UnityEditor.LogEntries each poll (main thread).
        private static readonly List<ConsoleEntry> _compilerEntries = new List<ConsoleEntry>();

        // Fast-lookup set of compiler entry messages for dedup in OnUnityLog (main thread only).
        private static readonly HashSet<string> _compilerMessages = new HashSet<string>();

        // Warnings suppressed by ClearRuntime so they don't reappear on the next LogEntries refresh.
        private static readonly HashSet<string> _suppressedCompilerWarnings = new HashSet<string>();

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
            _compilerMessages.Clear();
            _compilerErrorCount = _compilerWarningCount = _compilerLogCount = 0;
            _suppressedCompilerWarnings.Clear();
            ClearCount++;
            LogEntriesBridge.Clear();
            Version++;
        }

        /// <summary>
        /// Clears only runtime logs (ring buffer). Compiler diagnostics and their counts are preserved,
        /// except that compiler warnings are removed from the store and suppressed so they don't
        /// reappear on the next LogEntries refresh. Compiler errors remain visible.
        /// </summary>
        public static void ClearRuntime()
        {
            Array.Clear(_ring, 0, Capacity);
            _head = 0;
            _count = 0;
            _logCount = _warningCount = _errorCount = 0;
            ClearCount++;
            Version++;

            for (int i = _compilerEntries.Count - 1; i >= 0; i--)
            {
                var ce = _compilerEntries[i];
                if (ce.Category == ConsoleCategory.Warning)
                {
                    _suppressedCompilerWarnings.Add(ce.Message ?? string.Empty);
                    _compilerEntries.RemoveAt(i);
                }
            }
            _compilerWarningCount = 0;
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
#if UNITY_EDITOR && DEBUG
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) &&
                (condition ?? "").Contains("CS0246"))
            {
                UnityEngine.Debug.LogWarning($"[ConsoleLogStore.OnUnityLog] Source=UnityLog Count={_count} CompilerMessagesCount={_compilerMessages.Count} ConditionHash={(condition ?? "").GetHashCode()} Condition='{condition}'");
            }
#endif
            // DebugX logs do not reach Unity's log system in the editor (the native relay is dropped),
            // so this feed only carries plain Debug.Log calls, uncaught exceptions and third-party logs.
            // Skip messages already captured as compiler/import diagnostics to avoid duplicates.
            if (CaptureCompilerErrors && _compilerMessages.Contains(condition ?? string.Empty))
                return;

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

            while (_ingest.TryDequeue(out var entry))
            {
                Append(entry);
                changed = true;
            }

            if (CaptureCompilerErrors)
            {
                if (LogEntriesBridge.Refresh(_compilerEntries))
                {
                    int compilerErrors = 0, compilerWarnings = 0, compilerLogs = 0;
                    _compilerMessages.Clear();
                    for (int i = _compilerEntries.Count - 1; i >= 0; i--)
                    {
                        var ce = _compilerEntries[i];
                        if (ce.Category == ConsoleCategory.Warning &&
                            _suppressedCompilerWarnings.Contains(ce.Message ?? string.Empty))
                        {
                            _compilerEntries.RemoveAt(i);
                        }
                    }
                    for (int i = 0; i < _compilerEntries.Count; i++)
                    {
                        var ce = _compilerEntries[i];
                        if (ce.Category == ConsoleCategory.Error)
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
                        var compilerSet = new HashSet<string>(_compilerEntries.Count);
                        foreach (var ce in _compilerEntries)
                            compilerSet.Add(ce.Message ?? string.Empty);

                        int removed = 0;
                        int write = 0;
                        for (int read = 0; read < _count; read++)
                        {
                            var entry = _ring[(_head + read) % Capacity];
                            bool match = entry != null &&
                                compilerSet.Contains(entry.Message ?? string.Empty) &&
                                entry.Source == ConsoleSource.Unity &&
                                entry.Category == ConsoleCategory.Error;
                            if (match)
                            {
                                Decrement(entry.Category);
                                removed++;
                                continue;
                            }
                            if (write != read)
                                _ring[(_head + write) % Capacity] = entry;
                            write++;
                        }
                        for (int i = write; i < _count; i++)
                            _ring[(_head + i) % Capacity] = null;
                        _count = write;
#if UNITY_EDITOR && DEBUG
                        UnityEngine.Debug.LogWarning($"[ConsoleLogStore.Pump] Compaction: removed={removed} remainingRing={_count} compilerEntries={_compilerEntries.Count}");
#endif
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
