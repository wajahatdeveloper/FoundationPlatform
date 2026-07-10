using UnityEditor;
using UnityEngine;

namespace HierarchyX {
    /// <summary>
    /// Highlights the GameObject under the mouse in the Hierarchy with an outline
    /// in the Scene View, so rows can be visually located in the scene at a glance.
    /// Repaints only when the hovered object changes.
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyXHoverHighlight {

        private static GameObject hovered;
        private static readonly GameObject[] drawBuffer = new GameObject[1];

        static HierarchyXHoverHighlight() {
            SceneView.duringSceneGui += OnSceneGui;
            EditorApplication.update += Tick;
        }

        /// <summary>Row pass: records the hovered GameObject (Repaint events carry a live mouse position).</summary>
        internal static void Track(Rect rect, GameObject go, HierarchyXSettings s) {
            if (!s.hoverHighlight || Event.current.type != EventType.Repaint)
                return;

            var full = rect;
            full.xMin = 0f;
            full.xMax = EditorGUIUtility.currentViewWidth;
            if (full.Contains(Event.current.mousePosition))
                Set(go);
        }

        private static void Tick() {
            if (hovered == null)
                return;
            // Clear once the mouse leaves the hierarchy window (or the object dies).
            var over = EditorWindow.mouseOverWindow;
            if (over == null || over.GetType().Name != "SceneHierarchyWindow")
                Set(null);
        }

        private static void Set(GameObject go) {
            if (ReferenceEquals(hovered, go))
                return;
            hovered = go;
            SceneView.RepaintAll();
        }

        private static void OnSceneGui(SceneView view) {
            if (hovered == null || Event.current.type != EventType.Repaint)
                return;
            var s = HierarchyXSettings.Instance;
            if (!s.enabled || !s.hoverHighlight)
                return;

            drawBuffer[0] = hovered;
            Handles.DrawOutline(drawBuffer, s.hoverHighlightColor);
        }
    }
}
