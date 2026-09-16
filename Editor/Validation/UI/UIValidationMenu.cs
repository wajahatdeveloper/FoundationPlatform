#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Validation.UI
{
    internal static class UIValidationMenu
    {
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
