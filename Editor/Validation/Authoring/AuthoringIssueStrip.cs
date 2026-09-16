#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.AetherInspector;
using AetherNexus.FoundationPlatform.AetherInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Validation
{
    /// <summary>
    /// The one validation block every authoring inspector draws. Button labels are fixed here rather than
    /// per tool, which is what makes the shared action vocabulary a contract instead of a convention each
    /// package re-guesses.
    /// </summary>
    public static class AuthoringIssueStrip
    {
        public const string ValidateLabel = "Validate";
        public const string FixCommonLabel = "Fix common issues";
        public const string ApplyFixLabel = "Apply fix";

        /// <summary>Ellipsis is Unity's own convention for "this opens something", same as its menu entries.</summary>
        public const string ApplyFixChoiceLabel = "Apply fix...";

        private static readonly List<AuthoringIssue> Issues = new();

        /// <summary>Runs the asset-scope validators registered for <paramref name="target"/> and renders them.</summary>
        public static void Draw(UnityEngine.Object target, string readyMessage)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Issues.Clear();
            AuthoringValidatorRegistry.CollectForAsset(target, Issues);
            Draw(target, Issues, readyMessage);
        }

        /// <summary>Renders findings a caller already collected (folder rows, scene checks, window panes).</summary>
        public static void Draw(UnityEngine.Object dirtyTarget, IReadOnlyList<AuthoringIssue> issues, string readyMessage)
        {
            if (issues == null)
                throw new ArgumentNullException(nameof(issues));
            if (string.IsNullOrWhiteSpace(readyMessage))
                throw new ArgumentException("A ready message is required; it states what a clean result means.", nameof(readyMessage));

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(ValidateLabel, GUILayout.Width(80)))
                {
                    AuthoringValidatorRegistry.InvalidateResults();
                    if (dirtyTarget != null)
                        EditorUtility.SetDirty(dirtyTarget);
                }

                using (new EditorGUI.DisabledScope(CountBatchableFixes(issues) < 2))
                {
                    if (GUILayout.Button(FixCommonLabel, GUILayout.Width(120)))
                        ApplyAll(dirtyTarget, issues);
                }
            }

            if (issues.Count == 0)
            {
                GuiKit.InfoBox(readyMessage, InfoMessageType.Info);
                return;
            }

            for (int i = 0; i < issues.Count; i++)
                DrawIssue(dirtyTarget, issues[i]);
        }

        private static void DrawIssue(UnityEngine.Object dirtyTarget, AuthoringIssue issue)
        {
            GuiKit.ValidationBox(issue.GetDisplayMessage(), ToMessageType(issue.Severity));

            if (!issue.HasFix)
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(issue.Fix.Message, EditorStyles.wordWrappedMiniLabel);
                string label = issue.Fix.RequiresChoice ? ApplyFixChoiceLabel : ApplyFixLabel;
                if (GUILayout.Button(label, GUILayout.Width(88)))
                    ApplyOne(dirtyTarget, issue);
            }
        }

        private static void ApplyOne(UnityEngine.Object dirtyTarget, AuthoringIssue issue)
        {
            issue.Fix.Apply();
            MarkDirty(dirtyTarget, issue);
            AuthoringValidatorRegistry.InvalidateResults();
        }

        private static void ApplyAll(UnityEngine.Object dirtyTarget, IReadOnlyList<AuthoringIssue> issues)
        {
            for (int i = 0; i < issues.Count; i++)
            {
                AuthoringIssue issue = issues[i];
                if (!issue.HasFix || issue.Fix.RequiresChoice)
                    continue;

                issue.Fix.Apply();
                MarkDirty(dirtyTarget, issue);
            }

            AuthoringValidatorRegistry.InvalidateResults();
        }

        // Dirtying is enough; Unity writes on the next save. Saving the whole database per fix made a
        // five-issue asset five full AssetDatabase writes.
        private static void MarkDirty(UnityEngine.Object dirtyTarget, AuthoringIssue issue)
        {
            if (issue.RelatedObject != null)
                EditorUtility.SetDirty(issue.RelatedObject);
            else if (dirtyTarget != null)
                EditorUtility.SetDirty(dirtyTarget);
        }

        /// <summary>Batchable fixes only — the ones a single click can apply without asking anything.</summary>
        public static int CountBatchableFixes(IReadOnlyList<AuthoringIssue> issues)
        {
            int count = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                AuthoringIssue issue = issues[i];
                if (issue.HasFix && !issue.Fix.RequiresChoice)
                    count++;
            }

            return count;
        }

        public static InfoMessageType ToMessageType(AuthoringIssueSeverity severity) => severity switch
        {
            AuthoringIssueSeverity.Error => InfoMessageType.Error,
            AuthoringIssueSeverity.Warning => InfoMessageType.Warning,
            AuthoringIssueSeverity.Info => InfoMessageType.Info,
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown authoring issue severity.")
        };

        public static MessageType ToUnityMessageType(AuthoringIssueSeverity severity) => severity switch
        {
            AuthoringIssueSeverity.Error => MessageType.Error,
            AuthoringIssueSeverity.Warning => MessageType.Warning,
            AuthoringIssueSeverity.Info => MessageType.Info,
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown authoring issue severity.")
        };
    }
}
#endif
