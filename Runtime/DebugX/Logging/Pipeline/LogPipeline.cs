using System.Collections.Generic;
using System.Threading;

namespace FoundationPlatform.DebugX
{
    /// <summary>
    /// Routes log events to configured sinks with filtering.
    ///
    /// Sink lists are copy-on-write immutable arrays: Configure/AddSink build new arrays under a lock
    /// and publish them with a volatile write, while the emit paths only read a snapshot — no lock is
    /// held while sinks run, so a slow file write can never block a logging thread.
    /// </summary>
    public static class LogPipeline
    {
        private static readonly ILogSink[] Empty = new ILogSink[0];

        // Copy-on-write snapshots. Replaced wholesale under _configLock, read lock-free.
        private static volatile ILogSink[] _sinks = Empty;
        private static volatile ILogSink[] _mainThreadSinks = Empty;
        private static volatile ILogSink[] _backgroundSinks = Empty;
        private static volatile bool _isInitialized = false;

        private static volatile LogLevel _minimumLevel = LogLevel.Debug;
        private static volatile HashSet<string> _excludedChannels;
        private static readonly object _configLock = new object();

        public static void Configure(System.Action<LogPipelineConfig> configure)
        {
            var config = new LogPipelineConfig();
            configure(config);

            lock (_configLock)
            {
                _minimumLevel = config.MinimumLevel;
                _excludedChannels = config.ExcludedChannels != null ? new HashSet<string>(config.ExcludedChannels) : null;

                // Apply channel filters to all sinks that support them
                foreach (var sink in config.Sinks)
                {
                    if (sink is LogSinkBase sinkBase)
                    {
                        sinkBase.SetChannelFilters(config.ExcludedChannels);
                    }
                }

                Publish(config.Sinks.ToArray());
                _isInitialized = true;
            }

            // Start async queue and ensure main thread dispatcher exists
            LogQueue.Start();
            MainThreadDispatcher.EnsureExists();
        }

        /// <summary>
        /// Changes the pipeline's minimum level at runtime (e.g. from the DebugX Console settings page).
        /// Takes effect immediately for ShouldEmit / Emit. Does not persist — callers own persistence.
        /// </summary>
        public static void SetMinimumLevel(LogLevel level)
        {
            _minimumLevel = level;
        }

        /// <summary>Current pipeline minimum level.</summary>
        public static LogLevel MinimumLevel => _minimumLevel;

        public static void AddSink(ILogSink sink)
        {
            if (sink == null) return;

            lock (_configLock)
            {
                var current = _sinks;
                for (int i = 0; i < current.Length; i++)
                    if (ReferenceEquals(current[i], sink))
                        return;

                var next = new ILogSink[current.Length + 1];
                current.CopyTo(next, 0);
                next[current.Length] = sink;
                Publish(next);
            }
        }

        /// <summary>Rebuilds the published snapshots from a full sink array. Call under _configLock.</summary>
        private static void Publish(ILogSink[] all)
        {
            var main = new List<ILogSink>();
            var background = new List<ILogSink>();
            foreach (var sink in all)
            {
                if (sink.RequiresMainThread) main.Add(sink);
                else background.Add(sink);
            }

            _mainThreadSinks = main.Count > 0 ? main.ToArray() : Empty;
            _backgroundSinks = background.Count > 0 ? background.ToArray() : Empty;
            _sinks = all;
        }

        /// <summary>
        /// Returns true if the pipeline would emit a log at the given level and channel.
        /// Call before building the message to avoid string allocation when filtered out.
        /// </summary>
        public static bool ShouldEmit(LogLevel level, string channel)
        {
            if (level < _minimumLevel)
                return false;

            if (!string.IsNullOrEmpty(channel) && _excludedChannels != null && _excludedChannels.Contains(channel))
                return false;

            return true;
        }

        [UnityEngine.HideInCallstack]
        public static void Emit(LogEvent logEvent)
        {
            // Fast path: check minimum level (thread-safe read)
            if (logEvent.Level < _minimumLevel)
                return;

            bool syncConsole = DebugXBuilder.UseSyncConsole
                && MainThreadDispatcher.MainThreadId != 0
                && Thread.CurrentThread.ManagedThreadId == MainThreadDispatcher.MainThreadId;

            if (syncConsole && _isInitialized)
            {
                var mainSinks = _mainThreadSinks;
                if (mainSinks.Length > 0)
                {
                    EmitToSinks(mainSinks, logEvent);
                    LogQueue.Enqueue(logEvent, backgroundOnly: true);
                    return;
                }
            }

            LogQueue.Enqueue(logEvent, backgroundOnly: false);
        }

        [UnityEngine.HideInCallstack]
        internal static void ProcessLogEvent(LogEvent logEvent, bool backgroundOnly = false)
        {
            if (!_isInitialized || _sinks.Length == 0)
            {
                // Fallback to Unity Debug if no sinks configured
                // This must run on main thread
                LogQueue.EnqueueMainThreadAction(() => FallbackLog(logEvent));
                return;
            }

            // Process background sinks (file sinks) on worker thread
            EmitToSinks(_backgroundSinks, logEvent);

            // Queue main thread sinks (console sinks) for main thread execution, unless already emitted synchronously
            if (!backgroundOnly && _mainThreadSinks.Length > 0)
            {
                LogQueue.EnqueueMainThreadAction(() => EmitToSinks(_mainThreadSinks, logEvent));
            }
        }

        [UnityEngine.HideInCallstack]
        private static void EmitToSinks(ILogSink[] sinks, LogEvent logEvent)
        {
            foreach (var sink in sinks)
            {
                try
                {
                    if (sink.ShouldEmit(logEvent))
                        sink.Emit(logEvent);
                }
                catch (System.Exception ex)
                {
                    // Don't let sink failures break logging
                    UnityEngine.Debug.LogWarning($"[LogPipeline] Sink {sink.GetType().Name} failed: {ex.Message}");
                }
            }
        }

        [UnityEngine.HideInCallstack]
        private static void FallbackLog(LogEvent logEvent)
        {
            string message = $"[{logEvent.Channel ?? "Default"}] {logEvent.Message}";

            switch (logEvent.Level)
            {
                case LogLevel.Error:
                case LogLevel.Fatal:
                    UnityEngine.Debug.LogError(message, logEvent.UnityContext);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(message, logEvent.UnityContext);
                    break;
                default:
                    UnityEngine.Debug.Log(message, logEvent.UnityContext);
                    break;
            }
        }

        /// <summary>
        /// Shutdown the pipeline and flush all pending logs
        /// </summary>
        public static void Shutdown()
        {
            LogQueue.Stop();
        }
    }
}
