using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Drops the selected objects onto the surface below them: raycast down from the
    /// bounds base (ignoring the selection's own colliders), translate so the bounds
    /// bottom rests on the hit point. Optional fallback to the y=0 plane.
    /// </summary>
    [InitializeOnLoad]
    internal static class DropToFloor {

        private static readonly RaycastHit[] hitBuffer = new RaycastHit[32];

        static DropToFloor() {
            KeyRouter.Register("dropToFloor",
                () => EditorEnhancerXSettings.instance.dropToFloorKey,
                KeyScope.SceneView | KeyScope.Hierarchy,
                Execute);
        }

        [MenuItem(MenuPaths.EditorEnhancer.DropToFloor, false, 2)]
        private static void Menu() => Execute();

        [MenuItem(MenuPaths.EditorEnhancer.DropToFloor, true)]
        private static bool MenuValidate() => Selection.transforms.Length > 0;

        private static bool Execute() {
            var transforms = Selection.transforms;
            if (transforms.Length == 0)
                return false;

            var s = EditorEnhancerXSettings.instance.dropToFloor;
            var dropped = false;

            foreach (var t in transforms) {
                if (!SelectionBoundsUtility.TryGetBounds(t.gameObject, out var bounds))
                    continue;

                var origin = new Vector3(bounds.center.x, bounds.min.y + 0.001f, bounds.center.z);
                var floorY = float.NegativeInfinity;

                var count = Physics.RaycastNonAlloc(origin, Vector3.down, hitBuffer, float.PositiveInfinity);
                for (var i = 0; i < count; i++) {
                    var hit = hitBuffer[i];
                    if (hit.collider == null || hit.collider.transform.IsChildOf(t))
                        continue;
                    if (hit.point.y > floorY)
                        floorY = hit.point.y;
                }

                if (float.IsNegativeInfinity(floorY)) {
                    if (!s.fallbackToZeroPlane)
                        continue;
                    floorY = 0f;
                }

                var delta = bounds.min.y - floorY;
                if (Mathf.Approximately(delta, 0f))
                    continue;

                Undo.RecordObject(t, "Drop To Floor");
                t.position += Vector3.down * delta;
                dropped = true;
            }
            return dropped;
        }
    }
}
