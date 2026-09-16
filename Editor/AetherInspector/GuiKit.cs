#if UNITY_EDITOR
using System;
using AetherNexus.FoundationPlatform.AetherInspector;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.AetherInspector.Editor
{
    /// <summary>
    /// Public facade over <see cref="AetherInspectorTheme"/> for editor windows outside the inspector engine.
    /// </summary>
    public static class GuiKit
    {
        public static void BeginBox(string label) => AetherInspectorTheme.BeginBox(label);

        /// <summary>Begins a box with no label.</summary>
        public static void BeginBox() => AetherInspectorTheme.BeginBox();

        public static void EndBox() => AetherInspectorTheme.EndBox();

        /// <summary>
        /// helpBox container for inspector content. Prefer this over
        /// <c>new EditorGUILayout.VerticalScope(EditorStyles.helpBox)</c>: it also declares the nesting,
        /// so <c>[FoldoutGroup]</c> / section headers drawn inside stay within the box instead of hanging
        /// over its left border. See <see cref="AetherInspectorTheme.ContainerScope"/>.
        /// </summary>
        public static AetherInspectorTheme.ContainerScope Container(GUIStyle style,
            params GUILayoutOption[] options)
            => new AetherInspectorTheme.ContainerScope(style, options);

        /// <summary>Begins a container with no style.</summary>
        public static AetherInspectorTheme.ContainerScope Container() => Container(null);

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
            => AetherInspectorTheme.SectionFoldout(expanded, label);

        public static bool FoldoutInSection(bool expanded, GUIContent label)
            => AetherInspectorTheme.SectionFoldout(expanded, label);

        /// <summary>
        /// The canonical section for a hand-written editor: flat foldout header plus an indented body,
        /// with expansion persisted per <paramref name="stateKey"/> across domain reloads. This is the
        /// replacement for <c>LabelField(label, EditorStyles.boldLabel)</c> followed by loose content —
        /// it matches what <c>[FoldoutGroup]</c> renders, so attribute-driven and hand-drawn sections in
        /// the same inspector line up.
        /// </summary>
        public static SectionScope Section(string stateKey, string label)
            => new SectionScope(stateKey, new GUIContent(label), true);

        /// <summary>Section that starts collapsed the first time it is seen — use for Advanced / Debug.</summary>
        public static SectionScope CollapsedSection(string stateKey, string label)
            => new SectionScope(stateKey, new GUIContent(label), false);

        /// <summary>Horizontal run of action buttons, used for the Primary action and Fixes rows.</summary>
        public static EditorGUILayout.HorizontalScope ActionRow() => new EditorGUILayout.HorizontalScope();

        /// <summary>Themed action button. Label must come from the shared action vocabulary.</summary>
        public static bool ActionButton(string label)
            => GUILayout.Button(label, AetherInspectorTheme.CompactButton);

        public static bool ActionButton(string label, float width)
            => GUILayout.Button(label, AetherInspectorTheme.CompactButton, GUILayout.Width(width));

        /// <summary>Action button that is greyed out while <paramref name="enabled"/> is false.</summary>
        public static bool ActionButton(string label, bool enabled)
        {
            using (new EditorGUI.DisabledScope(!enabled))
            {
                return ActionButton(label);
            }
        }

        public struct SectionScope : IDisposable
        {
            private readonly bool _expanded;

            public bool Expanded => _expanded;

            public SectionScope(string stateKey, GUIContent label, bool expandedByDefault)
            {
                var key = "GuiKit.Section." + stateKey;
                _expanded = AetherInspectorTheme.SectionFoldout(
                    SessionState.GetBool(key, expandedByDefault), label);
                SessionState.SetBool(key, _expanded);
                if (_expanded)
                    AetherInspectorTheme.BeginSectionFoldoutBody();
            }

            public void Dispose()
            {
                if (_expanded)
                    AetherInspectorTheme.EndSectionFoldoutBody();
            }
        }

        public static void Title(string title) => AetherInspectorTheme.DrawTitle(title);

        public static void Title(string title, string subtitle) => AetherInspectorTheme.DrawTitle(title, subtitle);

        public static void Title(string title, string subtitle, TextAlignment textAlignment,
            bool horizontalLine, bool boldLabel)
            => AetherInspectorTheme.DrawTitle(title, subtitle, textAlignment, horizontalLine, boldLabel);

        public static void InfoBox(string message, InfoMessageType type)
            => AetherInspectorTheme.DrawInfoBox(message, type);

        /// <summary>Draws an info box using InfoMessageType.Info.</summary>
        public static void InfoBox(string message) => AetherInspectorTheme.DrawInfoBox(message);

        public static void ValidationBox(string message, InfoMessageType type)
            => AetherInspectorTheme.DrawValidationBox(message, type);

        /// <summary>Draws a validation box using InfoMessageType.Error.</summary>
        public static void ValidationBox(string message) => AetherInspectorTheme.DrawValidationBox(message);

        public static int Toolbar(int selected, string[] labels)
            => AetherInspectorTheme.Toolbar(selected, labels);

        /// <summary>Themed tag/chip pill with optional "×" remove button. See <see cref="AetherInspectorTheme.DrawTagPill"/>.</summary>
        public static void TagPill(Rect rect, GUIContent content, Color accent, Action onRemove)
            => AetherInspectorTheme.DrawTagPill(rect, content, accent, onRemove);

        /// <summary>Draws a tag pill with no remove button.</summary>
        public static void TagPill(Rect rect, GUIContent content, Color accent) => TagPill(rect, content, accent, null);

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
            out Rect[] trailingRects, float buttonSize)
            => AetherInspectorTheme.SectionHeaderRow(rect, label, expanded, trailingButtons, out trailingRects, buttonSize);

        /// <summary>Rect-based flat section header, using a 20px button size.</summary>
        public static bool SectionHeaderRow(Rect rect, GUIContent label, bool expanded, int trailingButtons,
            out Rect[] trailingRects) => AetherInspectorTheme.SectionHeaderRow(rect, label, expanded, trailingButtons, out trailingRects);

        /// <summary>
        /// Wraps custom-drawn layout content in <see cref="EditorGUI.BeginProperty"/> so a hand-drawn
        /// field keeps Unity's own property semantics: prefab-override bar and bold label, right-click
        /// Revert, and Preset apply/revert. Use for content that spans more than one row.
        /// </summary>
        public static PropertyBlockScope PropertyBlock(SerializedProperty property, GUIContent label)
            => new PropertyBlockScope(property, label);

        /// <summary>
        /// BeginProperty over a vertical layout group. The group rect comes from the previous layout
        /// pass, which is what the override bar needs (it only draws on Repaint).
        /// </summary>
        public struct PropertyBlockScope : IDisposable
        {
            /// <summary>The label BeginProperty handed back — bold when the value is a prefab override.</summary>
            public GUIContent Label { get; }

            public PropertyBlockScope(SerializedProperty property, GUIContent label)
            {
                var rect = EditorGUILayout.BeginVertical();
                // BeginProperty returns Unity's single shared temp GUIContent; any nested BeginProperty
                // (array/list drawers re-enter it for their own header) rewrites it in place, so the
                // scope has to keep its own copy or the label reads back blank at draw time.
                Label = new GUIContent(EditorGUI.BeginProperty(rect, label ?? GUIContent.none, property));
            }

            public void Dispose()
            {
                EditorGUI.EndProperty();
                EditorGUILayout.EndVertical();
            }
        }
    }
}
#endif
