#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FoundationPlatform.FrameworkInspector.Editor
{
    /// <summary>
    /// Native IMGUI layout helpers used by this project's editor windows/inspectors (boxes, box
    /// headers, foldouts, titles). Lives in the autoreferenced FoundationPlatform.FrameworkInspector.Editor assembly so
    /// every editor asmdef can use it without a reference edit.
    /// </summary>
    public static class GuiKit
    {
        private static readonly Color HeaderBg = new(0f, 0f, 0f, 0.15f);
        private static readonly Color RuleColor = new(0.5f, 0.5f, 0.5f, 0.5f);
        private static GUIStyle _titleStyle;
        private static GUIStyle _subtitleStyle;

        public static void BeginBox(string label = null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        public static void EndBox() => EditorGUILayout.EndVertical();

        public static void BeginBoxHeader()
        {
            var rect = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(rect, HeaderBg);
        }

        public static void EndBoxHeader() => EditorGUILayout.EndHorizontal();

        public static bool Foldout(bool expanded, string label)
            => EditorGUILayout.Foldout(expanded, label, true, EditorStyles.foldoutHeader);

        public static bool Foldout(bool expanded, GUIContent label)
            => EditorGUILayout.Foldout(expanded, label, true, EditorStyles.foldoutHeader);

        public static void Title(string title) => Title(title, null);

        public static void Title(string title, string subtitle, TextAlignment textAlignment = TextAlignment.Left,
            bool horizontalLine = true, bool boldLabel = true)
        {
            EnsureStyles();
            EditorGUILayout.Space(2f);
            var rect = EditorGUILayout.GetControlRect(false, 20f);
            var ts = boldLabel ? _titleStyle : EditorStyles.label;
            ts.alignment = textAlignment == TextAlignment.Center ? TextAnchor.MiddleCenter
                : textAlignment == TextAlignment.Right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            GUI.Label(rect, title, ts);
            if (!string.IsNullOrEmpty(subtitle))
                GUI.Label(rect, subtitle, _subtitleStyle);
            if (horizontalLine)
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax, rect.width, 1f), RuleColor);
            EditorGUILayout.Space(2f);
        }

        private static void EnsureStyles()
        {
            if (_titleStyle != null) return;
            _titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            _subtitleStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
        }
    }
}
#endif
