using UnityEditor;
using UnityEngine;

namespace ProjectWindowX {
    /// <summary>Odd/even row tint for the Project window list mode.</summary>
    internal static class ZebraRows {

        internal static void Draw(Rect rect, ProjectWindowXSettings s) {
            if (Event.current.type != EventType.Repaint || s.oddRowColor.a <= 0.01f)
                return;
            if (rect.height <= 0f)
                return;

            var index = Mathf.FloorToInt(rect.y / rect.height);
            if ((index & 1) == 0)
                return;

            var row = rect;
            row.xMin = 0f;
            row.xMax = EditorGUIUtility.currentViewWidth;
            EditorGUI.DrawRect(row, s.oddRowColor);
        }
    }
}
