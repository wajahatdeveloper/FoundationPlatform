using UnityEngine;

namespace FoundationPlatform.DebugX
{
    /// <summary>
    /// Initialize logging on app start
    /// </summary>
    public static class DebugXInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            // Runs on the main thread — capture it so LogEvent can stamp frame numbers before the
            // dispatcher GameObject exists.
            MainThreadDispatcher.CaptureMainThread();

            // Configure caller info helper settings
            CallerInfoHelper.SkipMethodsContainingLog = true;

            // Configure stack trace settings (editor: from menu/prefs, else false)
#if UNITY_EDITOR
            DebugXBuilder.EnableFullStackTraces = UnityEditor.EditorPrefs.GetBool(DebugX.PrefKeyCaptureFullStackTraces, false);
            DebugXBuilder.UseSyncConsole = UnityEditor.EditorPrefs.GetBool(DebugX.PrefKeySyncConsole, false);
#endif

#if UNITY_EDITOR
            ConfigureEditorLogging();
#elif UNITY_ANDROID
            ConfigureAndroidLogging();
#elif UNITY_WEBGL
            ConfigureWebGLLogging();
#else
            DebugXBuilder.EnableFullStackTraces = false;
            ConfigureDefaultLogging();
#endif

            // Register application quit handler
            Application.quitting += OnApplicationQuitting;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            // Handle domain reload in editor
            Application.quitting -= OnApplicationQuitting;
        }

        private static void OnApplicationQuitting()
        {
            // Shutdown pipeline and flush all sinks
            LogPipeline.Shutdown();
        }

#if UNITY_EDITOR
        private static void ConfigureEditorLogging()
        {
            string logPath = Application.dataPath + "/../Logs/Editor";

            var fileSink = new FileSink(logPath, LogLevel.Debug);
            var jsonSink = new JsonFileSink(logPath + "/Structured", LogLevel.Information);

            // Editor floor is developer-configurable via the DebugX Console settings page (default Debug,
            // so per-op Verbose traces like [GAS:TagTrace] stay filtered — and their StackTrace capture is
            // skipped by the ShouldEmit gate — unless a developer opts into Verbose).
            var editorMinLevel = (LogLevel)UnityEditor.EditorPrefs.GetInt(
                DebugX.PrefKeyEditorMinLevel, (int)LogLevel.Debug);

            // In-editor the DebugX Console is the sole console: EditorConsoleSink feeds the console
            // store with structured entries and does NOT relay to UnityEngine.Debug.Log.
            LogPipeline.Configure(config => config
                .SetMinimumLevel(editorMinLevel)
                .AddSink(new EditorConsoleSink())
                .AddSink(fileSink)
                .AddSink(jsonSink)
                .ExcludeChannels()
            );

            FlushScheduler.EnsureExists();
            FlushScheduler.RegisterFileSink(fileSink);
            FlushScheduler.RegisterJsonFileSink(jsonSink);

            DebugX.Logger(LogChannels.Engine).Info("Editor logging initialized");
        }
#endif

        private static void ConfigureAndroidLogging()
        {
            string logPath = Application.persistentDataPath + "/Logs";
            
            FileSink fileSink = null;
            LogPipeline.Configure(config =>
            {
                config.SetMinimumLevel(LogLevel.Information)
                      .AddSink(new UnityConsoleSink(includeCallerInfo: false))
                      .ExcludeChannels();

#if DEVELOPMENT_BUILD
                fileSink = new FileSink(logPath, LogLevel.Warning);
                config.AddSink(fileSink);
#endif
            });

            if (fileSink != null)
            {
                FlushScheduler.EnsureExists();
                FlushScheduler.RegisterFileSink(fileSink);
            }

            DebugX.Info("Android logging initialized");
        }

        private static void ConfigureWebGLLogging()
        {
            LogPipeline.Configure(config => config
                .SetMinimumLevel(LogLevel.Information)
                .AddSink(new UnityConsoleSink(includeCallerInfo: false))
                .ExcludeChannels()
            );

            DebugX.Info("WebGL logging initialized");
        }

        private static void ConfigureDefaultLogging()
        {
            string logPath = Application.dataPath + "/Logs";
            
            var fileSink = new FileSink(logPath, LogLevel.Debug);
            var jsonSink = new JsonFileSink(logPath + "/Structured", LogLevel.Information);
            
            LogPipeline.Configure(config => config
            #if DEVELOPMENT_BUILD
                .SetMinimumLevel(LogLevel.Verbose)
            #else
                .SetMinimumLevel(LogLevel.Information)
            #endif
                .AddSink(new UnityConsoleSink(includeCallerInfo: false))
                .AddSink(fileSink)
                .AddSink(jsonSink)
                .ExcludeChannels()
            );

            FlushScheduler.EnsureExists();
            FlushScheduler.RegisterFileSink(fileSink);
            FlushScheduler.RegisterJsonFileSink(jsonSink);

            DebugX.Info("Default logging initialized");
        }
    }
}

