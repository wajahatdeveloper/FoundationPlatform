using UnityEditor;
using UnityEngine;

namespace ProjectWindowX {
    /// <summary>Right-aligned grey file-extension label on Project window list rows.</summary>
    internal static class FileExtensionLabels {

        private const float MinRowWidth = 128f;
        private static GUIStyle style;

        internal static void Draw(ProjectWindowX.RowContext ctx, Rect rect, float rightInset) {
            if (Event.current.type != EventType.Repaint)
                return;
            if (string.IsNullOrEmpty(ctx.extension) || rect.width < MinRowWidth)
                return;

            if (style == null) {
                style = new GUIStyle(EditorStyles.miniLabel) {
                    alignment = TextAnchor.MiddleRight,
                };
                style.normal.textColor = EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.35f)
                    : new Color(0f, 0f, 0f, 0.35f);
            }

            var label = rect;
            label.xMax -= rightInset + 4f;
            GUI.Label(label, ctx.extension, style);
        }
    }
}
