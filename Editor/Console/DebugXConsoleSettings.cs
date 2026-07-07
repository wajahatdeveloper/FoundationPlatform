using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FoundationPlatform.DebugX.ConsoleView.Editor
{
    /// <summary>A saved filter tab (Console Pro "custom filter" equivalent): captures the full filter state.</summary>
    [Serializable]
    internal sealed class FilterTab
    {
        public string Name;
        public string Search;
        public bool UseRegex;
        public bool showLog = true;
        public bool showWarning = true;
        public bool showError = true;
        public List<string> excludedChannels = new List<string>();
    }

    /// <summary>
    /// Per-project console settings, serialized to ProjectSettings/DebugXConsole.json (checked in with
    /// the project, not per-machine). Single source of truth shared by the window and the Project
    /// Settings provider. Replaces Console Pro's .cpf files and the old per-user EditorPrefs.
    /// </summary>
    [Serializable]
    internal sealed class DebugXConsoleSettings
    {
        // Appearance
        public int fontSize = 12;
        public int rowHeight = 20;
        public int timeFormat = 1; // 0 None, 1 Clock, 2 Clock+ms, 3 Delta, 4 Frame
        public bool alternatingRows = true;
        public bool showHeader = true;
        public bool twoLineRows = false; // caller file:line shown under the message
        public int colTimeWidth = 78;
        public int colChannelWidth = 96;
        public int colCountWidth = 34;
        public int detailPaneHeight = 220; // split position of the detail pane, persisted across sessions

        // Behaviour
        public bool clearOnPlay = true;
        public bool clearOnBuild = false;
        public bool captureCompilerErrors = true;

        // Filters (persisted across sessions)
        public bool showLog = true;
        public bool showWarning = true;
        public bool showError = true;
        public bool showVerbose = true;       // Verbose/Debug levels within the Log category
        public bool showSourceDebugX = true;  // per-source visibility (D / U / C)
        public bool showSourceUnity = true;
        public bool showSourceCompiler = true;
        public bool searchInStack = false;    // search also matches stack traces / exception text
        public bool collapse = false;
        public List<string> excludedChannels = new List<string>();
        public List<string> ignore = new List<string>();
        public List<FilterTab> tabs = new List<FilterTab>();

        // View state (persisted across domain reloads)
        public int sortColumn = 0; // 0 = None
        public bool sortAsc = true;
        public string search = "";
        public bool searchRegex = false;
        public int activeTab = -1; // index into tabs, -1 = none

        private static readonly string Path_ =
            System.IO.Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings", "DebugXConsole.json");

        private static DebugXConsoleSettings _instance;
        public static DebugXConsoleSettings Instance => _instance ??= Load();

        private static DebugXConsoleSettings Load()
        {
            try
            {
                if (File.Exists(Path_))
                {
                    var s = JsonUtility.FromJson<DebugXConsoleSettings>(File.ReadAllText(Path_));
                    if (s != null)
                    {
                        s.excludedChannels ??= new List<string>();
                        s.ignore ??= new List<string>();
                        s.tabs ??= new List<FilterTab>();
                        return s;
                    }
                }
            }
            catch { /* corrupt: start fresh */ }
            return new DebugXConsoleSettings();
        }

        public void Save()
        {
            try { File.WriteAllText(Path_, JsonUtility.ToJson(this, true)); }
            catch { /* best effort */ }
        }

        public static void Reload() => _instance = Load();
    }
}
