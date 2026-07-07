using System.Text;
using FoundationPlatform.DebugX;
using FoundationPlatform.DebugX.ConsoleView;

namespace FoundationPlatform.DebugX.ConsoleView.Editor
{
    /// <summary>
    /// Builds the display-only strings for an entry (property text, filtered stack) lazily on the main
    /// thread and caches them back onto the entry. Kept off the logging threads so nothing touches
    /// UnityEngine.Object members off-thread.
    /// </summary>
    internal static class ConsoleFormat
    {
        public static void EnsureDerived(ConsoleEntry e)
        {
            if (e == null || e.DerivedBuilt) return;
            e.PropertiesText = BuildProperties(e);
            e.DisplayStack = BuildStack(e);
            e.DerivedBuilt = true;
        }

        /// <summary>
        /// One-line caller summary ("Assets/Foo.cs:42  Method()") for the two-line row mode. Prefers the
        /// captured caller info, falls back to the first navigable stack frame, then the source context.
        /// Cached on the entry (may legitimately be empty).
        /// </summary>
        public static string EnsureCallerSummary(ConsoleEntry e)
        {
            if (e == null) return "";
            if (e.CallerSummaryBuilt) return e.CallerSummary;
            e.CallerSummaryBuilt = true;

            string path = null;
            int line = 0;
            if (ConsoleNavigation.TryBestSource(e, out string srcPath, out int srcLine))
            {
                path = UnityConsoleStackFormatter.ToUnityProjectPath(srcPath) ?? srcPath;
                line = srcLine;
            }

            if (path != null)
            {
                e.CallerSummary = string.IsNullOrEmpty(e.CallerMember)
                    ? $"{path}:{line}"
                    : $"{path}:{line}  {e.CallerMember}()";
            }
            else
            {
                e.CallerSummary = e.SourceContext ?? "";
            }
            return e.CallerSummary;
        }

        public static string BuildProperties(ConsoleEntry e)
        {
            if (e.Properties == null || e.Properties.Length == 0) return "";

            var sb = new StringBuilder();
            bool first = true;
            for (int i = 0; i < e.Properties.Length; i++)
            {
                var p = e.Properties[i];
                if (string.IsNullOrEmpty(p.Key) || p.Key.StartsWith("__")) continue;
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(p.Key).Append('=').Append(p.Value != null ? p.Value.ToString() : "null");
            }
            return sb.ToString();
        }

        private static string BuildStack(ConsoleEntry e)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(e.ExceptionText))
                sb.Append(e.ExceptionText);

            if (!string.IsNullOrEmpty(e.RawStackTrace))
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(UnityConsoleStackFormatter.FilterStackTraceForDisplay(e.RawStackTrace));
            }
            return sb.ToString();
        }
    }
}
