using System.Collections.Generic;
using AetherNexus.FoundationPlatform.DebugX;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.DebugX.ConsoleView.Editor
{
    /// <summary>
    /// Project Settings page (Project/DebugX Console) for the per-project console settings. Edits the
    /// shared <see cref="DebugXConsoleSettings"/> and keeps the runtime store flags in sync.
    /// </summary>
    internal static class DebugXConsoleSettingsProvider
    {
        [InitializeOnLoadMethod]
        private static void SyncStoreFromSettings()
        {
            var s = DebugXConsoleSettings.Instance;
            ConsoleLogStore.ClearOnPlay = s.clearOnPlay;
            ConsoleLogStore.CaptureCompilerErrors = s.captureCompilerErrors;
        }

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new SettingsProvider("Project/DebugX Console", SettingsScope.Project)
            {
                label = "DebugX Console",
                keywords = new HashSet<string>(new[]
                {
                    "log", "console", "debug", "font", "row", "timestamp", "alternating", "compiler", "watch", "channel"
                }),
                guiHandler = _ => DrawGui()
            };
        }

        private static void DrawGui()
        {
            var s = DebugXConsoleSettings.Instance;
            EditorGUIUtility.labelWidth = 220f;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            s.fontSize = EditorGUILayout.IntSlider("Font Size", s.fontSize, 9, 22);
            s.rowHeight = EditorGUILayout.IntSlider("Row Height", s.rowHeight, 16, 40);
            s.timeFormat = (int)(ConsoleColorConfig.TimeFormat)EditorGUILayout.EnumPopup(
                "Timestamp Format", (ConsoleColorConfig.TimeFormat)Mathf.Clamp(s.timeFormat, 0, 4));
            s.alternatingRows = EditorGUILayout.Toggle("Alternating Row Colors", s.alternatingRows);
            s.twoLineRows = EditorGUILayout.Toggle("Two-Line Rows (caller under message)", s.twoLineRows);
            bool appearanceChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Behaviour", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            s.clearOnPlay = EditorGUILayout.Toggle("Clear On Play", s.clearOnPlay);
            s.clearOnBuild = EditorGUILayout.Toggle("Clear On Build", s.clearOnBuild);
            s.captureCompilerErrors = EditorGUILayout.Toggle("Capture Compiler / Import Errors", s.captureCompilerErrors);
            bool behaviourChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stack Traces & Verbosity", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Per-machine (EditorPrefs), shared with the Tools > GameEngineCore > DebugX menu. " +
                "Minimum Level applies live and is re-read on domain reload.", MessageType.None);

            // Pipeline floor. Default Debug keeps per-op Verbose traces (e.g. [GAS:TagTrace]) filtered —
            // and skips their stack-string capture via the ShouldEmit gate. Raise to Verbose to see them.
            var currentMin = (LogLevel)EditorPrefs.GetInt(DebugX.PrefKeyEditorMinLevel, (int)LogLevel.Debug);
            EditorGUI.BeginChangeCheck();
            var newMin = (LogLevel)EditorGUILayout.EnumPopup("Editor Minimum Log Level", currentMin);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetInt(DebugX.PrefKeyEditorMinLevel, (int)newMin);
                LogPipeline.SetMinimumLevel(newMin);
            }

            // On = an extra managed stack appended to EVERY log (not just Error/Fatal). Noisy.
            bool capture = EditorPrefs.GetBool(DebugX.PrefKeyCaptureFullStackTraces, false);
            EditorGUI.BeginChangeCheck();
            capture = EditorGUILayout.Toggle("Capture Full Stack Traces", capture);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(DebugX.PrefKeyCaptureFullStackTraces, capture);
                DebugX.CaptureFullStackTraces = capture;
            }

            // Run console sinks synchronously on the main thread so captured stacks are accurate.
            bool sync = EditorPrefs.GetBool(DebugX.PrefKeySyncConsole, false);
            EditorGUI.BeginChangeCheck();
            sync = EditorGUILayout.Toggle("Sync Console (Correct Stack Traces)", sync);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(DebugX.PrefKeySyncConsole, sync);
                DebugX.SyncConsoleForStackTraces = sync;
            }

            EditorGUILayout.Space();
            appearanceChanged |= DrawStringList("Ignored Terms", s.ignore);
            appearanceChanged |= DrawTabList(s);
            appearanceChanged |= DrawStringList("Hidden Channels", s.excludedChannels);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Appearance", GUILayout.Width(140)))
                {
                    s.fontSize = ConsoleColorConfig.DefaultFontSize;
                    s.rowHeight = ConsoleColorConfig.DefaultRowHeight;
                    s.timeFormat = (int)ConsoleColorConfig.TimeFormat.Clock;
                    s.alternatingRows = true;
                    s.twoLineRows = false;
                    s.colTimeWidth = 78;
                    s.colChannelWidth = 96;
                    s.colCountWidth = 34;
                    appearanceChanged = true;
                }
                if (GUILayout.Button("Clear Ignore List", GUILayout.Width(140)))
                {
                    s.ignore.Clear();
                    appearanceChanged = true;
                }
                if (GUILayout.Button("Clear Saved Tabs", GUILayout.Width(140)))
                {
                    s.tabs.Clear();
                    s.activeTab = -1;
                    appearanceChanged = true;
                }
            }

            if (GUILayout.Button("Open DebugX Console", GUILayout.Width(180)))
                DebugXConsoleWindow.Open();

            EditorGUILayout.Space();

            if (appearanceChanged || behaviourChanged)
            {
                s.Save();
                if (behaviourChanged)
                {
                    ConsoleLogStore.ClearOnPlay = s.clearOnPlay;
                    ConsoleLogStore.CaptureCompilerErrors = s.captureCompilerErrors;
                }
            }
        }

        /// <summary>Foldout list of strings with a per-item remove button. Returns true when modified.</summary>
        private static bool DrawStringList(string title, List<string> items)
        {
            bool changed = false;
            EditorGUILayout.LabelField($"{title} ({items.Count})", EditorStyles.boldLabel);
            if (items.Count == 0)
            {
                EditorGUILayout.LabelField("(none)", EditorStyles.miniLabel);
                return false;
            }

            int remove = -1;
            for (int i = 0; i < items.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(items[i], EditorStyles.miniLabel);
                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                        remove = i;
                }
            }
            if (remove >= 0)
            {
                items.RemoveAt(remove);
                changed = true;
            }
            EditorGUILayout.Space(4);
            return changed;
        }

        /// <summary>Saved filter tabs with rename fields and per-tab remove buttons. Returns true when modified.</summary>
        private static bool DrawTabList(DebugXConsoleSettings s)
        {
            bool changed = false;
            EditorGUILayout.LabelField($"Saved Filter Tabs ({s.tabs.Count})", EditorStyles.boldLabel);
            if (s.tabs.Count == 0)
            {
                EditorGUILayout.LabelField("(none)", EditorStyles.miniLabel);
                return false;
            }

            int remove = -1;
            for (int i = 0; i < s.tabs.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string newName = EditorGUILayout.TextField(s.tabs[i].Name);
                    if (newName != s.tabs[i].Name && !string.IsNullOrWhiteSpace(newName))
                    {
                        s.tabs[i].Name = newName;
                        changed = true;
                    }
                    EditorGUILayout.LabelField(
                        string.IsNullOrEmpty(s.tabs[i].Search) ? "(no search)" : s.tabs[i].Search,
                        EditorStyles.miniLabel, GUILayout.MaxWidth(220));
                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                        remove = i;
                }
            }
            if (remove >= 0)
            {
                s.tabs.RemoveAt(remove);
                if (s.activeTab == remove) s.activeTab = -1;
                else if (s.activeTab > remove) s.activeTab--;
                changed = true;
            }
            EditorGUILayout.Space(4);
            return changed;
        }
    }
}
