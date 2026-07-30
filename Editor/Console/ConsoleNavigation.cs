using System.Text.RegularExpressions;
using AetherNexus.FoundationPlatform.DebugX;
using AetherNexus.FoundationPlatform.DebugX.ConsoleView;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.DebugX.ConsoleView.Editor
{
    /// <summary>
    /// Resolves and opens source locations from console rows and stack frames. Handles both Unity's
    /// clickable format "(at Assets/Foo.cs:12)" and System.Diagnostics' "in C:\...\Foo.cs:line 12".
    /// Frames that carry no file info (shown as "&lt;GUID&gt;:0") are not navigable — there is nothing
    /// to open — so only frames with a real .cs path become links.
    /// </summary>
    internal static class ConsoleNavigation
    {
        // Accepts  Foo.cs:12  and  Foo.cs:line 12
        private static readonly Regex StackFrame =
            new Regex(@"([^\s()]+\.cs):(?:line\s+)?(\d+)", RegexOptions.Compiled);

        private const string DebugXInternalMarker = "/debugx/";

        public static bool IsDebugXInternal(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return path.Replace('\\', '/').ToLowerInvariant().Contains(DebugXInternalMarker);
        }

        /// <summary>
        /// Best source location for an entry: the caller file:line if it points at user code, otherwise
        /// the first stack frame that carries file info and is not inside the DebugX logging internals.
        /// </summary>
        public static bool TryBestSource(ConsoleEntry entry, out string path, out int line)
        {
            path = null;
            line = 0;
            if (entry == null) return false;

            if (!string.IsNullOrEmpty(entry.CallerFilePath) && entry.CallerLineNumber > 0 &&
                !IsDebugXInternal(entry.CallerFilePath))
            {
                path = entry.CallerFilePath;
                line = entry.CallerLineNumber;
                return true;
            }

            string source = !string.IsNullOrEmpty(entry.RawStackTrace) ? entry.RawStackTrace : entry.Message;
            foreach (Match m in StackFrame.Matches(source ?? ""))
            {
                string p = m.Groups[1].Value;
                if (IsDebugXInternal(p)) continue;
                if (int.TryParse(m.Groups[2].Value, out int ln))
                {
                    path = p;
                    line = ln;
                    return true;
                }
            }
            return false;
        }

        public static bool OpenEntry(ConsoleEntry entry)
        {
            return TryBestSource(entry, out string path, out int line) && OpenPath(path, line);
        }

        /// <summary>
        /// Resolves a source path to the imported asset behind it. Fails for files that live outside
        /// Assets/ and Packages/ (generated sources, package caches) — those have no asset to reveal.
        /// </summary>
        public static bool TryResolveAsset(string filePath, out Object asset, out string projectPath)
        {
            asset = null;
            projectPath = null;
            if (string.IsNullOrEmpty(filePath)) return false;

            projectPath = UnityConsoleStackFormatter.ToUnityProjectPath(filePath);
            if (string.IsNullOrEmpty(projectPath)) return false;

            asset = AssetDatabase.LoadAssetAtPath<Object>(projectPath);
            return asset != null;
        }

        /// <summary>
        /// Selects and pings the entry's source file in the Project window. Reveal-only: it never
        /// opens an editor and never steals keyboard focus from the console.
        /// </summary>
        public static bool PingEntry(ConsoleEntry entry) => PingEntry(entry, out _);

        public static bool PingEntry(ConsoleEntry entry, out string projectPath)
        {
            projectPath = null;
            if (!TryBestSource(entry, out string path, out _)) return false;
            if (!TryResolveAsset(path, out Object asset, out projectPath)) return false;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            return true;
        }

        public static bool TryParseFirstFrame(string text, out string path, out int line)
        {
            path = null;
            line = 0;
            if (string.IsNullOrEmpty(text)) return false;

            var m = StackFrame.Match(text);
            if (!m.Success) return false;

            path = m.Groups[1].Value;
            return int.TryParse(m.Groups[2].Value, out line);
        }

        public static bool OpenPath(string filePath, int line)
        {
            if (string.IsNullOrEmpty(filePath)) return false;

            if (TryResolveAsset(filePath, out Object script, out _))
                return AssetDatabase.OpenAsset(script, Mathf.Max(1, line));

            return UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(filePath, Mathf.Max(1, line));
        }
    }
}
