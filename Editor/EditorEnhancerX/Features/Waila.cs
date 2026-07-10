using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EditorEnhancerX {
    /// <summary>
    /// Waila ("what am I looking at"): floating tooltip naming the object under the
    /// Scene View cursor (optional modifier gate). Smart Selection: a shortcut that
    /// cycles through overlapping objects under the cursor, deepest-first.
    /// </summary>
    [InitializeOnLoad]
    internal static class Waila {

        private static GameObject hovered;
        private static GUIStyle tooltipStyle;

        // Smart-selection cycle state
        private static readonly List<GameObject> cycleIgnore = new List<GameObject>();
        private static Vector2 cyclePosition;

        static Waila() {
            SceneViewHub.Register("waila", 10, Pass);
            KeyRouter.Register("smartSelect",
                () => EditorEnhancerXSettings.instance.smartSelectKey,
                KeyScope.SceneView,
                CycleSelection);
        }

        private static void Pass(SceneView view) {
            var s = EditorEnhancerXSettings.instance.waila;
            if (!s.enabled) {
                hovered = null;
                return;
            }

            var e = Event.current;
            if (e.type == EventType.MouseMove) {
                var modifierOk = !s.requireModifier || (e.modifiers & s.modifiers) == s.modifiers;
                var next = modifierOk ? HandleUtility.PickGameObject(e.mousePosition, false) : null;
                if (!ReferenceEquals(next, hovered)) {
                    hovered = next;
                    view.Repaint();
                }
            } else if (e.type == EventType.Repaint && hovered != null) {
                DrawTooltip(e.mousePosition);
            }
        }

        private static void DrawTooltip(Vector2 mousePosition) {
            if (tooltipStyle == null) {
                tooltipStyle = new GUIStyle(EditorStyles.helpBox) {
                    fontSize = 11,
                    padding = new RectOffset(6, 6, 3, 3),
                };
            }

            var text = hovered.name;
            var parent = hovered.transform.parent;
            if (parent != null)
                text = parent.name + " / " + text;

            Handles.BeginGUI();
            var content = new GUIContent(text);
            var size = tooltipStyle.CalcSize(content);
            var rect = new Rect(mousePosition.x + 14f, mousePosition.y + 10f, size.x, size.y);
            GUI.Label(rect, content, tooltipStyle);
            Handles.EndGUI();
        }

        private static bool CycleSelection() {
            var e = Event.current;
            if (e == null)
                return false;

            if ((e.mousePosition - cyclePosition).sqrMagnitude > 16f)
                cycleIgnore.Clear();
            cyclePosition = e.mousePosition;

            var picked = HandleUtility.PickGameObject(e.mousePosition, false, cycleIgnore.ToArray());
            if (picked == null && cycleIgnore.Count > 0) {
                // Wrapped past the last overlapping object — restart the cycle.
                cycleIgnore.Clear();
                picked = HandleUtility.PickGameObject(e.mousePosition, false);
            }
            if (picked == null)
                return false;

            cycleIgnore.Add(picked);
            Selection.activeGameObject = picked;
            return true;
        }
    }
}
