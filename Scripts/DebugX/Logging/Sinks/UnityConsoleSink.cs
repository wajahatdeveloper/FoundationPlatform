using System.Text;
using UnityEngine;

namespace DebugXLogging
{
    /// <summary>
    /// Relays DebugX log events to UnityEngine.Debug so builds still surface logs through the platform
    /// console (logcat / browser console / Player.log) and Unity's native error handling. This is the
    /// surviving half of the old ConsoleProSink, minus the CPAPI markers.
    ///
    /// In the editor the DebugX Console (EditorConsoleSink + ConsoleLogStore) is the sole console, so
    /// this sink is registered only in the non-editor pipeline configs.
    ///
    /// Must run on the main thread (calls UnityEngine.Debug).
    /// </summary>
    public sealed class UnityConsoleSink : LogSinkBase
    {
        private readonly bool _includeCallerInfo;

        public override bool RequiresMainThread => true;

        public UnityConsoleSink(LogLevel minimumLevel = LogLevel.Debug, bool includeCallerInfo = false)
        {
            MinimumLevel = minimumLevel;
            _includeCallerInfo = includeCallerInfo;
        }

        [UnityEngine.HideInCallstack]
        public override void Emit(LogEvent logEvent)
        {
            string message = FormatMessage(logEvent);

            string channel = logEvent.Channel;
            if (!string.IsNullOrEmpty(channel))
                message = $"[{channel}] {message}";

            // Append Unity clickable stack so double-click opens the correct file:line.
            message += UnityConsoleStackFormatter.FormatUnityClickableStack(logEvent);

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

        private string FormatMessage(LogEvent logEvent)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(logEvent.SourceContext))
                sb.Append($"[{logEvent.SourceContext}] ");

            sb.Append(logEvent.Message);

            if (logEvent.Properties != null && logEvent.Properties.Length > 0)
            {
                bool first = true;
                for (int i = 0; i < logEvent.Properties.Length; i++)
                {
                    var prop = logEvent.Properties[i];
                    if (!string.IsNullOrEmpty(logEvent.MessageTemplate) && logEvent.MessageTemplate.Contains("{" + prop.Key + "}"))
                        continue;

                    sb.Append(first ? " | " : ", ");
                    first = false;
                    sb.Append($"{prop.Key}={prop.Value}");
                }
            }

            if (_includeCallerInfo && !logEvent.Caller.IsEmpty)
                sb.Append($" @ {logEvent.Caller.MemberName}:{logEvent.Caller.LineNumber}");

            if (logEvent.Exception != null)
                sb.Append($"\n{logEvent.Exception}");

            if (!string.IsNullOrEmpty(logEvent.StackTrace))
                sb.Append("\n").Append(UnityConsoleStackFormatter.FilterStackTraceForDisplay(logEvent.StackTrace));

            return sb.ToString();
        }
    }
}
