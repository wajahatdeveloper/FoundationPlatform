using UnityEditor;
using UnityEngine;

namespace ProjectWindowX {
    /// <summary>Right-aligned grey file-extension label on Project window list rows.</summary>
    internal static class FileExtensionLabels {

        private const float MinRowWidth = 128f;
        private const float Padding = 4f;
        private static GUIStyle style;

        internal static void Draw(ProjectWindowX.RowContext ctx, Rect rect, ref float rightInset) {
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

            var width = style.CalcSize(new GUIContent(ctx.extension)).x;

            var label = rect;
            label.xMax -= rightInset + Padding;
            label.xMin = label.xMax - width;
            GUI.Label(label, ctx.extension, style);

            // Reserve the space we just drew into so later passes (badges/chips) land to our left.
            rightInset += width + Padding;
        }
    }
}
