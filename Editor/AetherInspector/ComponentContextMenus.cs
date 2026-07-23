#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.AetherInspector.Editor
{
    /// <summary>
    /// Extra CONTEXT/Component menu items: move-to-top/bottom, JSON copy/paste of
    /// component values (EditorJsonUtility), JSON file export/import, and the
    /// play-mode value-saver toggle.
    /// </summary>
    internal static class ComponentContextMenus
    {
        // ---- Reorder ----------------------------------------------------------

        [MenuItem(MenuPaths.ContextComponent.MoveToTop, false, 510)]
        private static void MoveToTop(MenuCommand cmd)
        {
            var component = (Component)cmd.context;
            while (ComponentUtility.MoveComponentUp(component)) { }
        }

        [MenuItem(MenuPaths.ContextComponent.MoveToTop, true)]
        private static bool MoveToTopValidate(MenuCommand cmd) => CanReorder(cmd);

        [MenuItem(MenuPaths.ContextComponent.MoveToBottom, false, 511)]
        private static void MoveToBottom(MenuCommand cmd)
        {
            var component = (Component)cmd.context;
            while (ComponentUtility.MoveComponentDown(component)) { }
        }

        [MenuItem(MenuPaths.ContextComponent.MoveToBottom, true)]
        private static bool MoveToBottomValidate(MenuCommand cmd) => CanReorder(cmd);

        private static bool CanReorder(MenuCommand cmd)
            => cmd.context is Component component && !(component is Transform);

        // ---- JSON copy/paste --------------------------------------------------

        [MenuItem(MenuPaths.ContextComponent.CopyValuesAsJson, false, 520)]
        private static void CopyJson(MenuCommand cmd)
        {
            EditorGUIUtility.systemCopyBuffer = EditorJsonUtility.ToJson(cmd.context, true);
        }

        [MenuItem(MenuPaths.ContextComponent.PasteValuesFromJson, false, 521)]
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
                Debug.LogError($"[AetherInspector] JSON paste failed: {e.Message}");
            }
        }

        [MenuItem(MenuPaths.ContextComponent.PasteValuesFromJson, true)]
        private static bool PasteJsonValidate(MenuCommand cmd)
        {
            var json = EditorGUIUtility.systemCopyBuffer;
            return !string.IsNullOrEmpty(json) && json.TrimStart().StartsWith("{");
        }

        [MenuItem(MenuPaths.ContextComponent.SaveValuesToJsonFile, false, 522)]
        private static void SaveJsonFile(MenuCommand cmd)
        {
            var component = cmd.context;
            var path = EditorUtility.SaveFilePanel("Save Component JSON", "", component.GetType().Name, "json");
            if (!string.IsNullOrEmpty(path))
                System.IO.File.WriteAllText(path, EditorJsonUtility.ToJson(component, true));
        }

        [MenuItem(MenuPaths.ContextComponent.LoadValuesFromJsonFile, false, 523)]
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
                Debug.LogError($"[AetherInspector] JSON load failed: {e.Message}");
            }
        }

        // ---- Play-mode value saver ---------------------------------------------

        [MenuItem(MenuPaths.ContextComponent.SaveValuesWhenExitingPlayMode, false, 530)]
        private static void ToggleSaveOnExit(MenuCommand cmd)
        {
            PlayModeValuesSaver.Toggle((Component)cmd.context);
        }

        [MenuItem(MenuPaths.ContextComponent.SaveValuesWhenExitingPlayMode, true)]
        private static bool ToggleSaveOnExitValidate(MenuCommand cmd)
        {
            if (!InspectorXSettings.instance.saveComponentValuesInPlayMode)
                return false;
            if (!(cmd.context is Component component) || EditorUtility.IsPersistent(component))
                return false;
            Menu.SetChecked(MenuPaths.ContextComponent.SaveValuesWhenExitingPlayMode, PlayModeValuesSaver.IsWatched(component));
            return true;
        }
    }
}
#endif
