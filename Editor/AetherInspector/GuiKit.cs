#if UNITY_EDITOR
using System;
using AetherNexus.FoundationPlatform.AetherInspector;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.AetherInspector.Editor
{
    /// <summary>
    /// Public facade over <see cref="AetherInspectorTheme"/> for editor windows outside the inspector engine.
    /// </summary>
    public static class GuiKit
    {
        public static void BeginBox(string label = null) => AetherInspectorTheme.BeginBox(label);

        public static void EndBox() => AetherInspectorTheme.EndBox();

        public static void BeginBoxHeader() => AetherInspectorTheme.BeginBoxHeader();

        public static void EndBoxHeader() => AetherInspectorTheme.EndBoxHeader();

        public static bool Foldout(bool expanded, string label)
            => AetherInspectorTheme.Foldout(expanded, label);

        public static bool Foldout(bool expanded, GUIContent label)
            => AetherInspectorTheme.Foldout(expanded, label);

        public static bool SectionFoldout(bool expanded, string label)
            => AetherInspectorTheme.SectionFoldout(expanded, label);

        public static bool SectionFoldout(bool expanded, GUIContent label)
            => AetherInspectorTheme.SectionFoldout(expanded, label);

        public static void BeginSectionFoldoutBody() => AetherInspectorTheme.BeginSectionFoldoutBody();

        public static void EndSectionFoldoutBody() => AetherInspectorTheme.EndSectionFoldoutBody();

        public static bool FoldoutInSection(bool expanded, string label)
            => AetherInspectorTheme.FoldoutInSection(expanded, label);

        public static bool FoldoutInSection(bool expanded, GUIContent label)
            => AetherInspectorTheme.FoldoutInSection(expanded, label);

        public static void Title(string title) => AetherInspectorTheme.DrawTitle(title);

        public static void Title(string title, string subtitle, TextAlignment textAlignment = TextAlignment.Left,
            bool horizontalLine = true, bool boldLabel = true)
            => AetherInspectorTheme.DrawTitle(title, subtitle, textAlignment, horizontalLine, boldLabel);

        public static void InfoBox(string message, InfoMessageType type = InfoMessageType.Info)
            => AetherInspectorTheme.DrawInfoBox(message, type);

        public static void ValidationBox(string message, InfoMessageType type = InfoMessageType.Error)
            => AetherInspectorTheme.DrawValidationBox(message, type);

        public static int Toolbar(int selected, string[] labels)
            => AetherInspectorTheme.Toolbar(selected, labels);

        /// <summary>Themed tag/chip pill with optional "×" remove button. See <see cref="AetherInspectorTheme.DrawTagPill"/>.</summary>
        public static void TagPill(Rect rect, GUIContent content, Color accent, Action onRemove = null)
            => AetherInspectorTheme.DrawTagPill(rect, content, accent, onRemove);

        /// <summary>Fallback accent for a tag/chip with no metadata color.</summary>
        public static Color TagAccentFallback => AetherInspectorTheme.TagChipAccentFallback;

        /// <summary>Amber accent for an unknown / out-of-scope tag.</summary>
        public static Color TagWarningAccent => AetherInspectorTheme.TagWarningAccent;

        /// <summary>1px border color for chips / color swatches.</summary>
        public static Color ChipOutline => AetherInspectorTheme.TagChipOutline;

        /// <summary>Square header button style (e.g. "+").</summary>
        public static GUIStyle HeaderButton => AetherInspectorTheme.HeaderButton;

        /// <summary>Draw a 1px outline around a rect.</summary>
        public static void RectOutline(Rect rect, Color color)
            => AetherInspectorTheme.DrawRectOutline(rect, color);

        /// <summary>
        /// Rect-based flat section header (arrow + bold label) reserving <paramref name="trailingButtons"/>
        /// right-side button slots. <paramref name="trailingRects"/> index 0 is rightmost.
        /// See <see cref="AetherInspectorTheme.SectionHeaderRow"/>.
        /// </summary>
        public static bool SectionHeaderRow(Rect rect, GUIContent label, bool expanded, int trailingButtons,
            out Rect[] trailingRects, float buttonSize = 20f)
            => AetherInspectorTheme.SectionHeaderRow(rect, label, expanded, trailingButtons, out trailingRects, buttonSize);
    }
}
#endif
