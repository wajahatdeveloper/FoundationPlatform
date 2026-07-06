using System;
using UnityEditor;
using UnityEngine;

namespace FoundationPlatform.Editor.Utilities
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

        public static int DrawActionStrip(string[] labels, bool[] enabled)
        {
            if (labels == null || enabled == null)
                throw new ArgumentNullException("[AuthoringUX:ERROR] Action strip labels/enabled arrays cannot be null.");
            if (labels.Length == 0)
                throw new InvalidOperationException("[AuthoringUX:ERROR] Action strip must contain at least one action.");
            if (labels.Length != enabled.Length)
                throw new InvalidOperationException("[AuthoringUX:ERROR] Action strip labels and enabled arrays must have equal length.");

            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(labels[i]))
                        throw new InvalidOperationException($"[AuthoringUX:ERROR] Action label at index {i} is empty.");

                    using (new EditorGUI.DisabledScope(!enabled[i]))
                    {
                        if (GUILayout.Button(labels[i]))
                            return i;
                    }
                }
            }

            return -1;
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

        public static void DrawTooltipIcon(string tooltip, float size = 16f)
        {
            if (string.IsNullOrWhiteSpace(tooltip))
                return;

            GUIContent content = new GUIContent(HelpIcon, tooltip);
            GUILayout.Label(content, GUIStyle.none, GUILayout.Width(size), GUILayout.Height(size));
        }
    }
}
