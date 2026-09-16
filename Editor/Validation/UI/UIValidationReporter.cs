#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Validation.UI
{
using FoundationPlatform.DebugX;

    /// <summary>
    /// Brings UI convention findings into Central Validation. The import postprocessor keeps its own
    /// console report — it is incremental and fires per import, which this project-wide sweep is not.
    /// </summary>
    internal sealed class UIConventionsValidator : IAuthoringValidator
    {
        private const string SourceName = "UI Conventions";

        public ValidationScope Scope => ValidationScope.Project;

        public string Source => SourceName;

        public Type TargetType => null;

        public void Collect(in ValidationRequest request, List<AuthoringIssue> issues)
        {
            UIValidationResult result = UIValidationEngine.ValidatePaths(paths: null, fullScan: true);
            for (int i = 0; i < result.Issues.Count; i++)
            {
                UIValidationIssue issue = result.Issues[i];
                issues.Add(new AuthoringIssue
                {
                    Severity = issue.Severity == UIValidationSeverity.Error
                        ? AuthoringIssueSeverity.Error
                        : AuthoringIssueSeverity.Warning,
                    Source = SourceName,
                    Message = $"{issue.RuleId}: {issue.Message} Fix: {issue.FixHint}",
                    AssetPath = issue.Path,
                    RelatedObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(issue.Path),
                });
            }
        }
    }

    internal static class UIValidationReporter
    {
        private const string LogPrefix = "[UI conventions]";

        internal static void Report(UIValidationResult result, string contextLabel)
        {
            if (result == null)
            {
                Debug.LogError($"{LogPrefix} Result is null.");
                return;
            }

            int errorCount = 0;
            int warningCount = 0;
            for (int i = 0; i < result.Issues.Count; i++)
            {
                if (result.Issues[i].Severity == UIValidationSeverity.Error)
                    errorCount++;
                else
                    warningCount++;
            }

            if (result.Issues.Count == 0)
            {
                DebugX.Debug(
                    $"{LogPrefix} {contextLabel} passed. UI scripts/prefabs checked={result.ScannedPathCount}, elapsedMs={result.ElapsedMs:F2}.");
                return;
            }

            StringBuilder sb = new();
            sb.AppendLine(
                $"{LogPrefix} {contextLabel} completed. UI scripts/prefabs checked={result.ScannedPathCount}, errors={errorCount}, warnings={warningCount}, elapsedMs={result.ElapsedMs:F2}.");
            if (!string.IsNullOrEmpty(result.ResolvedConfigPath))
                sb.AppendLine($"{LogPrefix} Folder mapping config: '{result.ResolvedConfigPath}'.");
            for (int i = 0; i < result.Issues.Count; i++)
            {
                UIValidationIssue issue = result.Issues[i];
                sb.Append('[').Append(issue.Severity).Append("] ");
                sb.Append(issue.RuleId).Append(" | ");
                sb.Append(issue.Path).Append(" | ");
                sb.Append(issue.Message).Append(" | Fix: ").Append(issue.FixHint);
                sb.AppendLine();
            }

            if (errorCount > 0)
                Debug.LogError(sb.ToString());
            else
                Debug.LogWarning(sb.ToString());
        }
    }
}
#endif
