#if UNITY_EDITOR
namespace FoundationPlatform.DebugX
{
    /// <summary>
    /// DebugX pipeline sink that feeds the in-editor <see cref="ConsoleLogStore"/> with fully
    /// structured entries. Deliberately does NOT relay to UnityEngine.Debug.Log — in the editor the
    /// DebugX Console is the sole console, so relaying would be redundant (the file/json sinks already
    /// fan out) and would double-count against the Unity-log capture feed.
    ///
    /// Runs on the pipeline's worker thread (RequiresMainThread = false); it only copies plain data
    /// and object references, never touching UnityEngine.Object members off the main thread.
    /// </summary>
    public sealed class EditorConsoleSink : LogSinkBase
    {
        public EditorConsoleSink(LogLevel minimumLevel = LogLevel.Verbose)
        {
            MinimumLevel = minimumLevel;
        }

        public override void Emit(LogEvent logEvent)
        {
            var entry = new ConsoleEntry
            {
                Id = ConsoleLogStore.NextId(),
                Timestamp = logEvent.Timestamp,
                Level = logEvent.Level,
                Source = ConsoleSource.DebugX,
                Channel = logEvent.Channel,
                SourceContext = logEvent.SourceContext,
                Message = logEvent.Message,
                RawStackTrace = logEvent.StackTrace,
                ExceptionText = logEvent.Exception != null ? logEvent.Exception.ToString() : null,
                Properties = logEvent.Properties,
                UnityContext = logEvent.UnityContext,
                FrameCount = logEvent.FrameCount,
                CollapseKey = BuildCollapseKey(logEvent)
            };

            if (!logEvent.Caller.IsEmpty)
            {
                entry.CallerFilePath = logEvent.Caller.FilePath;
                entry.CallerLineNumber = logEvent.Caller.LineNumber;
                entry.CallerMember = logEvent.Caller.MemberName;
            }

            ConsoleLogStore.Enqueue(entry);
        }

        private static string BuildCollapseKey(LogEvent e)
        {
            // String-only so it is safe to build off the main thread.
            return "D|" + (int)e.Level + "|" + (e.Channel ?? "") + "|" + e.Message;
        }
    }
}
#endif
