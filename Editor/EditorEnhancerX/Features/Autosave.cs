using UnityEditor;
using UnityEditor.SceneManagement;

namespace EditorEnhancerX {
    /// <summary>
    /// Saves dirty, previously-saved scenes on an interval and/or when entering play mode.
    /// Untitled scenes are never touched (no dialogs). Skips play mode, compiles and builds.
    /// </summary>
    [InitializeOnLoad]
    internal static class Autosave {

        private static double lastSaveTime;

        static Autosave() {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            lastSaveTime = EditorApplication.timeSinceStartup;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change) {
            var s = EditorEnhancerXSettings.instance;
            if (!s.masterEnabled || !s.autosave.enabled || !s.autosave.saveOnPlay)
                return;
            if (change == PlayModeStateChange.ExitingEditMode)
                SaveAll(s);
        }

        private static void Update() {
            var s = EditorEnhancerXSettings.instance;
            if (!s.masterEnabled || !s.autosave.enabled || !s.autosave.intervalEnabled)
                return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer)
                return;

            var interval = UnityEngine.Mathf.Max(1, s.autosave.intervalMinutes) * 60.0;
            if (EditorApplication.timeSinceStartup - lastSaveTime < interval)
                return;

            SaveAll(s);
        }

        private static void SaveAll(EditorEnhancerXSettings s) {
            lastSaveTime = EditorApplication.timeSinceStartup;

            var savedAny = false;
            for (var i = 0; i < EditorSceneManager.sceneCount; i++) {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isDirty || string.IsNullOrEmpty(scene.path))
                    continue;
                if (EditorSceneManager.SaveScene(scene))
                    savedAny = true;
            }

            if (s.autosave.saveAssets)
                AssetDatabase.SaveAssets();

            if (savedAny)
                UnityEngine.Debug.Log("[EditorEnhancerX] Autosaved open scenes.");
        }
    }
}
