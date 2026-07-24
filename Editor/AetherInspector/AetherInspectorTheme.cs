#if UNITY_EDITOR
using System;
using AetherNexus.FoundationPlatform.AetherInspector;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.AetherInspector.Editor
{
    /// <summary>
    /// Skin-aware IMGUI theme tokens and cached styles for AetherInspector chrome.
    /// Centralizes spacing, colors, and GUIStyles so inspectors align with Unity 6 editor look.
    /// </summary>
    public static class AetherInspectorTheme
    {
        public const float RowHeight = 18f;
        public const float SectionSpacing = 4f;
        /// <summary>Compact lead space before any header (foldout/title/box/Unity header).</summary>
        public const float HeaderSpacing = 2f;
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
        private static GUIStyle s_flatFoldout;
        private static GUIStyle s_tagChipText;
        private static GUIStyle s_tagChipRemove;
        private static GUIStyle s_headerButton;
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
            s_flatFoldout = null;
            s_tagChipText = null;
            s_tagChipRemove = null;
            s_headerButton = null;
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

        /// <summary>Fill behind a tag/chip pill.</summary>
        public static Color TagChipBackground
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.08f)
                    : new Color(0f, 0f, 0f, 0.10f);
            }
        }

        /// <summary>1px border around a tag/chip pill and color swatches.</summary>
        public static Color TagChipOutline
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.14f)
                    : new Color(0f, 0f, 0f, 0.22f);
            }
        }

        /// <summary>Accent stripe/swatch color when a tag has no metadata color.</summary>
        public static Color TagChipAccentFallback => new Color(0.5f, 0.5f, 0.5f, 1f);

        /// <summary>Accent for an unknown / out-of-scope tag (amber).</summary>
        public static Color TagWarningAccent => new Color(0.9f, 0.6f, 0.1f, 1f);

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

        /// <summary>
        /// Canonical flat header label — bold 12pt, left-aligned, clipped.
        /// Used by every section header (foldout, box, list, nested struct) for a single look.
        /// </summary>
        public static GUIStyle FlatHeaderLabel
        {
            get
            {
                EnsureSkin();
                if (s_sectionFoldoutTitle == null)
                {
                    s_sectionFoldoutTitle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 12,
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Clip,
                    };
                }
                return s_sectionFoldoutTitle;
            }
        }

        /// <summary>Back-compat alias — routes to <see cref="FlatHeaderLabel"/>.</summary>
        public static GUIStyle SectionFoldoutTitle => FlatHeaderLabel;

        /// <summary>
        /// Layout-based flat foldout style (bold 12pt) for <c>EditorGUILayout/EditorGUI.Foldout</c>
        /// calls (list headers, nested structs) so their arrow + label match the group foldout.
        /// </summary>
        public static GUIStyle FlatFoldoutStyle
        {
            get
            {
                EnsureSkin();
                if (s_flatFoldout == null)
                {
                    s_flatFoldout = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold,
                        fontSize = 12,
                    };
                }
                return s_flatFoldout;
            }
        }

        public static GUIStyle CompactButton => EditorStyles.miniButton;

        /// <summary>Text style inside a tag/chip pill.</summary>
        public static GUIStyle TagChipText
        {
            get
            {
                EnsureSkin();
                if (s_tagChipText == null)
                    s_tagChipText = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 10,
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(4, 0, 0, 0),
                    };
                return s_tagChipText;
            }
        }

        /// <summary>Small "×" remove button inside a tag/chip pill.</summary>
        public static GUIStyle TagChipRemoveButton
        {
            get
            {
                EnsureSkin();
                if (s_tagChipRemove == null)
                    s_tagChipRemove = new GUIStyle(EditorStyles.miniButton)
                    {
                        fixedWidth = 16,
                        fixedHeight = 16,
                        padding = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(2, 0, 0, 0),
                        fontSize = 11,
                        alignment = TextAnchor.MiddleCenter,
                    };
                return s_tagChipRemove;
            }
        }

        /// <summary>Square icon button (e.g. "+") for section header rows.</summary>
        public static GUIStyle HeaderButton
        {
            get
            {
                EnsureSkin();
                if (s_headerButton == null)
                    s_headerButton = new GUIStyle(EditorStyles.miniButton)
                    {
                        fixedWidth = 20,
                        fixedHeight = 16,
                        fontSize = 14,
                        padding = new RectOffset(0, 0, -2, 0),
                    };
                return s_headerButton;
            }
        }

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
        /// Flat section header — used by <c>[FoldoutGroup]</c>. Renders through the same
        /// <see cref="FlatFoldoutStyle"/> as list/nested foldouts so the arrow + label land at
        /// the identical x for a given indent level. A single subtle bottom rule at top-level
        /// depth separates top sections; nested foldouts stay chrome-free.
        /// </summary>
        public static bool SectionFoldout(bool expanded, string label)
            => SectionFoldout(expanded, new GUIContent(label));

        public static bool SectionFoldout(bool expanded, GUIContent label)
        {
            EditorGUILayout.Space(HeaderSpacing);
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            if (EditorGUI.indentLevel <= 0)
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), SectionRuleColor);

            return EditorGUI.Foldout(rect, expanded, label, true, FlatFoldoutStyle);
        }

        public static void BeginSectionFoldoutBody()
        {
            EditorGUILayout.Space(HeaderSpacing);
            EditorGUI.indentLevel++;
        }

        public static void EndSectionFoldoutBody()
        {
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(HeaderSpacing);
        }

        /// <summary>
        /// Rect-based flat section header for hand-laid drawers (that compute their own rects in
        /// GetPropertyHeight). Draws a flat foldout (arrow + bold label) through <see cref="FlatFoldoutStyle"/>
        /// — matching layout foldouts — and reserves <paramref name="trailingButtons"/> square slots on the
        /// right for caller-drawn buttons. No dark strip. <paramref name="trailingRects"/> is right-to-left:
        /// index 0 is the rightmost button.
        /// </summary>
        public static bool SectionHeaderRow(Rect rect, GUIContent label, bool expanded, int trailingButtons,
            out Rect[] trailingRects, float buttonSize = 20f)
        {
            const float gap = 2f;
            const float buttonHeight = 16f;
            trailingRects = trailingButtons > 0 ? new Rect[trailingButtons] : Array.Empty<Rect>();

            float by = rect.y + (rect.height - buttonHeight) * 0.5f;
            float x = rect.xMax;
            for (int i = 0; i < trailingButtons; i++)
            {
                x -= buttonSize;
                trailingRects[i] = new Rect(x, by, buttonSize, buttonHeight);
                x -= gap;
            }

            float foldWidth = Mathf.Max(0f, x - rect.x - gap);
            var foldRect = new Rect(rect.x, rect.y, foldWidth, rect.height);
            return EditorGUI.Foldout(foldRect, expanded, label, true, FlatFoldoutStyle);
        }

        /// <summary>
        /// Themed tag/chip pill: <see cref="TagChipBackground"/> fill, accent stripe, <see cref="TagChipOutline"/>
        /// border, and an optional "×" remove button. Skin-aware; no baked colors.
        /// </summary>
        public static void DrawTagPill(Rect rect, GUIContent content, Color accent, Action onRemove)
        {
            EditorGUI.DrawRect(rect, TagChipBackground);
            EditorGUI.DrawRect(new Rect(rect.x + 1, rect.y + 1, 4, rect.height - 2), accent);

            float labelWidth = onRemove != null ? rect.width - 24 : rect.width - 6;
            GUI.Label(new Rect(rect.x + 6, rect.y, labelWidth, rect.height), content, TagChipText);

            if (onRemove != null)
            {
                var removeRect = new Rect(rect.xMax - 18, rect.y + 2, 16, 16);
                if (GUI.Button(removeRect, "×", TagChipRemoveButton))
                    onRemove.Invoke();
            }

            DrawRectOutline(rect, TagChipOutline);
        }

        /// <summary>Draw a 1px outline around a rect (replaces Handles.DrawSolidRectangleWithOutline).</summary>
        public static void DrawRectOutline(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
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
            EditorGUILayout.Space(HeaderSpacing);
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
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

        public static void DrawInfoBox(string message, InfoMessageType type = InfoMessageType.Info)
        {
            bool dummy = true;
            DrawInfoBox(message, type, ref dummy, collapsible: false);
        }

        /// <summary>Themed callout matching AetherInspector chrome.</summary>
        public static void DrawInfoBox(string message, InfoMessageType type, ref bool expanded, bool collapsible)
        {
            if (string.IsNullOrEmpty(message)) return;

            var style = new GUIStyle(EditorStyles.wordWrappedLabel) { padding = new RectOffset(8, 8, 6, 6) };
            float wrapW = EditorGUIUtility.currentViewWidth - 24f;

            if (collapsible)
            {
                var headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                EditorGUI.DrawRect(headerRect, InfoBoxBackground(type));
                var border = InfoBoxBorder(type);
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, headerRect.width, 1f), border);
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 1f, headerRect.width, 1f), border);
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, 1f, headerRect.height), border);
                EditorGUI.DrawRect(new Rect(headerRect.xMax - 1f, headerRect.y, 1f, headerRect.height), border);

                var prevIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                var arrowRect = new Rect(headerRect.x + 6f, headerRect.y + 3f, 16f, headerRect.height - 6f);
                expanded = EditorGUI.Foldout(arrowRect, expanded, GUIContent.none, true);
                EditorGUI.indentLevel = prevIndent;

                var labelRect = new Rect(arrowRect.xMax + 4f, headerRect.y,
                    headerRect.width - arrowRect.width - 14f, headerRect.height);
                GUI.Label(labelRect, expanded ? message : "...", EditorStyles.label);

                if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
                {
                    expanded = !expanded;
                    Event.current.Use();
                    GUI.changed = true;
                }

                if (!expanded)
                {
                    EditorGUILayout.Space(SectionSpacing * 0.5f);
                    return;
                }

                EditorGUILayout.Space(2f);
            }

            if (!collapsible || expanded)
            {
                float h = style.CalcHeight(new GUIContent(message), wrapW);
                var rect = EditorGUILayout.GetControlRect(false, h + 8f);
                EditorGUI.DrawRect(rect, InfoBoxBackground(type));
                var border = InfoBoxBorder(type);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), border);
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), border);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), border);
                EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), border);
                GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, rect.height - 8f), message, style);
                if (!collapsible) EditorGUILayout.Space(SectionSpacing * 0.5f);
            }
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
