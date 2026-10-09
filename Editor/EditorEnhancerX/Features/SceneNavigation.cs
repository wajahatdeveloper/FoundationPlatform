using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Scene View navigation shortcuts: fast zoom in/out (halve/double the view size)
    /// and frame-selected-true-bounds (renderers/colliders/RectTransforms). Unbound by
    /// default; bind them in Edit ▸ Shortcuts.
    /// </summary>
    internal static class SceneNavigation {

        [Shortcut("EditorEnhancerX/Fast Zoom In", typeof(SceneView))]
        private static void ZoomIn() => Zoom(0.5f);

        [Shortcut("EditorEnhancerX/Fast Zoom Out", typeof(SceneView))]
        private static void ZoomOut() => Zoom(2f);

        [Shortcut("EditorEnhancerX/Frame Selected Bounds")]
        private static void FrameSelectedBounds() {
            if (!EditorEnhancerXSettings.Active)
                return;
            var view = SceneView.lastActiveSceneView;
            if (view == null)
                return;
            if (!SelectionBoundsUtility.TryGetBounds(Selection.gameObjects, out var bounds))
                return;
            view.Frame(bounds, false);
        }

        private static void Zoom(float factor) {
            if (!EditorEnhancerXSettings.Active)
                return;
            var view = SceneView.lastActiveSceneView;
            if (view == null)
                return;
            view.LookAt(view.pivot, view.rotation, view.size * factor);
        }
    }
}
