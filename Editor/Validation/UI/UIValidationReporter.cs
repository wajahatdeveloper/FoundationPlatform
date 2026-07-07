#if UNITY_EDITOR
using System.Text;
using UnityEngine;

namespace FoundationPlatform.Editor.Utilities.Validation.UI
{
using FoundationPlatform.DebugX;
    
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
