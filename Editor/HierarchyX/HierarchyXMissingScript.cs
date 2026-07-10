using UnityEditor;
using UnityEngine;

namespace HierarchyX {
    /// <summary>
    /// Draws a red error badge on rows whose GameObject carries one or more
    /// missing (null) scripts. Clicking the badge selects the object.
    /// </summary>
    internal static class HierarchyXMissingScript {

        private const float Width = 16f;
        private static GUIContent icon;

        /// <summary>Returns the right-edge width consumed (badge + gap), 0 when nothing drawn.</summary>
        internal static float Draw(Rect rect, GameObject go, HierarchyXSettings s, float extraRightInset) {
            if (!s.missingScriptIndicator)
                return 0f;
            if (!HierarchyXRowCache.Get(go).hasMissingScript)
                return 0f;

            if (icon == null) {
                icon = new GUIContent(EditorGUIUtility.IconContent("console.erroricon.sml")) {
                    tooltip = "Missing script(s) on this GameObject"
                };
            }

            var badge = new Rect(
                EditorGUIUtility.currentViewWidth - s.rightMargin - extraRightInset - Width,
                rect.yMin + (rect.height - Width) * 0.5f,
                Width, Width);

            if (GUI.Button(badge, icon, HierarchyXStyles.TransparentButton))
                Selection.activeGameObject = go;

            return Width + 2f;
        }
    }
}
