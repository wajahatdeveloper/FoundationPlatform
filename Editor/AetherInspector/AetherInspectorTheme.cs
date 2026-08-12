#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
        private static GUIStyle s_sectionTitleCenter;
        private static GUIStyle s_sectionTitleRight;
        private static GUIStyle s_sectionTitlePlain;
        private static GUIStyle s_sectionTitlePlainCenter;
        private static GUIStyle s_sectionTitlePlainRight;
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
        private static GUIStyle s_buttonHelpBox;
        private static GUIStyle s_compactButton;
        private static GUIStyle s_toolbarButton;
        private static GUIStyle s_infoBoxLabel;
        private static GUIStyle s_infoBoxCollapsedLabel;
        private static GUIStyle s_boxContainer;
        private static GUIStyle s_dropdownField;
        private static GUIStyle s_progressBarLabelCenter;
        private static GUIStyle s_progressBarLabelLeft;
        private static GUIStyle s_progressBarLabelRight;
        private static readonly GUIContent s_tempContent = new GUIContent();
        private static readonly Vector3[] s_caretVerts = new Vector3[3];
        private static readonly Dictionary<int, Texture2D> s_tintTexCache = new Dictionary<int, Texture2D>();
        private static readonly Dictionary<long, GUIStyle> s_displayAsStringCache = new Dictionary<long, GUIStyle>();

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
            s_sectionTitleCenter = null;
            s_sectionTitleRight = null;
            s_sectionTitlePlain = null;
            s_sectionTitlePlainCenter = null;
            s_sectionTitlePlainRight = null;
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
            s_buttonHelpBox = null;
            s_compactButton = null;
            s_toolbarButton = null;
            s_infoBoxLabel = null;
            s_infoBoxCollapsedLabel = null;
            s_boxContainer = null;
            s_dropdownField = null;
            s_progressBarLabelCenter = null;
            s_progressBarLabelLeft = null;
            s_progressBarLabelRight = null;
            s_displayAsStringCache.Clear();
            foreach (var kv in s_tintTexCache)
            {
                if (kv.Value != null) UnityEngine.Object.DestroyImmediate(kv.Value);
            }
            s_tintTexCache.Clear();
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

        public static Color BoxBackground
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.03f)
                    : new Color(0f, 0f, 0f, 0.03f);
            }
        }

        public static Color BoxBorder
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.10f)
                    : new Color(0f, 0f, 0f, 0.12f);
            }
        }

        public static Color FieldBackground
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(0f, 0f, 0f, 0.22f)
                    : new Color(1f, 1f, 1f, 0.55f);
            }
        }

        public static Color FieldBorder
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.12f)
                    : new Color(0f, 0f, 0f, 0.18f);
            }
        }

        public static Color SliderTrack
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.12f)
                    : new Color(0f, 0f, 0f, 0.16f);
            }
        }

        public static Color SliderFill
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(0.28f, 0.52f, 0.80f, 0.95f)
                    : new Color(0.22f, 0.45f, 0.72f, 0.90f);
            }
        }

        public static Color SliderThumb
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(0.92f, 0.92f, 0.92f, 1f)
                    : new Color(1f, 1f, 1f, 1f);
            }
        }

        public static Color ToggleTrackOff
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.14f)
                    : new Color(0f, 0f, 0f, 0.18f);
            }
        }

        public static Color ToggleTrackOn => SliderFill;

        public static Color ToggleThumb => SliderThumb;

        public static Color TabSelectedBackground
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.10f)
                    : new Color(0f, 0f, 0f, 0.08f);
            }
        }

        public static Color TabIdleBackground
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(0f, 0f, 0f, 0.08f)
                    : new Color(0f, 0f, 0f, 0.03f);
            }
        }

        public static Color ListDragLine
        {
            get
            {
                EnsureSkin();
                return EditorGUIUtility.isProSkin
                    ? new Color(0.35f, 0.60f, 0.95f, 1f)
                    : new Color(0.24f, 0.49f, 0.90f, 1f);
            }
        }

        public static Color Accent => SliderFill;

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
                    s_sectionTitle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 12,
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Clip,
                    };
                return s_sectionTitle;
            }
        }

        public static GUIStyle SectionTitleCentered
        {
            get
            {
                EnsureSkin();
                if (s_sectionTitleCenter == null)
                {
                    s_sectionTitleCenter = new GUIStyle(SectionTitle) { alignment = TextAnchor.MiddleCenter };
                }
                return s_sectionTitleCenter;
            }
        }

        public static GUIStyle SectionTitleRight
        {
            get
            {
                EnsureSkin();
                if (s_sectionTitleRight == null)
                {
                    s_sectionTitleRight = new GUIStyle(SectionTitle) { alignment = TextAnchor.MiddleRight };
                }
                return s_sectionTitleRight;
            }
        }

        public static GUIStyle SectionTitlePlain
        {
            get
            {
                EnsureSkin();
                if (s_sectionTitlePlain == null)
                    s_sectionTitlePlain = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 12,
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Clip,
                    };
                return s_sectionTitlePlain;
            }
        }

        public static GUIStyle TitleStyle(TextAlignment alignment, bool bold)
        {
            if (bold)
            {
                return alignment == TextAlignment.Center ? SectionTitleCentered
                    : alignment == TextAlignment.Right ? SectionTitleRight
                    : SectionTitle;
            }
            EnsureSkin();
            if (alignment == TextAlignment.Center)
            {
                if (s_sectionTitlePlainCenter == null)
                    s_sectionTitlePlainCenter = new GUIStyle(SectionTitlePlain) { alignment = TextAnchor.MiddleCenter };
                return s_sectionTitlePlainCenter;
            }
            if (alignment == TextAlignment.Right)
            {
                if (s_sectionTitlePlainRight == null)
                    s_sectionTitlePlainRight = new GUIStyle(SectionTitlePlain) { alignment = TextAnchor.MiddleRight };
                return s_sectionTitlePlainRight;
            }
            return SectionTitlePlain;
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

        public static GUIStyle CompactButton
        {
            get
            {
                EnsureSkin();
                if (s_compactButton == null)
                    s_compactButton = new GUIStyle(EditorStyles.miniButton) { fixedHeight = CompactButtonHeight };
                return s_compactButton;
            }
        }

        public static GUIStyle BoxContainerStyle
        {
            get
            {
                EnsureSkin();
                if (s_boxContainer == null)
                {
                    s_boxContainer = new GUIStyle(EditorStyles.helpBox)
                    {
                        padding = new RectOffset(8, 8, 6, 6),
                        margin = new RectOffset(0, 0, 2, 2),
                    };
                }
                return s_boxContainer;
            }
        }

        public static GUIStyle DropdownFieldStyle
        {
            get
            {
                EnsureSkin();
                if (s_dropdownField == null)
                {
                    s_dropdownField = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(6, 18, 0, 0),
                        clipping = TextClipping.Clip,
                    };
                }
                return s_dropdownField;
            }
        }

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

        public static GUIStyle ButtonStyleFor(ButtonStyle style)
        {
            EnsureSkin();
            switch (style)
            {
                case ButtonStyle.Box:
                    if (s_buttonHelpBox == null)
                        s_buttonHelpBox = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(6, 6, 6, 6), alignment = TextAnchor.MiddleCenter };
                    return s_buttonHelpBox;
                case ButtonStyle.FoldoutButton:
                    return ButtonFoldout;
                default:
                    return CompactButton;
            }
        }

        public static GUIStyle CreateDisplayAsStringStyle(DisplayAsStringAttribute das)
        {
            EnsureSkin();
            long key = ((long)(das.Overflow ? 1 : 0) << 40)
                | ((long)(das.EnableRichText ? 1 : 0) << 39)
                | ((long)(int)das.Alignment << 32)
                | (uint)das.FontSize;
            if (s_displayAsStringCache.TryGetValue(key, out var cached)) return cached;
            var style = new GUIStyle(EditorStyles.label)
            {
                wordWrap = !das.Overflow,
                richText = das.EnableRichText,
                alignment = das.Alignment == TextAlignment.Center ? TextAnchor.MiddleCenter
                    : das.Alignment == TextAlignment.Right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft,
            };
            if (das.FontSize > 0) style.fontSize = das.FontSize;
            s_displayAsStringCache[key] = style;
            return style;
        }

        public static GUIStyle CreateProgressBarLabelStyle(ProgressBarAttribute pb)
        {
            EnsureSkin();
            if (pb.ValueLabelAlignment == TextAlignment.Left)
            {
                if (s_progressBarLabelLeft == null)
                    s_progressBarLabelLeft = MakeProgressLabel(TextAnchor.MiddleLeft);
                return s_progressBarLabelLeft;
            }
            if (pb.ValueLabelAlignment == TextAlignment.Right)
            {
                if (s_progressBarLabelRight == null)
                    s_progressBarLabelRight = MakeProgressLabel(TextAnchor.MiddleRight);
                return s_progressBarLabelRight;
            }
            if (s_progressBarLabelCenter == null)
                s_progressBarLabelCenter = MakeProgressLabel(TextAnchor.MiddleCenter);
            return s_progressBarLabelCenter;
        }

        private static GUIStyle MakeProgressLabel(TextAnchor anchor)
        {
            return new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = anchor,
                normal = { textColor = Color.white },
            };
        }

        public static GUIContent TempContent(string text)
        {
            s_tempContent.text = text;
            s_tempContent.tooltip = string.Empty;
            s_tempContent.image = null;
            return s_tempContent;
        }

        public static GUIContent TempContent(string text, string tooltip)
        {
            s_tempContent.text = text;
            s_tempContent.tooltip = tooltip ?? string.Empty;
            s_tempContent.image = null;
            return s_tempContent;
        }

        /// <summary>Cached 1×1 tint texture for rare GUIStyle background overrides.</summary>
        public static Texture2D GetTintTexture(Color color)
        {
            int key = ((byte)(color.r * 255f) << 24) | ((byte)(color.g * 255f) << 16)
                | ((byte)(color.b * 255f) << 8) | (byte)(color.a * 255f);
            if (s_tintTexCache.TryGetValue(key, out var tex) && tex != null) return tex;
            tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            tex.SetPixel(0, 0, color);
            tex.Apply(false, true);
            s_tintTexCache[key] = tex;
            return tex;
        }

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

        // --- Layout helpers --------------------------------------------------------------

        public static void BeginInspectorScope()
        {
            EditorGUILayout.Space(2f);
        }

        public static void EndInspectorScope()
        {
            EditorGUILayout.Space(2f);
        }

        // --- Container tracking ----------------------------------------------------------
        // EditorGUI.Foldout widens the rect it is handed leftwards while EditorGUIUtility.hierarchyMode
        // is on (always, inside an Inspector window) so root-level arrows hug the window edge while the
        // label still aligns with field labels. Inside a padded container the pull is bigger than the
        // padding, so the arrow lands on/outside the container border. Section headers therefore cancel
        // the pull whenever they are nested — containers declare themselves via ContainerScope.

        private static int s_containerDepth;

        /// <summary>
        /// Leftward pull <see cref="EditorGUI.Foldout(Rect,bool,GUIContent,bool,GUIStyle)"/> applies to
        /// the rect it is given. Mirrors Unity's internal expression; naturally 0 outside
        /// <see cref="EditorGUIUtility.hierarchyMode"/>, so EditorWindow call sites are unaffected.
        /// </summary>
        internal static float FoldoutHangOffset => EditorGUIUtility.hierarchyMode
            ? (int)(EditorStyles.foldout.padding.left - EditorStyles.label.padding.left)
            : 0f;

        /// <summary>Nesting depth of declared padded containers. 0 = inspector/window root.</summary>
        internal static int ContainerDepth => s_containerDepth;

        internal static void PushContainer() => s_containerDepth++;

        internal static void PopContainer()
        {
            if (s_containerDepth > 0) s_containerDepth--;
        }

        /// <summary>Domain-reload safety net; scopes are IDisposable so they balance on their own.</summary>
        internal static void ResetContainerDepth() => s_containerDepth = 0;

        /// <summary>
        /// Adjusts a layout rect before handing it to <c>EditorGUI.Foldout</c>: inside a declared
        /// container the hierarchyMode pull is cancelled so the header stays in bounds; at root the
        /// rect passes through unchanged and keeps Unity's flush-to-the-edge arrow.
        /// </summary>
        internal static Rect HeaderRect(Rect rect)
        {
            if (s_containerDepth > 0) rect.xMin += FoldoutHangOffset;
            return rect;
        }

        /// <summary>x the foldout arrow of <see cref="HeaderRect"/> actually lands at, for aligned chrome.</summary>
        internal static float HeaderArrowX(Rect rect)
            => s_containerDepth > 0 ? rect.x : rect.x - FoldoutHangOffset;

        /// <summary>
        /// Horizontal indent applied per nesting level of engine-drawn content (foldout/toggle/title
        /// bodies, list/dictionary rows, nested objects) — deliberately smaller than Unity's fixed
        /// ~15px <see cref="EditorGUI.indentLevel"/> step so deep nesting stays compact.
        /// </summary>
        public const float NestedIndentWidth = 8f;

        /// <summary>
        /// Compact nested-content indent: reserves <see cref="NestedIndentWidth"/> px via layout
        /// instead of Unity's fixed-width <see cref="EditorGUI.IndentLevelScope"/>. Purely cosmetic —
        /// does not affect <see cref="ContainerDepth"/>. Use <see cref="NestedGroupScope"/> instead for
        /// a chrome-free boundary (foldout/title body, inline recursion) that should also count as
        /// nested for header pull-cancel / rule-gating purposes.
        /// </summary>
        public sealed class NestedIndentScope : IDisposable
        {
            public NestedIndentScope()
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(NestedIndentWidth);
                GUILayout.BeginVertical();
            }

            public void Dispose()
            {
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// <see cref="NestedIndentScope"/> that also marks its content as nested (see
        /// <see cref="ContainerDepth"/>) — for chrome-free boundaries (a <c>[FoldoutGroup]</c> body, a
        /// <c>[TitleGroup(Indent = true)]</c> body, an <c>[InlineProperty]</c> recursion) where a
        /// further nested header still needs correct pull-cancel / rule-gating despite no helpBox.
        /// </summary>
        public sealed class NestedGroupScope : IDisposable
        {
            private readonly NestedIndentScope _indent;

            public NestedGroupScope()
            {
                PushContainer();
                _indent = new NestedIndentScope();
            }

            public void Dispose()
            {
                _indent.Dispose();
                PopContainer();
            }
        }

        /// <summary>
        /// helpBox container that also marks section headers drawn inside it as nested, so
        /// <c>[FoldoutGroup]</c> headers stay within the box instead of hanging over its left border.
        /// Use instead of <c>new EditorGUILayout.VerticalScope(EditorStyles.helpBox)</c> around
        /// inspector content.
        /// </summary>
        public sealed class ContainerScope : IDisposable
        {
            private readonly EditorGUILayout.VerticalScope _scope;

            public ContainerScope(GUIStyle style, params GUILayoutOption[] options)
            {
                _scope = new EditorGUILayout.VerticalScope(style ?? BoxContainerStyle,
                    options ?? Array.Empty<GUILayoutOption>());
                PushContainer();
            }

            /// <summary>Begins a container with no style.</summary>
            public ContainerScope() : this(null) { }

            public void Dispose()
            {
                PopContainer();
                _scope.Dispose();
            }
        }

        public static void BeginSection()
        {
            EditorGUILayout.BeginVertical(BoxContainerStyle);
            PushContainer();
        }

        public static void EndSection()
        {
            PopContainer();
            EditorGUILayout.EndVertical();
        }

        public static void BeginBox(string label)
        {
            BeginSection();
            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.LabelField(label, SectionTitle);
        }

        /// <summary>Begins a box with no label.</summary>
        public static void BeginBox() => BeginBox(null);

        public static void EndBox() => EndSection();

        public static void BeginBoxHeader()
        {
            var rect = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(rect, BoxHeaderBackground);
        }

        public static void EndBoxHeader() => EditorGUILayout.EndHorizontal();

        public static bool Foldout(bool expanded, string label)
            => SectionFoldout(expanded, label);

        public static bool Foldout(bool expanded, GUIContent label)
            => SectionFoldout(expanded, label);

        /// <summary>
        /// Flat section header — used by <c>[FoldoutGroup]</c>. Renders through the same
        /// <see cref="FlatFoldoutStyle"/> as list/nested foldouts so the arrow + label land at
        /// the identical x for a given indent level. A single subtle bottom rule at top-level
        /// depth separates top sections; nested foldouts stay chrome-free.
        /// </summary>
        public static bool SectionFoldout(bool expanded, string label)
            => SectionFoldout(expanded, TempContent(label));

        public static bool SectionFoldout(bool expanded, GUIContent label)
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            var headerRect = HeaderRect(rect);

            if (ContainerDepth <= 0)
            {
                float ruleX = HeaderArrowX(headerRect);
                EditorGUI.DrawRect(new Rect(ruleX, rect.yMax - 1f, rect.xMax - ruleX, 1f), SectionRuleColor);
            }

            return EditorGUI.Foldout(headerRect, expanded, label, true, FlatFoldoutStyle);
        }

        /// <summary>
        /// Runs <paramref name="drawContent"/> in a horizontal row with a trailing help-icon tooltip —
        /// the <c>[TooltipIcon]</c> attribute's composition helper, shared by <c>RenderField</c> (serialized
        /// fields) and <c>PocoInspector.DrawValue</c> (<c>[ShowInInspector]</c> members) so the field itself
        /// can still go through its normal (possibly attribute-specific) rendering while gaining the icon.
        /// </summary>
        public static void DrawWithTooltipIcon(Action drawContent, string tooltip)
        {
            if (string.IsNullOrWhiteSpace(tooltip)) { drawContent(); return; }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    drawContent();
                }
                AetherNexus.FoundationPlatform.Editor.Utilities.AuthoringUxShared.DrawTooltipIcon(tooltip);
            }
        }

        // Compact layout indent (NestedIndentWidth) + ContainerDepth push instead of Unity's fixed
        // ~15px EditorGUI.indentLevel step, so a foldout nested under this body (chrome-free, no
        // helpBox) still cancels its own header pull / rule correctly. Exposed as a Begin/End pair
        // (not NestedGroupScope's IDisposable) since existing callers hold it open across a
        // try/finally rather than a using block.
        public static void BeginSectionFoldoutBody()
        {
            PushContainer();
            GUILayout.BeginHorizontal();
            GUILayout.Space(NestedIndentWidth);
            GUILayout.BeginVertical();
        }

        public static void EndSectionFoldoutBody()
        {
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            PopContainer();
        }

        /// <summary>
        /// Rect-based flat section header for hand-laid drawers (that compute their own rects in
        /// GetPropertyHeight). Draws a flat foldout (arrow + bold label) through <see cref="FlatFoldoutStyle"/>
        /// — matching layout foldouts — and reserves <paramref name="trailingButtons"/> square slots on the
        /// right for caller-drawn buttons. No dark strip. <paramref name="trailingRects"/> is right-to-left:
        /// index 0 is the rightmost button.
        /// </summary>
        public static bool SectionHeaderRow(Rect rect, GUIContent label, bool expanded, int trailingButtons,
            out Rect[] trailingRects, float buttonSize)
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
            // Only the foldout cell is adjusted for nesting; trailing button rects stay pinned to rect.xMax.
            var foldRect = HeaderRect(new Rect(rect.x, rect.y, foldWidth, rect.height));
            return EditorGUI.Foldout(foldRect, expanded, label, true, FlatFoldoutStyle);
        }

        /// <summary>Rect-based flat section header, using a 20px button size.</summary>
        public static bool SectionHeaderRow(Rect rect, GUIContent label, bool expanded, int trailingButtons,
            out Rect[] trailingRects) => SectionHeaderRow(rect, label, expanded, trailingButtons, out trailingRects, 20f);

        /// <summary>
        /// Themed tag/chip pill: <see cref="TagChipBackground"/> fill, accent stripe, <see cref="TagChipOutline"/>
        /// border, and an optional "×" remove button. Skin-aware; no baked colors.
        /// </summary>
        public static void DrawTagPill(Rect rect, GUIContent content, Color accent, Action onRemove)
        {
            DrawRoundedRect(rect, TagChipBackground, 3f);
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

        public static void DrawTitle(string title) => DrawTitle(title, null);

        /// <summary>Draws a title, using left alignment, a horizontal rule, and a bold label.</summary>
        public static void DrawTitle(string title, string subtitle) => DrawTitle(title, subtitle, TextAlignment.Left, true, true);

        public static void DrawTitle(string title, string subtitle, TextAlignment textAlignment,
            bool horizontalLine, bool boldLabel)
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            GUI.Label(rect, title, TitleStyle(textAlignment, boldLabel));
            if (!string.IsNullOrEmpty(subtitle))
                GUI.Label(rect, subtitle, SectionSubtitle);
            if (horizontalLine)
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax, rect.width, 1f), SectionRuleColor);
            EditorGUILayout.Space(2f);
        }

        public static void DrawInfoBox(string message, InfoMessageType type)
        {
            bool dummy = true;
            DrawInfoBox(message, type, ref dummy, collapsible: false);
        }

        /// <summary>Draws an info box using InfoMessageType.Info.</summary>
        public static void DrawInfoBox(string message) => DrawInfoBox(message, InfoMessageType.Info);

        /// <summary>Themed callout matching AetherInspector chrome.</summary>
        public static void DrawInfoBox(string message, InfoMessageType type, ref bool expanded, bool collapsible)
        {
            if (string.IsNullOrEmpty(message)) return;
            EnsureSkin();
            if (s_infoBoxLabel == null)
                s_infoBoxLabel = new GUIStyle(EditorStyles.wordWrappedLabel) { padding = new RectOffset(8, 8, 6, 6) };
            if (s_infoBoxCollapsedLabel == null)
                s_infoBoxCollapsedLabel = new GUIStyle(EditorStyles.label) { wordWrap = false };

            float wrapW = EditorGUIUtility.currentViewWidth - 24f;

            if (collapsible && !expanded)
            {
                var headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                EditorGUI.DrawRect(headerRect, InfoBoxBackground(type));
                DrawRectOutline(headerRect, InfoBoxBorder(type));

                var prevIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                var labelRect = new Rect(headerRect.x + 6f, headerRect.y + 2f, headerRect.width - 12f, headerRect.height - 4f);
                int nl = message.IndexOf('\n');
                string firstLine = nl >= 0 ? message.Substring(0, nl) : message;
                if (firstLine.Length > 64) firstLine = firstLine.Substring(0, 64) + "...";
                else if (nl >= 0) firstLine += "...";
                GUI.Label(labelRect, firstLine, s_infoBoxCollapsedLabel);
                EditorGUI.indentLevel = prevIndent;

                if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
                {
                    expanded = true;
                    Event.current.Use();
                    GUI.changed = true;
                }

                EditorGUILayout.Space(SectionSpacing * 0.5f);
                return;
            }

            float h = s_infoBoxLabel.CalcHeight(TempContent(message), wrapW);
            var rect = EditorGUILayout.GetControlRect(false, h + 8f);
            EditorGUI.DrawRect(rect, InfoBoxBackground(type));
            DrawRectOutline(rect, InfoBoxBorder(type));
            GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, rect.height - 8f), message, s_infoBoxLabel);

            if (collapsible && Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                expanded = false;
                Event.current.Use();
                GUI.changed = true;
            }

            if (!collapsible) EditorGUILayout.Space(SectionSpacing * 0.5f);
        }

        private static string TruncateWithEllipsis(string text, float maxWidth, GUIStyle style)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (maxWidth <= 0f) return text;
            if (style.CalcSize(new GUIContent(text)).x <= maxWidth) return text;
            while (text.Length > 0 && style.CalcSize(new GUIContent(text + "...")).x > maxWidth)
            {
                text = text.Substring(0, text.Length - 1);
            }
            return text + "...";
        }

        public static void DrawValidationBox(string message, InfoMessageType type)
            => DrawInfoBox(message, type);

        /// <summary>Draws a validation box using InfoMessageType.Error.</summary>
        public static void DrawValidationBox(string message) => DrawValidationBox(message, InfoMessageType.Error);

        /// <summary>Skin-aware toolbar for tab groups.</summary>
        public static int Toolbar(int selected, string[] labels)
        {
            EnsureSkin();
            if (s_toolbarButton == null)
                s_toolbarButton = new GUIStyle(EditorStyles.toolbarButton) { fixedHeight = 22f };
            return GUILayout.Toolbar(selected, labels, s_toolbarButton);
        }

        public static int Toolbar(Rect rect, int selected, string[] labels)
        {
            EnsureSkin();
            if (s_toolbarButton == null)
                s_toolbarButton = new GUIStyle(EditorStyles.toolbarButton) { fixedHeight = 22f };
            return GUI.Toolbar(rect, selected, labels, s_toolbarButton);
        }

        // --- Zero-alloc chrome drawing ---------------------------------------------------

        public static void DrawRoundedRect(Rect rect, Color fill, float radius)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (radius <= 0.5f || rect.width < 2f || rect.height < 2f)
            {
                EditorGUI.DrawRect(rect, fill);
                return;
            }
            radius = Mathf.Min(radius, rect.width * 0.5f, rect.height * 0.5f);
            EditorGUI.DrawRect(new Rect(rect.x + radius, rect.y, rect.width - radius * 2f, rect.height), fill);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + radius, radius, rect.height - radius * 2f), fill);
            EditorGUI.DrawRect(new Rect(rect.xMax - radius, rect.y + radius, radius, rect.height - radius * 2f), fill);
            Handles.BeginGUI();
            Handles.color = fill;
            Handles.DrawSolidDisc(new Vector3(rect.x + radius, rect.y + radius, 0f), Vector3.forward, radius);
            Handles.DrawSolidDisc(new Vector3(rect.xMax - radius, rect.y + radius, 0f), Vector3.forward, radius);
            Handles.DrawSolidDisc(new Vector3(rect.x + radius, rect.yMax - radius, 0f), Vector3.forward, radius);
            Handles.DrawSolidDisc(new Vector3(rect.xMax - radius, rect.yMax - radius, 0f), Vector3.forward, radius);
            Handles.EndGUI();
        }

        public static void DrawDropdownCaret(Rect fieldRect)
        {
            if (Event.current.type != EventType.Repaint) return;
            float cx = fieldRect.xMax - 10f;
            float cy = fieldRect.y + fieldRect.height * 0.5f;
            s_caretVerts[0] = new Vector3(cx - 4f, cy - 2f, 0f);
            s_caretVerts[1] = new Vector3(cx + 4f, cy - 2f, 0f);
            s_caretVerts[2] = new Vector3(cx, cy + 3f, 0f);
            Handles.BeginGUI();
            Handles.color = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.65f) : new Color(0f, 0f, 0f, 0.55f);
            Handles.DrawAAConvexPolygon(s_caretVerts);
            Handles.EndGUI();
        }

        public static void DrawFieldChrome(Rect rect)
        {
            EditorGUI.DrawRect(rect, FieldBackground);
            DrawRectOutline(rect, FieldBorder);
        }

        public static bool DrawStyledDropdown(Rect rect, GUIContent label, string currentText)
        {
            DrawFieldChrome(rect);
            GUI.Label(rect, currentText, DropdownFieldStyle);
            DrawDropdownCaret(rect);
            return EditorGUI.DropdownButton(rect, GUIContent.none, FocusType.Keyboard, GUIStyle.none);
        }

        public static float DrawStyledSlider(Rect rect, float value, float min, float max,
            Color track, Color fill, Color thumb)
        {
            float trackH = 4f;
            float thumbR = 6f;
            var trackRect = new Rect(rect.x, rect.y + (rect.height - trackH) * 0.5f, rect.width, trackH);
            EditorGUI.DrawRect(trackRect, track);
            float t = Mathf.InverseLerp(min, max, value);
            float fillW = trackRect.width * t;
            if (fillW > 0f)
                EditorGUI.DrawRect(new Rect(trackRect.x, trackRect.y, fillW, trackRect.height), fill);
            float thumbX = trackRect.x + fillW;
            if (Event.current.type == EventType.Repaint)
            {
                Handles.BeginGUI();
                Handles.color = thumb;
                Handles.DrawSolidDisc(new Vector3(thumbX, trackRect.y + trackH * 0.5f, 0f), Vector3.forward, thumbR);
                Handles.EndGUI();
            }

            int id = GUIUtility.GetControlID(FocusType.Passive);
            var evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (rect.Contains(evt.mousePosition) && GUI.enabled)
                    {
                        GUIUtility.hotControl = id;
                        value = Mathf.Lerp(min, max, Mathf.Clamp01((evt.mousePosition.x - rect.x) / Mathf.Max(1f, rect.width)));
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        value = Mathf.Lerp(min, max, Mathf.Clamp01((evt.mousePosition.x - rect.x) / Mathf.Max(1f, rect.width)));
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id) { GUIUtility.hotControl = 0; evt.Use(); }
                    break;
            }
            return value;
        }

        public static void DrawStyledMinMaxSlider(Rect rect, ref float minValue, ref float maxValue,
            float limitMin, float limitMax, Color track, Color fill, Color thumb)
        {
            float trackH = 4f;
            float thumbR = 6f;
            var trackRect = new Rect(rect.x, rect.y + (rect.height - trackH) * 0.5f, rect.width, trackH);
            EditorGUI.DrawRect(trackRect, track);
            float t0 = Mathf.InverseLerp(limitMin, limitMax, minValue);
            float t1 = Mathf.InverseLerp(limitMin, limitMax, maxValue);
            float x0 = trackRect.x + trackRect.width * t0;
            float x1 = trackRect.x + trackRect.width * t1;
            EditorGUI.DrawRect(new Rect(x0, trackRect.y, Mathf.Max(0f, x1 - x0), trackRect.height), fill);
            if (Event.current.type == EventType.Repaint)
            {
                Handles.BeginGUI();
                Handles.color = thumb;
                Handles.DrawSolidDisc(new Vector3(x0, trackRect.y + trackH * 0.5f, 0f), Vector3.forward, thumbR);
                Handles.DrawSolidDisc(new Vector3(x1, trackRect.y + trackH * 0.5f, 0f), Vector3.forward, thumbR);
                Handles.EndGUI();
            }

            int id = GUIUtility.GetControlID(FocusType.Passive);
            var evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (rect.Contains(evt.mousePosition) && GUI.enabled)
                    {
                        GUIUtility.hotControl = id;
                        float mx = evt.mousePosition.x;
                        float d0 = Mathf.Abs(mx - x0);
                        float d1 = Mathf.Abs(mx - x1);
                        s_minmaxDragNear = d0 <= d1;
                        ApplyMinMaxDrag(rect, ref minValue, ref maxValue, limitMin, limitMax, s_minmaxDragNear);
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        ApplyMinMaxDrag(rect, ref minValue, ref maxValue, limitMin, limitMax, s_minmaxDragNear);
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id) { GUIUtility.hotControl = 0; evt.Use(); }
                    break;
            }
        }

        private static bool s_minmaxDragNear;

        private static void ApplyMinMaxDrag(Rect rect, ref float minValue, ref float maxValue,
            float limitMin, float limitMax, bool near)
        {
            float v = Mathf.Lerp(limitMin, limitMax, Mathf.Clamp01((Event.current.mousePosition.x - rect.x) / Mathf.Max(1f, rect.width)));
            if (near)
            {
                minValue = Mathf.Clamp(v, limitMin, maxValue);
            }
            else
            {
                maxValue = Mathf.Clamp(v, minValue, limitMax);
            }
        }

        public static bool DrawToggleSwitch(Rect rect, bool value)
        {
            float h = 16f;
            float w = 28f;
            var switchRect = new Rect(rect.x, rect.y + (rect.height - h) * 0.5f, w, h);
            DrawRoundedRect(switchRect, value ? ToggleTrackOn : ToggleTrackOff, h * 0.5f);
            float thumbR = 5.5f;
            float tx = value ? switchRect.xMax - thumbR - 2f : switchRect.x + thumbR + 2f;
            if (Event.current.type == EventType.Repaint)
            {
                Handles.BeginGUI();
                Handles.color = ToggleThumb;
                Handles.DrawSolidDisc(new Vector3(tx, switchRect.y + h * 0.5f, 0f), Vector3.forward, thumbR);
                Handles.EndGUI();
            }
            if (GUI.enabled && Event.current.type == EventType.MouseDown && switchRect.Contains(Event.current.mousePosition))
            {
                value = !value;
                GUI.changed = true;
                Event.current.Use();
            }
            return value;
        }

        public static bool DrawToggleSwitchLeft(Rect rect, GUIContent label, bool value)
        {
            const float switchW = 30f;
            var switchRect = new Rect(rect.x, rect.y, switchW, rect.height);
            bool next = DrawToggleSwitch(switchRect, value);
            var labelRect = new Rect(rect.x + switchW + 6f, rect.y, rect.width - switchW - 6f, rect.height);
            GUI.Label(labelRect, label, FlatHeaderLabel);
            return next;
        }

        public static float DrawKnob(Rect rect, float value, float min, float max)
        {
            float size = Mathf.Min(rect.width, rect.height, 56f);
            var knobRect = new Rect(rect.x, rect.y, size, size);
            Vector2 center = knobRect.center;
            float radius = size * 0.42f;
            if (Event.current.type == EventType.Repaint)
            {
                Handles.BeginGUI();
                Handles.color = FieldBackground;
                Handles.DrawSolidDisc(center, Vector3.forward, radius);
                Handles.color = FieldBorder;
                Handles.DrawWireDisc(center, Vector3.forward, radius);
                float t = Mathf.InverseLerp(min, max, value);
                float ang = Mathf.Lerp(135f, -135f, t) * Mathf.Deg2Rad;
                var tip = center + new Vector2(Mathf.Cos(ang), -Mathf.Sin(ang)) * (radius - 4f);
                Handles.color = Accent;
                Handles.DrawAAPolyLine(3f, center, tip);
                Handles.EndGUI();
            }

            int id = GUIUtility.GetControlID(FocusType.Passive);
            var evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (knobRect.Contains(evt.mousePosition) && GUI.enabled)
                    {
                        GUIUtility.hotControl = id;
                        value = KnobValueFromMouse(center, min, max);
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        value = KnobValueFromMouse(center, min, max);
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id) { GUIUtility.hotControl = 0; evt.Use(); }
                    break;
            }
            return value;
        }

        private static float KnobValueFromMouse(Vector2 center, float min, float max)
        {
            Vector2 d = Event.current.mousePosition - center;
            float deg = Mathf.Atan2(-d.y, d.x) * Mathf.Rad2Deg;
            // Map 135..-135 (clockwise-ish through bottom) to 0..1
            float clamped = Mathf.Clamp(deg, -135f, 135f);
            float t = Mathf.InverseLerp(135f, -135f, clamped);
            return Mathf.Lerp(min, max, t);
        }

        public static bool DrawRoundedButton(Rect rect, GUIContent content, GUIStyle style)
        {
            if (Event.current.type == EventType.Repaint)
                DrawRoundedRect(rect, TabIdleBackground, 4f);
            return GUI.Button(rect, content, style ?? CompactButton);
        }
    }
}
#endif
