#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FoundationPlatform.FrameworkInspector.Editor
{
    /// <summary>
    /// Skin-aware IMGUI theme tokens and cached styles for FrameworkInspector chrome.
    /// Centralizes spacing, colors, and GUIStyles so inspectors align with Unity 6 editor look.
    /// </summary>
    public static class FrameworkInspectorTheme
    {
        public const float RowHeight = 18f;
        public const float SectionSpacing = 4f;
        public const float CompactButtonHeight = 20f;
        public const float DefaultLabelWidth = 0f;

        private static bool? s_lastProSkin;

        private static GUIStyle s_sectionTitle;
        private static GUIStyle s_sectionSubtitle;
        private static GUIStyle s_centeredSectionTitle;
        private static GUIStyle s_tableHeader;
        private static GUIStyle s_tableCell;
        private static GUIStyle s_menuRow;
        private static GUIStyle s_menuRowSelected;

        [InitializeOnLoadMethod]
        private static void OnLoad() => InvalidateSkinCache();

        public static void InvalidateSkinCache()
        {
            s_lastProSkin = null;
            ClearStyleCache();
        }

        private static void ClearStyleCache()
        {
            s_sectionTitle = null;
            s_sectionSubtitle = null;
            s_centeredSectionTitle = null;
            s_tableHeader = null;
            s_tableCell = null;
            s_menuRow = null;
            s_menuRowSelected = null;
        }

        private static void EnsureSkin()
        {
            bool pro = EditorGUIUtility.isProSkin;
            if (s_lastProSkin == pro) return;
            ClearStyleCache();
            s_lastProSkin = pro;
        }

        // --- Colors (skin-aware) ---------------------------------------------------------

        public static Color BoxHeaderBackground
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(0f, 0f, 0f, 0.15f)
                    : new Color(0f, 0f, 0f, 0.06f);
            }
        }

        public static Color SectionRuleColor
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.12f)
                    : new Color(0f, 0f, 0f, 0.18f);
            }
        }

        public static Color MenuSelectionBackground
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(0.17f, 0.36f, 0.53f, 0.85f)
                    : new Color(0.22f, 0.44f, 0.70f, 0.55f);
            }
        }

        public static Color MenuHoverBackground
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.05f)
                    : new Color(0f, 0f, 0f, 0.05f);
            }
        }

        public static Color MenuSeparatorColor
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(0f, 0f, 0f, 0.45f)
                    : new Color(0f, 0f, 0f, 0.20f);
            }
        }

        public static Color TableHeaderBackground
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(0f, 0f, 0f, 0.22f)
                    : new Color(0f, 0f, 0f, 0.08f);
            }
        }

        public static Color TableRowBackgroundA
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(0f, 0f, 0f, 0.06f)
                    : Color.clear;
            }
        }

        public static Color TableRowBackgroundB
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.02f)
                    : new Color(0f, 0f, 0f, 0.03f);
            }
        }

        public static Color TableGridLine
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(0f, 0f, 0f, 0.22f)
                    : new Color(0f, 0f, 0f, 0.12f);
            }
        }

        public static Color ProgressBarBackground
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(0.16f, 0.16f, 0.16f)
                    : new Color(0.75f, 0.75f, 0.75f);
            }
        }

        // --- Styles ----------------------------------------------------------------------

        public static GUIStyle SectionTitle
        {
            get
            {
                EnsureSkin();
                if (s_sectionTitle == null)
                    s_sectionTitle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
                return s_sectionTitle;
            }
        }

        public static GUIStyle SectionSubtitle
        {
            get
            {
                EnsureSkin();
                if (s_sectionSubtitle == null)
                    s_sectionSubtitle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
                return s_sectionSubtitle;
            }
        }

        public static GUIStyle CenteredSectionTitle
        {
            get
            {
                EnsureSkin();
                if (s_centeredSectionTitle == null)
                    s_centeredSectionTitle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
                return s_centeredSectionTitle;
            }
        }

        public static GUIStyle CompactButton => EditorStyles.miniButton;

        public static GUIStyle TableHeader
        {
            get
            {
                EnsureSkin();
                if (s_tableHeader == null)
                    s_tableHeader = new GUIStyle(EditorStyles.miniBoldLabel) { fontSize = 11, clipping = TextClipping.Clip };
                return s_tableHeader;
            }
        }

        public static GUIStyle TableCell
        {
            get
            {
                EnsureSkin();
                if (s_tableCell == null)
                    s_tableCell = new GUIStyle(EditorStyles.label) { fontSize = 11, clipping = TextClipping.Clip };
                return s_tableCell;
            }
        }

        public static GUIStyle MenuRow
        {
            get
            {
                EnsureSkin();
                if (s_menuRow == null)
                    s_menuRow = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
                return s_menuRow;
            }
        }

        public static GUIStyle MenuRowSelected
        {
            get
            {
                EnsureSkin();
                if (s_menuRowSelected == null)
                {
                    s_menuRowSelected = new GUIStyle(MenuRow);
                    s_menuRowSelected.normal.textColor = Color.white;
                    s_menuRowSelected.onNormal.textColor = Color.white;
                }
                return s_menuRowSelected;
            }
        }

        public static GUIStyle CreateDisplayAsStringStyle(DisplayAsStringAttribute das)
        {
            var style = new GUIStyle(EditorStyles.label)
            {
                wordWrap = !das.Overflow,
                richText = das.EnableRichText,
                alignment = das.Alignment == TextAlignment.Center ? TextAnchor.MiddleCenter
                    : das.Alignment == TextAlignment.Right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft,
            };
            if (das.FontSize > 0) style.fontSize = das.FontSize;
            return style;
        }

        public static GUIStyle CreateProgressBarLabelStyle(ProgressBarAttribute pb)
        {
            return new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = pb.ValueLabelAlignment == TextAlignment.Left ? TextAnchor.MiddleLeft
                    : pb.ValueLabelAlignment == TextAlignment.Right ? TextAnchor.MiddleRight : TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };
        }

        // --- Layout helpers --------------------------------------------------------------

        public static void BeginInspectorScope()
        {
            EditorGUILayout.Space(2f);
        }

        public static void EndInspectorScope()
        {
            EditorGUILayout.Space(2f);
        }

        public static void BeginSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        }

        public static void EndSection() => EditorGUILayout.EndVertical();

        public static void BeginBox(string label = null)
        {
            BeginSection();
            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.LabelField(label, SectionTitle);
        }

        public static void EndBox() => EndSection();

        public static void BeginBoxHeader()
        {
            var rect = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(rect, BoxHeaderBackground);
        }

        public static void EndBoxHeader() => EditorGUILayout.EndHorizontal();

        public static bool Foldout(bool expanded, string label)
            => EditorGUILayout.Foldout(expanded, label, true, EditorStyles.foldoutHeader);

        public static bool Foldout(bool expanded, GUIContent label)
            => EditorGUILayout.Foldout(expanded, label, true, EditorStyles.foldoutHeader);

        /// <summary>
        /// Plain foldout for use inside <see cref="EditorStyles.helpBox"/> sections.
        /// <see cref="EditorStyles.foldoutHeader"/> overlaps box borders; use this instead.
        /// </summary>
        public static bool FoldoutInSection(bool expanded, string label)
        {
            EditorGUILayout.Space(SectionSpacing * 0.5f);
            return EditorGUILayout.Foldout(expanded, label, true);
        }

        public static bool FoldoutInSection(bool expanded, GUIContent label)
        {
            EditorGUILayout.Space(SectionSpacing * 0.5f);
            return EditorGUILayout.Foldout(expanded, label, true);
        }

        public static void DrawTitle(string title) => DrawTitle(title, null);

        public static void DrawTitle(string title, string subtitle, TextAlignment textAlignment = TextAlignment.Left,
            bool horizontalLine = true, bool boldLabel = true)
        {
            EditorGUILayout.Space(2f);
            var rect = EditorGUILayout.GetControlRect(false, 20f);
            var ts = boldLabel ? SectionTitle : EditorStyles.label;
            ts.alignment = textAlignment == TextAlignment.Center ? TextAnchor.MiddleCenter
                : textAlignment == TextAlignment.Right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            GUI.Label(rect, title, ts);
            if (!string.IsNullOrEmpty(subtitle))
                GUI.Label(rect, subtitle, SectionSubtitle);
            if (horizontalLine)
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax, rect.width, 1f), SectionRuleColor);
            EditorGUILayout.Space(2f);
        }
    }
}
#endif
