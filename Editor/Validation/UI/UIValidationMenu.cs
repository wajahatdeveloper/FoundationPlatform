#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Validation.UI
{
    internal static class UIValidationMenu
    {
        [MenuItem(MenuPaths.Linting.RunFullScan, false, MenuPriorities.Linting + 1)]
        [DesignerFeature(
            "Run Full UI Lint Scan",
            "Checks every UI prefab and scene against the project's UI rules and reports what breaks them.",
            "ui lint scan validate check rules canvas prefab warning error full sweep",
            DesignerFeatureKind.Validation,
            "")]
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
        [DesignerFeature(
            "Print Active Lint Config Path",
            "Logs which lint config file is currently in effect, for when the rules being applied are not the ones you expected.",
            "lint config path which file active rules settings print log locate",
            DesignerFeatureKind.Debug,
            "")]
        private static void LogConfigPath()
        {
            UIValidationConfigBridgeSnapshot snapshot = UIValidationConfigBridge.BuildSnapshot();
            string path = string.IsNullOrEmpty(snapshot.ResolvedConfigPath)
                ? UIValidationConventions.ConfigAssetPath
                : snapshot.ResolvedConfigPath;
            Debug.Log($"[UI conventions] Active folder-mapping config: '{path}'.");
        }
    }
}
#endif
