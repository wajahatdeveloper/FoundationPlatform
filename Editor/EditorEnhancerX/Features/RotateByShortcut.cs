using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Rotate the selection in 90° steps by shortcut: yaw around world up,
    /// pitch around the Scene View camera's dominant right axis. Honors
    /// Tools.pivotMode (Center = rotate around shared bounds center).
    /// </summary>
    [InitializeOnLoad]
    internal static class RotateByShortcut {

        static RotateByShortcut() {
            var scope = KeyScope.SceneView | KeyScope.Hierarchy;
            KeyRouter.Register("rotateLeft", () => EditorEnhancerXSettings.instance.rotateLeftKey, scope, () => Rotate(Vector3.up, -90f));
            KeyRouter.Register("rotateRight", () => EditorEnhancerXSettings.instance.rotateRightKey, scope, () => Rotate(Vector3.up, 90f));
            KeyRouter.Register("rotateUp", () => EditorEnhancerXSettings.instance.rotateUpKey, scope, () => Rotate(CameraRightAxis(), -90f));
            KeyRouter.Register("rotateDown", () => EditorEnhancerXSettings.instance.rotateDownKey, scope, () => Rotate(CameraRightAxis(), 90f));
        }

        // Snap the Scene View camera's right vector to the closest world axis so
        // pitch rotations stay axis-aligned regardless of camera orientation.
        private static Vector3 CameraRightAxis() {
            var view = SceneView.lastActiveSceneView;
            if (view == null)
                return Vector3.right;
            var right = view.rotation * Vector3.right;
            return Mathf.Abs(right.x) >= Mathf.Abs(right.z)
                ? new Vector3(Mathf.Sign(right.x), 0f, 0f)
                : new Vector3(0f, 0f, Mathf.Sign(right.z));
        }

        private static bool Rotate(Vector3 axis, float angle) {
            var transforms = Selection.transforms;
            if (transforms.Length == 0)
                return false;

            Undo.RecordObjects(transforms, "Rotate Selection");

            if (UnityEditor.Tools.pivotMode == PivotMode.Center && transforms.Length > 1) {
                var gameObjects = new GameObject[transforms.Length];
                for (var i = 0; i < transforms.Length; i++)
                    gameObjects[i] = transforms[i].gameObject;
                if (SelectionBoundsUtility.TryGetBounds(gameObjects, out var bounds)) {
                    foreach (var t in transforms)
                        t.RotateAround(bounds.center, axis, angle);
                    return true;
                }
            }

            foreach (var t in transforms)
                t.RotateAround(t.position, axis, angle);
            return true;
        }
    }
}
