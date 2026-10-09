using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Switch between Scene and Game views (optionally auto-switching to Game on play).
    /// Maximize is Unity's own Shift+Space.
    /// </summary>
    [InitializeOnLoad]
    internal static class WindowShortcuts {

        static WindowShortcuts() {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        // Unbound by default; bind in Edit ▸ Shortcuts.
        [Shortcut("EditorEnhancerX/Switch Scene and Game View")]
        private static void SwitchViewShortcut() {
            if (EditorEnhancerXSettings.Active) SwitchView();
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
