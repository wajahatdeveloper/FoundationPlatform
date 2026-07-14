using UnityEngine;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// World-space bounds of GameObjects for framing/floor-snapping/size display.
    /// Priority per object: Renderers (skipping trail/particle noise) → Colliders →
    /// RectTransform world corners → transform position.
    /// </summary>
    public static class SelectionBoundsUtility {

        private static readonly Vector3[] cornerBuffer = new Vector3[4];

        public static bool TryGetBounds(GameObject[] gameObjects, out Bounds bounds) {
            bounds = default;
            bool has = false;
            if (gameObjects == null) return false;
            foreach (var go in gameObjects) {
                if (go == null) continue;
                if (!TryGetBounds(go, out var b)) continue;
                if (!has) { bounds = b; has = true; }
                else bounds.Encapsulate(b);
            }
            return has;
        }

        public static bool TryGetBounds(GameObject go, out Bounds bounds) {
            bounds = default;
            if (go == null) return false;
            bool has = false;

            var renderers = go.GetComponentsInChildren<Renderer>(false);
            foreach (var r in renderers) {
                if (r == null || r is TrailRenderer || r is ParticleSystemRenderer) continue;
                Accumulate(ref bounds, ref has, r.bounds);
            }
            if (has) return true;

            var colliders = go.GetComponentsInChildren<Collider>(false);
            foreach (var c in colliders) {
                if (c == null) continue;
                Accumulate(ref bounds, ref has, c.bounds);
            }
            if (has) return true;

            var rects = go.GetComponentsInChildren<RectTransform>(false);
            foreach (var rt in rects) {
                if (rt == null) continue;
                rt.GetWorldCorners(cornerBuffer);
                for (int i = 0; i < 4; i++)
                    Accumulate(ref bounds, ref has, new Bounds(cornerBuffer[i], Vector3.zero));
            }
            if (has) return true;

            bounds = new Bounds(go.transform.position, Vector3.zero);
            return true;
        }

        private static void Accumulate(ref Bounds bounds, ref bool has, Bounds add) {
            if (!has) { bounds = add; has = true; }
            else bounds.Encapsulate(add);
        }
    }
}
