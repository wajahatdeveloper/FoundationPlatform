using UnityEditor;
using UnityEngine;

namespace HierarchyX {
    /// <summary>
    /// Hover-only row controls on the right edge: active checkbox, visibility eye and
    /// pickability toggle. Left-click toggles the object (with descendants for vis/pick);
    /// right-click on the eye isolates (solo visibility), right-click on the pick icon
    /// solos pickability. Drawn as an overlay so row layout never shifts.
    /// Uses only the public SceneVisibilityManager API.
    /// </summary>
    internal static class HierarchyXRowControls {

        private const float Size = 16f;
        private const float Spacing = 2f;

        private static GameObject visibilityIsolated;
        private static GameObject pickingSoloed;

        internal static void Draw(Rect rect, GameObject go, HierarchyXSettings s) {
            if (!s.rowActiveToggle && !s.soloButtons)
                return;

            var e = Event.current;
            var full = rect;
            full.xMin = 0f;
            full.xMax = EditorGUIUtility.currentViewWidth;
            if (!full.Contains(e.mousePosition))
                return;

            var x = EditorGUIUtility.currentViewWidth - s.rightMargin - Size;

            if (s.rowActiveToggle) {
                var r = IconRect(rect, x);
                var active = go.activeSelf;
                EditorGUI.BeginChangeCheck();
                var next = GUI.Toggle(r, active, new GUIContent(string.Empty, active ? "Deactivate" : "Activate"));
                if (EditorGUI.EndChangeCheck() && next != active) {
                    Undo.RecordObject(go, next ? "Activate GameObject" : "Deactivate GameObject");
                    go.SetActive(next);
                }
                x -= Size + Spacing;
            }

            if (!s.soloButtons)
                return;

            var svm = SceneVisibilityManager.instance;

            // Visibility eye
            var eyeRect = IconRect(rect, x);
            var hidden = svm.IsHidden(go);
            DrawIcon(eyeRect, hidden ? "scenevis_hidden_hover" : "scenevis_visible_hover",
                "Click: toggle visibility (with children)\nRight-click: solo visibility");
            if (e.type == EventType.MouseDown && eyeRect.Contains(e.mousePosition)) {
                if (e.button == 0) {
                    svm.ToggleVisibility(go, true);
                } else if (e.button == 1) {
                    if (visibilityIsolated == go) {
                        svm.ExitIsolation();
                        visibilityIsolated = null;
                    } else {
                        svm.Isolate(go, true);
                        visibilityIsolated = go;
                    }
                }
                e.Use();
            }
            x -= Size + Spacing;

            // Pickability
            var pickRect = IconRect(rect, x);
            var pickDisabled = svm.IsPickingDisabled(go);
            DrawIcon(pickRect, pickDisabled ? "scenepicking_notpickable_hover" : "scenepicking_pickable_hover",
                "Click: toggle pickability (with children)\nRight-click: solo pickability");
            if (e.type == EventType.MouseDown && pickRect.Contains(e.mousePosition)) {
                if (e.button == 0) {
                    svm.TogglePicking(go, true);
                } else if (e.button == 1) {
                    if (pickingSoloed == go) {
                        svm.EnableAllPicking();
                        pickingSoloed = null;
                    } else {
                        svm.DisableAllPicking();
                        svm.EnablePicking(go, true);
                        pickingSoloed = go;
                    }
                }
                e.Use();
            }
        }

        private static Rect IconRect(Rect row, float x) {
            return new Rect(x, row.yMin + (row.height - Size) * 0.5f, Size, Size);
        }

        private static void DrawIcon(Rect rect, string iconName, string tooltip) {
            var content = EditorGUIUtility.IconContent(iconName);
            GUI.Label(rect, new GUIContent(content != null ? content.image : null, tooltip));
        }
    }
}
