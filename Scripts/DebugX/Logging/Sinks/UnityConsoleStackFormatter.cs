using System;
using System.Collections.Generic;
using System.Text;

namespace DebugXLogging
{
    /// <summary>
    /// Formats call site / stack for Unity console clickable links: (at Assets/Path/File.cs:line)
    /// </summary>
    public static class UnityConsoleStackFormatter
    {
        private const string AssetsPrefix = "Assets/";
        private const string PackagesPrefix = "Packages/";
        private const string InPathMarker = " in ";

        /// <summary>
        /// Converts a full file path to Unity project-relative path (Assets/... or Packages/...).
        /// Returns null if path is null, empty, or not under Assets or Packages.
        /// </summary>
        public static string ToUnityProjectPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return null;

            string normalized = fullPath.Replace('\\', '/');
            int assetsIndex = normalized.IndexOf(AssetsPrefix);
            int packagesIndex = normalized.IndexOf(PackagesPrefix);

            int start = -1;
            if (assetsIndex >= 0 && (packagesIndex < 0 || assetsIndex <= packagesIndex))
                start = assetsIndex;
            else if (packagesIndex >= 0)
                start = packagesIndex;

            if (start < 0)
                return null;

            return normalized.Substring(start);
        }

        /// <summary>
        /// Returns a single line in Unity's clickable format: (at path:lineNumber)
        /// </summary>
        public static string FormatSingleLine(string projectPath, int lineNumber)
        {
            if (string.IsNullOrEmpty(projectPath) || lineNumber <= 0)
                return "";
            return $"(at {projectPath}:{lineNumber})";
        }

        /// <summary>
        /// Appends Unity clickable stack line(s) for the log event. Returns "\n(at Assets/...:N)" or "".
        /// Safe: returns "" on any exception.
        /// </summary>
        public static string FormatUnityClickableStack(LogEvent logEvent)
        {
            try
            {
                if (string.IsNullOrEmpty(logEvent.Caller.FilePath) || logEvent.Caller.LineNumber <= 0)
                    return "";

                string projectPath = ToUnityProjectPath(logEvent.Caller.FilePath);
                if (projectPath == null)
                    return "";

                string line = FormatSingleLine(projectPath, logEvent.Caller.LineNumber);
                return string.IsNullOrEmpty(line) ? "" : "\n" + line;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Filters DebugX/async noise and trims absolute paths to Assets/... or Packages/... form.
        /// </summary>
        public static string FilterStackTraceForDisplay(string rawTrace)
        {
            if (string.IsNullOrEmpty(rawTrace))
                return rawTrace;

            var lines = rawTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var filteredLines = new List<string>(lines.Length);
            var foundCaller = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (ShouldSkipStackTraceLine(trimmed, ref foundCaller))
                    continue;

                filteredLines.Add(TrimStackTraceLinePath(line));
            }

            return filteredLines.Count == 0 ? "" : string.Join("\n", filteredLines);
        }

        /// <summary>
        /// Replaces absolute file paths in a stack trace with Unity project-relative paths.
        /// </summary>
        public static string TrimStackTracePaths(string rawTrace)
        {
            if (string.IsNullOrEmpty(rawTrace))
                return rawTrace;

            var lines = rawTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder(rawTrace.Length);

            for (var i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                    sb.Append('\n');

                sb.Append(TrimStackTraceLinePath(lines[i]));
            }

            return sb.ToString();
        }

        private static string TrimStackTraceLinePath(string line)
        {
            var inIndex = line.LastIndexOf(InPathMarker, StringComparison.Ordinal);
            if (inIndex < 0)
                return line;

            var pathStart = inIndex + InPathMarker.Length;
            var colonIndex = line.LastIndexOf(':');
            if (colonIndex <= pathStart)
                return line;

            var fullPath = line.Substring(pathStart, colonIndex - pathStart);
            var projectPath = ToUnityProjectPath(fullPath);
            if (projectPath == null)
                return line;

            return line.Substring(0, pathStart) + projectPath + line.Substring(colonIndex);
        }

        private static bool ShouldSkipStackTraceLine(string trimmed, ref bool foundCaller)
        {
            if (!foundCaller)
            {
                if (trimmed.StartsWith("at DebugXLogging.", StringComparison.Ordinal) ||
                    trimmed.StartsWith("DebugXLogging.", StringComparison.Ordinal) ||
                    trimmed.StartsWith("at DebugX.", StringComparison.Ordinal) ||
                    trimmed.StartsWith("DebugX.", StringComparison.Ordinal) ||
                    trimmed.StartsWith("at DebugXBuilder.", StringComparison.Ordinal) ||
                    trimmed.StartsWith("DebugXBuilder.", StringComparison.Ordinal))
                {
                    return true;
                }

                foundCaller = true;
            }

            if (trimmed.Contains("Cysharp.Threading.Tasks", StringComparison.Ordinal))
                return true;

            if (trimmed.Contains("UnityEngine.SetupCoroutine", StringComparison.Ordinal))
                return true;

            if (trimmed.Contains("<>d__", StringComparison.Ordinal) && trimmed.Contains("MoveNext", StringComparison.Ordinal))
                return true;

            return false;
        }
    }
}
