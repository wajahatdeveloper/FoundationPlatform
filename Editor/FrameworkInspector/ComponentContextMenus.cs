#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FoundationPlatform.FrameworkInspector.Editor
{
    /// <summary>
    /// Extra CONTEXT/Component menu items: move-to-top/bottom, JSON copy/paste of
    /// component values (EditorJsonUtility), JSON file export/import, and the
    /// play-mode value-saver toggle.
    /// </summary>
    internal static class ComponentContextMenus
    {
        // ---- Reorder ----------------------------------------------------------

        [MenuItem("CONTEXT/Component/Move To Top", false, 510)]
        private static void MoveToTop(MenuCommand cmd)
        {
            var component = (Component)cmd.context;
            while (ComponentUtility.MoveComponentUp(component)) { }
        }

        [MenuItem("CONTEXT/Component/Move To Top", true)]
        private static bool MoveToTopValidate(MenuCommand cmd) => CanReorder(cmd);

        [MenuItem("CONTEXT/Component/Move To Bottom", false, 511)]
        private static void MoveToBottom(MenuCommand cmd)
        {
            var component = (Component)cmd.context;
            while (ComponentUtility.MoveComponentDown(component)) { }
        }

        [MenuItem("CONTEXT/Component/Move To Bottom", true)]
        private static bool MoveToBottomValidate(MenuCommand cmd) => CanReorder(cmd);

        private static bool CanReorder(MenuCommand cmd)
            => cmd.context is Component component && !(component is Transform);

        // ---- JSON copy/paste --------------------------------------------------

        [MenuItem("CONTEXT/Component/Copy Values As JSON", false, 520)]
        private static void CopyJson(MenuCommand cmd)
        {
            EditorGUIUtility.systemCopyBuffer = EditorJsonUtility.ToJson(cmd.context, true);
        }

        [MenuItem("CONTEXT/Component/Paste Values From JSON", false, 521)]
        private static void PasteJson(MenuCommand cmd)
        {
            var json = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(json))
                return;
            Undo.RecordObject(cmd.context, "Paste Component Values From JSON");
            try
            {
                EditorJsonUtility.FromJsonOverwrite(json, cmd.context);
                EditorUtility.SetDirty(cmd.context);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FrameworkInspector] JSON paste failed: {e.Message}");
            }
        }

        [MenuItem("CONTEXT/Component/Paste Values From JSON", true)]
        private static bool PasteJsonValidate(MenuCommand cmd)
        {
            var json = EditorGUIUtility.systemCopyBuffer;
            return !string.IsNullOrEmpty(json) && json.TrimStart().StartsWith("{");
        }

        [MenuItem("CONTEXT/Component/Save Values To JSON File...", false, 522)]
        private static void SaveJsonFile(MenuCommand cmd)
        {
            var component = cmd.context;
            var path = EditorUtility.SaveFilePanel("Save Component JSON", "", component.GetType().Name, "json");
            if (!string.IsNullOrEmpty(path))
                System.IO.File.WriteAllText(path, EditorJsonUtility.ToJson(component, true));
        }

        [MenuItem("CONTEXT/Component/Load Values From JSON File...", false, 523)]
        private static void LoadJsonFile(MenuCommand cmd)
        {
            var path = EditorUtility.OpenFilePanel("Load Component JSON", "", "json");
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                return;
            Undo.RecordObject(cmd.context, "Load Component Values From JSON");
            try
            {
                EditorJsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(path), cmd.context);
                EditorUtility.SetDirty(cmd.context);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FrameworkInspector] JSON load failed: {e.Message}");
            }
        }

        // ---- Play-mode value saver ---------------------------------------------

        private const string SaveOnExitMenu = "CONTEXT/Component/Save Values When Exiting Play Mode";

        [MenuItem(SaveOnExitMenu, false, 530)]
        private static void ToggleSaveOnExit(MenuCommand cmd)
        {
            PlayModeValuesSaver.Toggle((Component)cmd.context);
        }

        [MenuItem(SaveOnExitMenu, true)]
        private static bool ToggleSaveOnExitValidate(MenuCommand cmd)
        {
            if (!InspectorXSettings.instance.saveComponentValuesInPlayMode)
                return false;
            if (!(cmd.context is Component component) || EditorUtility.IsPersistent(component))
                return false;
            Menu.SetChecked(SaveOnExitMenu, PlayModeValuesSaver.IsWatched(component));
            return true;
        }
    }
}
#endif
