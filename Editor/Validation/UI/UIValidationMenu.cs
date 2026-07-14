#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Validation.UI
{
    internal static class UIValidationMenu
    {
        [MenuItem(MenuPaths.Linting.RunFullScan, false, MenuPriorities.Linting + 1)]
        private static void RunFullScan()
        {
            UIValidationResult result = UIValidationEngine.ValidatePaths(paths: null, fullScan: true);
            UIValidationReporter.Report(result, "Full scan");

            int errors = 0;
            int warnings = 0;
            for (int i = 0; i < result.Issues.Count; i++)
            {
                if (result.Issues[i].Severity == UIValidationSeverity.Error)
                    errors++;
                else
                    warnings++;
            }

            EditorUtility.DisplayDialog(
                "UI Validation Full Scan",
                $"Scanned: {result.ScannedPathCount}\nErrors: {errors}\nWarnings: {warnings}\nElapsed: {result.ElapsedMs:F2} ms",
                "OK");
        }

        [MenuItem(MenuPaths.Linting.PrintActiveConfigPath, false, MenuPriorities.Linting + 2)]
        private static void LogConfigPath()
        {
            UIValidationConfigBridgeSnapshot snapshot = UIValidationConfigBridge.BuildSnapshot();
            string path = string.IsNullOrEmpty(snapshot.ResolvedConfigPath)
                ? UIValidationConventions.ConfigAssetPath
                : snapshot.ResolvedConfigPath;
            Debug.Log($"[UI conventions] Active folder-mapping config: '{path}'.");
        }

        [MenuItem(MenuPaths.Linting.RolloutWarningFirst, false, MenuPriorities.Linting + 3)]
        private static void SetWarningFirstMode()
        {
            UIValidationPolicy.SetRolloutMode(UIValidationRolloutMode.WarningFirst);
            Debug.Log("[UI conventions] Rollout mode set to WarningFirst.");
        }

        [MenuItem(MenuPaths.Linting.RolloutWarningFirst, true, MenuPriorities.Linting + 3)]
        private static bool ValidateWarningFirstMode()
        {
            Menu.SetChecked(MenuPaths.Linting.RolloutWarningFirst,
                UIValidationPolicy.GetRolloutMode() == UIValidationRolloutMode.WarningFirst);
            return true;
        }

        [MenuItem(MenuPaths.Linting.RolloutStrict, false, MenuPriorities.Linting + 4)]
        private static void SetStrictMode()
        {
            UIValidationPolicy.SetRolloutMode(UIValidationRolloutMode.Strict);
            Debug.Log("[UI conventions] Rollout mode set to Strict.");
        }

        [MenuItem(MenuPaths.Linting.RolloutStrict, true, MenuPriorities.Linting + 4)]
        private static bool ValidateStrictMode()
        {
            Menu.SetChecked(MenuPaths.Linting.RolloutStrict,
                UIValidationPolicy.GetRolloutMode() == UIValidationRolloutMode.Strict);
            return true;
        }
    }
}
#endif
