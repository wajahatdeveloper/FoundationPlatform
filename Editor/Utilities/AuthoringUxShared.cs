using System;
using AetherNexus.FoundationPlatform.AetherInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities
{
    public static class AuthoringUxShared
    {
        public static void DrawSourceOfTruthInfo(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new InvalidOperationException("[AuthoringUX:ERROR] Source-of-truth message must be non-empty.");
            EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        public static void DrawSectionHeader(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new InvalidOperationException("[AuthoringUX:ERROR] Section title must be non-empty.");
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        public static void DrawReadOnlyPreview(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("[AuthoringUX:ERROR] Preview text must be non-empty.");
            EditorGUILayout.HelpBox(text, MessageType.None);
        }

        public static void DrawValidationSummary(int errorCount, int warningCount, int infoCount, string policyText)
        {
            if (errorCount < 0 || warningCount < 0 || infoCount < 0)
                throw new InvalidOperationException("[AuthoringUX:ERROR] Validation counts cannot be negative.");
            if (string.IsNullOrWhiteSpace(policyText))
                throw new InvalidOperationException("[AuthoringUX:ERROR] Validation policy text must be non-empty.");

            var messageType = MessageType.Info;
            if (errorCount > 0)
                messageType = MessageType.Error;
            else if (warningCount > 0)
                messageType = MessageType.Warning;

            EditorGUILayout.HelpBox(
                $"Validation: {errorCount} error(s), {warningCount} warning(s), {infoCount} info. {policyText}",
                messageType);
        }

        /// <summary>Draws a single validation issue row for shared scene/package validation UIs.</summary>
        public static void DrawValidationIssue(string source, string message, MessageType severity)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new InvalidOperationException("[AuthoringUX:ERROR] Validation issue source must be non-empty.");
            if (string.IsNullOrWhiteSpace(message))
                throw new InvalidOperationException("[AuthoringUX:ERROR] Validation issue message must be non-empty.");

            EditorGUILayout.HelpBox($"[{source}] {message}", severity);
        }

        public static int DrawActionStrip(string[] labels, bool[] enabled)
        {
            if (labels == null || enabled == null)
                throw new ArgumentNullException("[AuthoringUX:ERROR] Action strip labels/enabled arrays cannot be null.");
            if (labels.Length == 0)
                throw new InvalidOperationException("[AuthoringUX:ERROR] Action strip must contain at least one action.");
            if (labels.Length != enabled.Length)
                throw new InvalidOperationException("[AuthoringUX:ERROR] Action strip labels and enabled arrays must have equal length.");

            return DrawButtonGroup(labels, enabled);
        }

        /// <summary>
        /// Equal-width compact button row (fills inspector width). Returns clicked index, or -1.
        /// </summary>
        public static int DrawButtonGroup(params string[] labels) => DrawButtonGroup(labels, null);

        /// <summary>
        /// Equal-width compact button row. Optional per-button enabled flags (null = all enabled).
        /// Returns clicked index, or -1.
        /// </summary>
        public static int DrawButtonGroup(string[] labels, bool[] enabled)
        {
            if (labels == null)
                throw new ArgumentNullException(nameof(labels));
            if (labels.Length == 0)
                throw new InvalidOperationException("[AuthoringUX:ERROR] Button group must contain at least one action.");
            if (enabled != null && enabled.Length != labels.Length)
                throw new InvalidOperationException("[AuthoringUX:ERROR] Button group labels and enabled arrays must have equal length.");

            GUIStyle style = AetherInspectorTheme.CompactButton;
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(labels[i]))
                        throw new InvalidOperationException($"[AuthoringUX:ERROR] Button label at index {i} is empty.");

                    bool on = enabled == null || enabled[i];
                    using (new EditorGUI.DisabledScope(!on))
                    {
                        if (GUILayout.Button(labels[i], style))
                            return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>Single compact button that does not stretch full width.</summary>
        public static bool DrawCompactButton(string label) => DrawCompactButton(label, true, false);

        /// <summary>Single compact button with enabled flag; does not stretch full width.</summary>
        public static bool DrawCompactButton(string label, bool enabled) => DrawCompactButton(label, enabled, false);

        /// <summary>Single compact button. Set expandWidth true for primary full-row actions.</summary>
        public static bool DrawCompactButton(string label, bool enabled, bool expandWidth)
        {
            if (string.IsNullOrWhiteSpace(label))
                throw new InvalidOperationException("[AuthoringUX:ERROR] Compact button label must be non-empty.");

            using (new EditorGUI.DisabledScope(!enabled))
            {
                return GUILayout.Button(
                    label,
                    AetherInspectorTheme.CompactButton,
                    GUILayout.ExpandWidth(expandWidth));
            }
        }

        private static Texture2D s_HelpIcon;
        private static Texture2D HelpIcon
        {
            get
            {
                if (s_HelpIcon == null)
                {
                    var iconContent = EditorGUIUtility.IconContent("_Help");
                    if (iconContent != null)
                        s_HelpIcon = iconContent.image as Texture2D;
                }
                return s_HelpIcon;
            }
        }

        public static void DrawTooltipIcon(Rect rect, string tooltip)
        {
            if (string.IsNullOrWhiteSpace(tooltip))
                return;

            GUIContent content = new GUIContent(HelpIcon, tooltip);
            GUI.Label(rect, content, GUIStyle.none);
        }

        public static void DrawTooltipIcon(string tooltip, float size)
        {
            if (string.IsNullOrWhiteSpace(tooltip))
                return;

            GUIContent content = new GUIContent(HelpIcon, tooltip);
            GUILayout.Label(content, GUIStyle.none, GUILayout.Width(size), GUILayout.Height(size));
        }

        /// <summary>Draws the tooltip icon at a size of 16.</summary>
        public static void DrawTooltipIcon(string tooltip) => DrawTooltipIcon(tooltip, 16f);

        /// <summary>
        /// Collapsed-by-default foldout for nested asset inspectors on manager surfaces.
        /// Caller owns persistence of <paramref name="open"/> (default false).
        /// </summary>
        public static void DrawEditDetailsFoldout(ref bool open, Action draw)
        {
            if (draw == null)
                throw new ArgumentNullException(nameof(draw));

            open = EditorGUILayout.Foldout(open, "Edit details", true);
            if (!open)
                return;

            draw();
        }
    }
}
