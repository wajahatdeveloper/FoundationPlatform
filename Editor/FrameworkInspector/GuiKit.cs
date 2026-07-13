#if UNITY_EDITOR
using System;
using FoundationPlatform.FrameworkInspector;
using UnityEngine;

namespace FoundationPlatform.FrameworkInspector.Editor
{
    /// <summary>
    /// Public facade over <see cref="FrameworkInspectorTheme"/> for editor windows outside the inspector engine.
    /// </summary>
    public static class GuiKit
    {
        public static void BeginBox(string label = null) => FrameworkInspectorTheme.BeginBox(label);

        public static void EndBox() => FrameworkInspectorTheme.EndBox();

        public static void BeginBoxHeader() => FrameworkInspectorTheme.BeginBoxHeader();

        public static void EndBoxHeader() => FrameworkInspectorTheme.EndBoxHeader();

        public static bool Foldout(bool expanded, string label)
            => FrameworkInspectorTheme.Foldout(expanded, label);

        public static bool Foldout(bool expanded, GUIContent label)
            => FrameworkInspectorTheme.Foldout(expanded, label);

        public static bool SectionFoldout(bool expanded, string label)
            => FrameworkInspectorTheme.SectionFoldout(expanded, label);

        public static bool SectionFoldout(bool expanded, GUIContent label)
            => FrameworkInspectorTheme.SectionFoldout(expanded, label);

        public static void BeginSectionFoldoutBody() => FrameworkInspectorTheme.BeginSectionFoldoutBody();

        public static void EndSectionFoldoutBody() => FrameworkInspectorTheme.EndSectionFoldoutBody();

        public static bool FoldoutInSection(bool expanded, string label)
            => FrameworkInspectorTheme.FoldoutInSection(expanded, label);

        public static bool FoldoutInSection(bool expanded, GUIContent label)
            => FrameworkInspectorTheme.FoldoutInSection(expanded, label);

        public static void Title(string title) => FrameworkInspectorTheme.DrawTitle(title);

        public static void Title(string title, string subtitle, TextAlignment textAlignment = TextAlignment.Left,
            bool horizontalLine = true, bool boldLabel = true)
            => FrameworkInspectorTheme.DrawTitle(title, subtitle, textAlignment, horizontalLine, boldLabel);

        public static void InfoBox(string message, InfoMessageType type = InfoMessageType.Info)
            => FrameworkInspectorTheme.DrawInfoBox(message, type);

        public static void ValidationBox(string message, InfoMessageType type = InfoMessageType.Error)
            => FrameworkInspectorTheme.DrawValidationBox(message, type);

        public static int Toolbar(int selected, string[] labels)
            => FrameworkInspectorTheme.Toolbar(selected, labels);

        /// <summary>Themed tag/chip pill with optional "×" remove button. See <see cref="FrameworkInspectorTheme.DrawTagPill"/>.</summary>
        public static void TagPill(Rect rect, GUIContent content, Color accent, Action onRemove = null)
            => FrameworkInspectorTheme.DrawTagPill(rect, content, accent, onRemove);

        /// <summary>Fallback accent for a tag/chip with no metadata color.</summary>
        public static Color TagAccentFallback => FrameworkInspectorTheme.TagChipAccentFallback;

        /// <summary>Amber accent for an unknown / out-of-scope tag.</summary>
        public static Color TagWarningAccent => FrameworkInspectorTheme.TagWarningAccent;

        /// <summary>1px border color for chips / color swatches.</summary>
        public static Color ChipOutline => FrameworkInspectorTheme.TagChipOutline;

        /// <summary>Square header button style (e.g. "+").</summary>
        public static GUIStyle HeaderButton => FrameworkInspectorTheme.HeaderButton;

        /// <summary>Draw a 1px outline around a rect.</summary>
        public static void RectOutline(Rect rect, Color color)
            => FrameworkInspectorTheme.DrawRectOutline(rect, color);

        /// <summary>
        /// Rect-based flat section header (arrow + bold label) reserving <paramref name="trailingButtons"/>
        /// right-side button slots. <paramref name="trailingRects"/> index 0 is rightmost.
        /// See <see cref="FrameworkInspectorTheme.SectionHeaderRow"/>.
        /// </summary>
        public static bool SectionHeaderRow(Rect rect, GUIContent label, bool expanded, int trailingButtons,
            out Rect[] trailingRects, float buttonSize = 20f)
            => FrameworkInspectorTheme.SectionHeaderRow(rect, label, expanded, trailingButtons, out trailingRects, buttonSize);
    }
}
#endif
