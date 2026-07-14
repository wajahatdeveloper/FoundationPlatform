using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Scene View navigation shortcuts: fast zoom in/out (halve/double the view size)
    /// and frame-selected-true-bounds (renderers/colliders/RectTransforms).
    /// </summary>
    [InitializeOnLoad]
    internal static class SceneNavigation {

        static SceneNavigation() {
            KeyRouter.Register("zoomIn", () => EditorEnhancerXSettings.instance.zoomInKey, KeyScope.SceneView, () => Zoom(0.5f));
            KeyRouter.Register("zoomOut", () => EditorEnhancerXSettings.instance.zoomOutKey, KeyScope.SceneView, () => Zoom(2f));
            KeyRouter.Register("frameBounds",
                () => EditorEnhancerXSettings.instance.frameBoundsKey,
                KeyScope.SceneView | KeyScope.Hierarchy,
                FrameSelectedBounds);
        }

        private static bool Zoom(float factor) {
            var view = SceneView.lastActiveSceneView;
            if (view == null)
                return false;
            view.LookAt(view.pivot, view.rotation, view.size * factor);
            return true;
        }

        private static bool FrameSelectedBounds() {
            var view = SceneView.lastActiveSceneView;
            if (view == null)
                return false;
            if (!SelectionBoundsUtility.TryGetBounds(Selection.gameObjects, out var bounds))
                return false;
            view.Frame(bounds, false);
            return true;
        }
    }
}
