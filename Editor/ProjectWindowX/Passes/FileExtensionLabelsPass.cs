using UnityEditor;
using UnityEngine;

namespace ProjectWindowX {
    /// <summary>Right-aligned grey file-extension label on Project window list rows.</summary>
    public sealed class FileExtensionLabelsPass : IProjectWindowXPass {
        private const float MinRowWidth = 128f;
        private const float Padding = 4f;
        private static GUIStyle style;

        public string Id => "projectwindowx.file-extension-labels";
        // Runs before the badge passes (250/260) so the extension label claims the
        // rightmost slot and badges/chips are pushed to its left, not the other way round.
        public int Order => 210;

        public bool Enabled(ProjectWindowXSettings s) => s.extensionLabels;

        public void Draw(ProjectWindowX.RowContext ctx, Rect rect, bool listMode, ref float rightInset) {
            if (!listMode || ctx.isFolder)
                return;
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

            rightInset += width + Padding;
        }
    }
}
