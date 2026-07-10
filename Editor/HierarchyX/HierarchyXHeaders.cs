using System;
using UnityEditor;
using UnityEngine;

namespace HierarchyX {
    /// <summary>
    /// Renders GameObjects whose name starts with the configured prefix (default "---")
    /// as full-width section header bars. Header objects are plain, empty GameObjects
    /// tagged EditorOnly so they are stripped from builds.
    /// </summary>
    internal static class HierarchyXHeaders {

        private const string CreateMenuPath = "GameObject/HierarchyX Header";
        private static readonly char[] TrimChars = { ' ', '-', '=', '/' };

        /// <summary>True when the row is a header (caller skips icon/tree/label passes).</summary>
        internal static bool TryDraw(Rect rect, GameObject go, HierarchyXSettings s) {
            if (!s.headersEnabled || string.IsNullOrEmpty(s.headerPrefix))
                return false;

            var name = go.name;
            if (!name.StartsWith(s.headerPrefix, StringComparison.Ordinal))
                return false;

            if (Event.current.type == EventType.Repaint) {
                var bar = rect;
                bar.xMin = 0f;
                bar.xMax = EditorGUIUtility.currentViewWidth;

                // Keep Unity's selection highlight readable through the bar.
                var color = s.headerColor;
                if (Selection.Contains(go))
                    color.a *= 0.5f;
                EditorGUI.DrawRect(bar, color);

                var text = name.Substring(s.headerPrefix.Length).Trim(TrimChars);
                if (text.Length == 0)
                    text = name;
                GUI.Label(bar, text, HierarchyXStyles.HeaderStyle);
            }
            return true;
        }

        [MenuItem(CreateMenuPath, false, 0)]
        private static void CreateHeader() {
            var s = HierarchyXSettings.Instance;
            var prefix = string.IsNullOrEmpty(s.headerPrefix) ? "---" : s.headerPrefix;
            var go = new GameObject(prefix + " Header") { tag = "EditorOnly" };

            var active = Selection.activeTransform;
            if (active != null) {
                go.transform.SetParent(active.parent, false);
                go.transform.SetSiblingIndex(active.GetSiblingIndex());
            }

            Undo.RegisterCreatedObjectUndo(go, "Create Header");
            Selection.activeGameObject = go;
        }
    }
}
