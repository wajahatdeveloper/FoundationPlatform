#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using static AetherNexus.FoundationPlatform.Editor.Utilities.PathComparisonUtility;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Validation.UI
{
    internal enum UILayer
    {
        Unknown = 0,
        UIElement = 1,
        Widget = 2,
        Panel = 3,
        Orchestration = 4
    }

    internal static class UIValidationConventions
    {
        internal const string ConfigAssetPath = ProjectContentConfig.DefaultAssetPath;
        internal const string UserScriptsUiRoot = "Assets/Scripts/UI";
        internal const string UserScriptsDomainsUiRoot = "Assets/Scripts/DomainScripts/UI";
        internal const string UserScriptsDomainsUiOrchestration = "Assets/Scripts/DomainScripts/UI/Orchestration";
        internal const string UserScriptsScenesRoot = "Assets/Scripts/Scenes";
        internal const string UserDataUiRoot = "Assets/Content/UI";

        internal static readonly string[] UserScriptLayerFolders =
        {
            "Assets/Scripts/UI/UIElements",
            "Assets/Scripts/UI/Widgets",
            "Assets/Scripts/UI/Panels",
            "Assets/Scripts/UI/Orchestration",
            UserScriptsDomainsUiOrchestration,
            UserScriptsScenesRoot
        };

        internal static readonly string[] UserPrefabLayerFolders =
        {
            "Assets/Content/UI/Prefabs/UIElements",
            "Assets/Content/UI/Prefabs/Widgets",
            "Assets/Content/UI/Prefabs/Panels"
        };

        internal static readonly string[] UserConfigFolders =
        {
            "Assets/Content/UI/Configs"
        };

        internal static readonly string[] ThirdPartyAllowlistPrefixes =
        {
            "Assets/Plugins/",
            "Assets/AssetPacks/",
            "Assets/Libraries/"
        };

        internal static readonly Dictionary<UILayer, string> ScriptLayerFolderByLayer = new()
        {
            { UILayer.UIElement, "Assets/Scripts/UI/UIElements" },
            { UILayer.Widget, "Assets/Scripts/UI/Widgets" },
            { UILayer.Panel, "Assets/Scripts/UI/Panels" },
            { UILayer.Orchestration, "Assets/Scripts/UI/Orchestration" }
        };

        internal static readonly Dictionary<UILayer, string> PrefabLayerFolderByLayer = new()
        {
            { UILayer.UIElement, "Assets/Content/UI/Prefabs/UIElements" },
            { UILayer.Widget, "Assets/Content/UI/Prefabs/Widgets" },
            { UILayer.Panel, "Assets/Content/UI/Prefabs/Panels" }
        };

        internal static readonly Dictionary<UILayer, string[]> SuffixesByLayer = new()
        {
            { UILayer.UIElement, new[] { "View" } },
            { UILayer.Widget, new[] { "Widget" } },
            { UILayer.Panel, new[] { "Panel" } },
            { UILayer.Orchestration, new[] { "UIManager", "Presenter", "Screen", "Flow", "View" } }
        };

        internal static bool IsUserUiScriptRoot(string path)
        {
            return IsPathUnder(path, UserScriptsUiRoot)
                   || IsPathUnder(path, UserScriptsDomainsUiRoot)
                   || IsPathUnder(path, UserScriptsScenesRoot);
        }

        internal static bool IsCandidateUiPath(string path)
        {
            return IsUserUiScriptRoot(path) || IsPathUnder(path, UserDataUiRoot);
        }

        /// <summary>
        /// Asset changes outside these paths do not run incremental UI convention checks.
        /// </summary>
        internal static bool TriggersIncrementalValidation(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string normalized = path.Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return false;

            if (IsCandidateUiPath(normalized))
                return true;

            return string.Equals(normalized, ConfigAssetPath, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsPathUnder(string path, string root) => PathComparisonUtility.IsPathUnder(path, root);

        internal static bool IsThirdPartyPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string normalizedPath = path.Replace('\\', '/');
            for (int i = 0; i < ThirdPartyAllowlistPrefixes.Length; i++)
            {
                if (normalizedPath.StartsWith(ThirdPartyAllowlistPrefixes[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        internal static UILayer ResolveLayerFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return UILayer.Unknown;

            foreach (KeyValuePair<UILayer, string> kv in ScriptLayerFolderByLayer)
            {
                if (IsPathUnder(path, kv.Value))
                    return kv.Key;
            }

            if (IsPathUnder(path, UserScriptsDomainsUiOrchestration))
                return UILayer.Orchestration;

            if (IsPathUnder(path, UserScriptsScenesRoot))
                return UILayer.Orchestration;

            foreach (KeyValuePair<UILayer, string> kv in PrefabLayerFolderByLayer)
            {
                if (IsPathUnder(path, kv.Value))
                    return kv.Key;
            }

            return UILayer.Unknown;
        }
    }

    internal sealed class UIValidationConfigBridgeSnapshot
    {
        public string ResolvedConfigPath;
        public List<string> MappingErrors { get; } = new();
    }

    internal static class UIValidationConfigBridge
    {
        internal static UIValidationConfigBridgeSnapshot BuildSnapshot()
        {
            UIValidationConfigBridgeSnapshot snapshot = new();
            ProjectContentConfig config = ProjectContentConfig.Load();
            if (config == null)
            {
                snapshot.MappingErrors.Add($"Missing ProjectContentConfig. Expected at '{UIValidationConventions.ConfigAssetPath}'.");
                return snapshot;
            }

            snapshot.ResolvedConfigPath = AssetDatabase.GetAssetPath(config);
            return snapshot;
        }
    }
}
#endif
