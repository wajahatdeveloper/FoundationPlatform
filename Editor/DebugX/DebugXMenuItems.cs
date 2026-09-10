#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using AetherNexus.FoundationPlatform.DebugX;

namespace AetherNexus.FoundationPlatform.DebugX
{
    /// <summary>
    /// Editor menu items for DebugX
    /// </summary>
    public static class DebugXMenuItems
    {
        private const string OpenLogsFolderMenuPath = MenuPaths.Debug.OpenLogsFolder;
        private const string OpenPersistentDataFolderMenuPath = MenuPaths.Debug.OpenPersistentData;
        private const string CaptureFullStackTracesMenuPath = MenuPaths.Debug.CaptureFullStackTraces;
        private const string CaptureFullStackTracesPrefKey = DebugX.PrefKeyCaptureFullStackTraces;
        private const string SyncConsoleMenuPath = MenuPaths.Debug.SyncConsole;
        private const string SyncConsolePrefKey = DebugX.PrefKeySyncConsole;

        [MenuItem(OpenLogsFolderMenuPath, false, MenuPriorities.Debug)]
        [DesignerFeature(
            "Open Logs Folder",
            "Opens the folder holding this project's editor log files, for attaching one to a bug report.",
            "log logs folder open reveal file editor output crash report ndjson",
            DesignerFeatureKind.Debug,
            "")]
        public static void OpenLogsFolder()
        {
            string logPath = Application.dataPath + "/../Logs/Editor";

            // Ensure the directory exists
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            EditorUtility.RevealInFinder(logPath);
        }

        [MenuItem(OpenPersistentDataFolderMenuPath, false, MenuPriorities.Debug + 1)]
        [DesignerFeature(
            "Open Persistent Data Folder",
            "Opens the folder where the game writes saves and settings, so you can inspect or delete a save that is misbehaving.",
            "save saves persistent data folder open reveal delete wipe settings player prefs progress",
            DesignerFeatureKind.Debug,
            "")]
        public static void OpenPersistentDataFolder()
        {
            string persistentDataPath = Application.persistentDataPath;

            // Ensure the directory exists
            if (!Directory.Exists(persistentDataPath))
            {
                Directory.CreateDirectory(persistentDataPath);
            }

            EditorUtility.RevealInFinder(persistentDataPath);
        }

        [MenuItem(CaptureFullStackTracesMenuPath, false, MenuPriorities.Debug + 2)]
        [DesignerFeature(
            "Capture Full Stack Traces (toggle)",
            "Turns full stack traces on for log messages: more detail when chasing a bug, slower logging the rest of the time.",
            "stack trace full capture toggle log detail callsite performance verbose debug",
            DesignerFeatureKind.Debug,
            "")]
        public static void ToggleCaptureFullStackTraces()
        {
            bool next = !EditorPrefs.GetBool(CaptureFullStackTracesPrefKey, false);
            EditorPrefs.SetBool(CaptureFullStackTracesPrefKey, next);
            DebugX.CaptureFullStackTraces = next;
            Menu.SetChecked(CaptureFullStackTracesMenuPath, next);
        }

        [MenuItem(CaptureFullStackTracesMenuPath, true, MenuPriorities.Debug + 2)]
        public static bool ValidateCaptureFullStackTraces()
        {
            bool value = EditorPrefs.GetBool(CaptureFullStackTracesPrefKey, false);
            DebugX.CaptureFullStackTraces = value;
            Menu.SetChecked(CaptureFullStackTracesMenuPath, value);
            return true;
        }

        [MenuItem(SyncConsoleMenuPath, false, MenuPriorities.Debug + 3)]
        [DesignerFeature(
            "Sync Console (toggle)",
            "Mirrors DebugX stack traces into Unity's console so double-clicking a message jumps to the right line.",
            "console sync unity log stack trace double click jump line toggle mirror",
            DesignerFeatureKind.Debug,
            "")]
        public static void ToggleSyncConsoleForStackTraces()
        {
            bool next = !EditorPrefs.GetBool(SyncConsolePrefKey, false);
            EditorPrefs.SetBool(SyncConsolePrefKey, next);
            DebugX.SyncConsoleForStackTraces = next;
            Menu.SetChecked(SyncConsoleMenuPath, next);
        }

        [MenuItem(SyncConsoleMenuPath, true, MenuPriorities.Debug + 3)]
        public static bool ValidateSyncConsoleForStackTraces()
        {
            bool value = EditorPrefs.GetBool(SyncConsolePrefKey, false);
            DebugX.SyncConsoleForStackTraces = value;
            Menu.SetChecked(SyncConsoleMenuPath, value);
            return true;
        }
    }
}
#endif

