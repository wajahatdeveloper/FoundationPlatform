using UnityEditor;
using UnityEngine;

namespace ProjectWindowX {
    /// <summary>Odd/even row tint for the Project window list mode.</summary>
    public sealed class ZebraRowsPass : IProjectWindowXPass {
        public string Id => "projectwindowx.zebra-rows";
        public int Order => 0;

        public bool Enabled(ProjectWindowXSettings s) => s.zebraRows;

        public void Draw(ProjectWindowX.RowContext ctx, Rect rect, bool listMode, ref float rightInset) {
            if (!listMode)
                return;
            if (Event.current.type != EventType.Repaint || ProjectWindowXSettings.instance.oddRowColor.a <= 0.01f)
                return;
            if (rect.height <= 0f)
                return;

            var index = Mathf.FloorToInt(rect.y / rect.height);
            if ((index & 1) == 0)
                return;

            var row = rect;
            row.xMin = 0f;
            row.xMax = EditorGUIUtility.currentViewWidth;
            EditorGUI.DrawRect(row, ProjectWindowXSettings.instance.oddRowColor);
        }
    }
}
