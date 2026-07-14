using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Live readout of the active transform's position/rotation/scale next to the
    /// current Move/Rotate/Scale handle in the Scene View.
    /// </summary>
    [InitializeOnLoad]
    internal static class ToolValues {

        private static GUIStyle style;

        static ToolValues() {
            SceneViewHub.Register("toolValues", 30, Pass);
        }

        private static void Pass(SceneView view) {
            if (!EditorEnhancerXSettings.instance.toolValuesEnabled)
                return;
            if (Event.current.type != EventType.Repaint)
                return;

            var active = Selection.activeTransform;
            if (active == null)
                return;

            string text;
            switch (UnityEditor.Tools.current) {
                case Tool.Move:
                    var p = active.position;
                    text = $"P  {p.x:0.###}  {p.y:0.###}  {p.z:0.###}";
                    break;
                case Tool.Rotate:
                    var r = active.eulerAngles;
                    text = $"R  {r.x:0.#}  {r.y:0.#}  {r.z:0.#}";
                    break;
                case Tool.Scale:
                    var sc = active.localScale;
                    text = $"S  {sc.x:0.###}  {sc.y:0.###}  {sc.z:0.###}";
                    break;
                default:
                    return;
            }
            if (Selection.transforms.Length > 1)
                text += $"   (+{Selection.transforms.Length - 1})";

            if (style == null) {
                style = new GUIStyle(EditorStyles.helpBox) {
                    fontSize = 11,
                    padding = new RectOffset(6, 6, 3, 3),
                };
            }

            var guiPoint = HandleUtility.WorldToGUIPoint(UnityEditor.Tools.handlePosition);
            Handles.BeginGUI();
            var content = new GUIContent(text);
            var size = style.CalcSize(content);
            GUI.Label(new Rect(guiPoint.x + 20f, guiPoint.y + 20f, size.x, size.y), content, style);
            Handles.EndGUI();
        }
    }
}
