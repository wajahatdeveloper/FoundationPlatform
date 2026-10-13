#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using AetherNexus.FoundationPlatform.Logging;

namespace AetherNexus.FoundationPlatform.Logging
{
    /// <summary>
    /// Editor menu items for DebugX
    /// </summary>
    public static class DebugXMenuItems
    {
        [MenuItem(MenuPaths.Utilities.OpenLogsFolder, false, MenuPriorities.Utilities + 5)]
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

        [MenuItem(MenuPaths.Utilities.OpenPersistentData, false, MenuPriorities.Utilities + 6)]
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
    }
}
#endif

