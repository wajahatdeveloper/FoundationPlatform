using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Window shortcuts: maximize the active window, and switch between
    /// Scene and Game views (optionally auto-switching to Game on play).
    /// </summary>
    [InitializeOnLoad]
    internal static class WindowShortcuts {

        static WindowShortcuts() {
            KeyRouter.Register("maximize",
                () => EditorEnhancerXSettings.instance.maximizeKey,
                KeyScope.SceneView | KeyScope.Hierarchy | KeyScope.Global,
                Maximize);
            KeyRouter.Register("switchView",
                () => EditorEnhancerXSettings.instance.switchViewKey,
                KeyScope.SceneView | KeyScope.Global,
                SwitchView);
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change) {
            var s = EditorEnhancerXSettings.instance;
            if (!s.masterEnabled || !s.viewSwitcher.switchToGameViewOnPlay)
                return;
            if (change == PlayModeStateChange.EnteredPlayMode)
                EditorApplication.ExecuteMenuItem("Window/General/Game");
            else if (change == PlayModeStateChange.EnteredEditMode)
                SceneView.lastActiveSceneView?.Focus();
        }

        private static bool Maximize() {
            var window = EditorWindow.focusedWindow ?? EditorWindow.mouseOverWindow;
            if (window == null)
                return false;
            try {
                window.maximized = !window.maximized;
                return true;
            } catch {
                return false; // floating windows can't maximize
            }
        }

        private static bool SwitchView() {
            if (EditorWindow.focusedWindow is SceneView)
                return EditorApplication.ExecuteMenuItem("Window/General/Game");

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null) {
                sceneView.Focus();
                return true;
            }
            return EditorApplication.ExecuteMenuItem("Window/General/Scene");
        }
    }
}
