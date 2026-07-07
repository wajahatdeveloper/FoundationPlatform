#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using FoundationPlatform.DebugX;
using FoundationPlatform.Utilities.Menus;

namespace FoundationPlatform.DebugX
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

