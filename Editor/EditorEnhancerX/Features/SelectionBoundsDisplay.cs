using UnityEditor;
using UnityEngine;

namespace EditorEnhancerX {
    /// <summary>
    /// Draws the selection's world bounds as a wire cube with per-axis size labels.
    /// Bounds recompute only when the selection or its transforms change.
    /// </summary>
    [InitializeOnLoad]
    internal static class SelectionBoundsDisplay {

        private static Bounds bounds;
        private static bool hasBounds;
        private static bool dirty = true;
        private static int transformsHash;

        static SelectionBoundsDisplay() {
            Selection.selectionChanged += () => dirty = true;
            Undo.undoRedoPerformed += () => dirty = true;
            SceneViewHub.Register("selectionBounds", 20, Pass);
        }

        private static void Pass(SceneView view) {
            if (!EditorEnhancerXSettings.instance.selectionBoundsEnabled)
                return;
            if (Event.current.type != EventType.Repaint)
                return;

            var gameObjects = Selection.gameObjects;
            if (gameObjects.Length == 0) {
                hasBounds = false;
                return;
            }

            var hash = ComputeTransformsHash(gameObjects);
            if (dirty || hash != transformsHash) {
                dirty = false;
                transformsHash = hash;
                hasBounds = SelectionBoundsUtility.TryGetBounds(gameObjects, out bounds);
            }
            if (!hasBounds)
                return;

            Handles.color = new Color(1f, 0.7f, 0.1f, 0.9f);
            Handles.DrawWireCube(bounds.center, bounds.size);

            var c = bounds.center;
            var ext = bounds.extents;
            Handles.Label(new Vector3(c.x, bounds.min.y, bounds.max.z), $"X: {bounds.size.x:0.###}");
            Handles.Label(new Vector3(bounds.max.x, c.y, bounds.max.z), $"Y: {bounds.size.y:0.###}");
            Handles.Label(new Vector3(bounds.max.x, bounds.min.y, c.z), $"Z: {bounds.size.z:0.###}");
            Handles.color = Color.white;
        }

        private static int ComputeTransformsHash(GameObject[] gameObjects) {
            var hash = 17;
            foreach (var go in gameObjects) {
                if (go == null)
                    continue;
                var t = go.transform;
                hash = hash * 31 + t.position.GetHashCode();
                hash = hash * 31 + t.rotation.GetHashCode();
                hash = hash * 31 + t.lossyScale.GetHashCode();
            }
            return hash;
        }
    }
}
