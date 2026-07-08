#if UNITY_EDITOR
using FoundationPlatform.FrameworkInspector;
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

        private static GUIStyle s_sectionFoldoutTitle;
        private static GUIStyle s_buttonBox;
        private static GUIStyle s_buttonFoldout;

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
            s_sectionFoldoutTitle = null;
            s_buttonBox = null;
            s_buttonFoldout = null;
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

        public static Color InfoBoxBackground(InfoMessageType type)
        {
            EnsureSkin();
            return type switch
            {
                InfoMessageType.Error => EditorGUIUtility.isProSkin
                    ? new Color(0.45f, 0.12f, 0.12f, 0.35f)
                    : new Color(0.95f, 0.75f, 0.75f, 0.9f),
                InfoMessageType.Warning => EditorGUIUtility.isProSkin
                    ? new Color(0.45f, 0.35f, 0.05f, 0.35f)
                    : new Color(0.98f, 0.92f, 0.70f, 0.95f),
                InfoMessageType.Info => EditorGUIUtility.isProSkin
                    ? new Color(0.12f, 0.28f, 0.45f, 0.35f)
                    : new Color(0.78f, 0.88f, 0.98f, 0.95f),
                _ => EditorGUIUtility.isProSkin
                    ? new Color(0f, 0f, 0f, 0.12f)
                    : new Color(0f, 0f, 0f, 0.06f),
            };
        }

        public static Color InfoBoxBorder(InfoMessageType type)
        {
            EnsureSkin();
            return type switch
            {
                InfoMessageType.Error => EditorGUIUtility.isProSkin
                    ? new Color(0.85f, 0.25f, 0.25f, 0.55f)
                    : new Color(0.75f, 0.15f, 0.15f, 0.45f),
                InfoMessageType.Warning => EditorGUIUtility.isProSkin
                    ? new Color(0.95f, 0.75f, 0.15f, 0.55f)
                    : new Color(0.85f, 0.65f, 0.05f, 0.45f),
                InfoMessageType.Info => EditorGUIUtility.isProSkin
                    ? new Color(0.35f, 0.65f, 0.95f, 0.55f)
                    : new Color(0.15f, 0.45f, 0.85f, 0.45f),
                _ => SectionRuleColor,
            };
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

        public static Color FoldoutHeaderBackground
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.04f)
                    : new Color(0f, 0f, 0f, 0.04f);
            }
        }

        public static Color FoldoutHeaderHoverBackground
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.08f)
                    : new Color(0f, 0f, 0f, 0.06f);
            }
        }

        public static GUIStyle SectionFoldoutTitle
        {
            get
            {
                EnsureSkin();
                if (s_sectionFoldoutTitle == null)
                {
                    s_sectionFoldoutTitle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Clip,
                    };
                }
                return s_sectionFoldoutTitle;
            }
        }

        public static GUIStyle CompactButton => EditorStyles.miniButton;

        public static GUIStyle ButtonBox
        {
            get
            {
                EnsureSkin();
                if (s_buttonBox == null)
                    s_buttonBox = new GUIStyle(EditorStyles.miniButton) { fixedHeight = CompactButtonHeight };
                return s_buttonBox;
            }
        }

        public static GUIStyle ButtonFoldout
        {
            get
            {
                EnsureSkin();
                if (s_buttonFoldout == null)
                {
                    s_buttonFoldout = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
                }
                return s_buttonFoldout;
            }
        }

        public static GUIStyle ButtonStyleFor(ButtonStyle style) => style switch
        {
            ButtonStyle.Box => new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(6, 6, 6, 6) },
            ButtonStyle.FoldoutButton => ButtonFoldout,
            _ => CompactButton,
        };

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
        /// Full-width section header with subtle chrome — used by <c>[FoldoutGroup]</c>.
        /// </summary>
        public static bool SectionFoldout(bool expanded, string label)
            => SectionFoldout(expanded, new GUIContent(label));

        public static bool SectionFoldout(bool expanded, GUIContent label)
        {
            const float headerH = 22f;
            EditorGUILayout.Space(SectionSpacing);
            var rect = EditorGUILayout.GetControlRect(false, headerH);

            bool hover = rect.Contains(Event.current.mousePosition);
            EditorGUI.DrawRect(rect, hover ? FoldoutHeaderHoverBackground : FoldoutHeaderBackground);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), SectionRuleColor);

            var foldRect = new Rect(rect.x + 4f, rect.y + (headerH - EditorGUIUtility.singleLineHeight) * 0.5f, 14f, EditorGUIUtility.singleLineHeight);
            expanded = EditorGUI.Foldout(foldRect, expanded, GUIContent.none, true, EditorStyles.foldout);

            float labelX = foldRect.xMax + 2f;
            GUI.Label(new Rect(labelX, rect.y, rect.xMax - labelX - 4f, rect.height), label, SectionFoldoutTitle);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && Event.current.button == 0)
            {
                expanded = !expanded;
                GUI.changed = true;
                Event.current.Use();
            }

            return expanded;
        }

        public static void BeginSectionFoldoutBody()
        {
            EditorGUILayout.Space(SectionSpacing * 0.5f);
            EditorGUI.indentLevel++;
        }

        public static void EndSectionFoldoutBody()
        {
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(SectionSpacing);
        }

        /// <summary>
        /// Plain foldout for use inside <see cref="EditorStyles.helpBox"/> sections.
        /// <see cref="EditorStyles.foldoutHeader"/> overlaps box borders; use this instead.
        /// </summary>
        public static bool FoldoutInSection(bool expanded, string label)
        {
            return SectionFoldout(expanded, label);
        }

        public static bool FoldoutInSection(bool expanded, GUIContent label)
        {
            return SectionFoldout(expanded, label);
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

        /// <summary>Themed callout matching FrameworkInspector chrome (replaces raw HelpBox in engine UI).</summary>
        public static void DrawInfoBox(string message, InfoMessageType type = InfoMessageType.Info)
        {
            if (string.IsNullOrEmpty(message)) return;
            var style = new GUIStyle(EditorStyles.wordWrappedLabel) { padding = new RectOffset(8, 8, 6, 6) };
            float h = style.CalcHeight(new GUIContent(message), EditorGUIUtility.currentViewWidth - 24f);
            var rect = EditorGUILayout.GetControlRect(false, h + 8f);
            EditorGUI.DrawRect(rect, InfoBoxBackground(type));
            var border = InfoBoxBorder(type);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), border);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), border);
            GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, rect.height - 8f), message, style);
            EditorGUILayout.Space(SectionSpacing * 0.5f);
        }

        public static void DrawValidationBox(string message, InfoMessageType type = InfoMessageType.Error)
            => DrawInfoBox(message, type);

        /// <summary>Skin-aware toolbar for tab groups.</summary>
        public static int Toolbar(int selected, string[] labels)
        {
            var style = new GUIStyle(EditorStyles.toolbarButton) { fixedHeight = 22f };
            return GUILayout.Toolbar(selected, labels, style);
        }
    }
}
#endif
