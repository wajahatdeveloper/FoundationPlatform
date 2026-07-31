#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

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
        internal const string ConfigAssetPath = DataFolderMappingConfig.CentralAuthoringProjectConfigAssetPath;
        internal const string UserScriptsUiRoot = "Assets/Scripts/UI";
        internal const string UserScriptsDomainsUiRoot = "Assets/Scripts/DomainScripts/UI";
        internal const string UserScriptsDomainsUiOrchestration = "Assets/Scripts/DomainScripts/UI/Orchestration";
        internal const string UserScriptsScenesRoot = "Assets/Scripts/Scenes";
        internal const string UserDataUiRoot = "Assets/Data/UI";

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
            "Assets/Data/UI/Prefabs/UIElements",
            "Assets/Data/UI/Prefabs/Widgets",
            "Assets/Data/UI/Prefabs/Panels"
        };

        internal static readonly string[] UserConfigFolders =
        {
            "Assets/Data/UI/Configs"
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
            { UILayer.UIElement, "Assets/Data/UI/Prefabs/UIElements" },
            { UILayer.Widget, "Assets/Data/UI/Prefabs/Widgets" },
            { UILayer.Panel, "Assets/Data/UI/Prefabs/Panels" }
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

        internal static bool IsPathUnder(string path, string folder)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(folder))
                return false;

            string normalizedPath = path.Replace('\\', '/');
            string normalizedFolder = folder.Replace('\\', '/').TrimEnd('/');
            return normalizedPath.StartsWith(normalizedFolder + "/", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(normalizedPath, normalizedFolder, StringComparison.OrdinalIgnoreCase);
        }

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
        public Dictionary<Type, string> TypeToFolder { get; } = new();
        public Dictionary<UILayer, HashSet<string>> LayerFolders { get; } = new()
        {
            { UILayer.UIElement, new HashSet<string>(StringComparer.OrdinalIgnoreCase) },
            { UILayer.Widget, new HashSet<string>(StringComparer.OrdinalIgnoreCase) },
            { UILayer.Panel, new HashSet<string>(StringComparer.OrdinalIgnoreCase) },
            { UILayer.Orchestration, new HashSet<string>(StringComparer.OrdinalIgnoreCase) }
        };
    }

    internal static class UIValidationConfigBridge
    {
        internal static UIValidationConfigBridgeSnapshot BuildSnapshot()
        {
            UIValidationConfigBridgeSnapshot snapshot = new();
            DataFolderMappingConfig config = DataFolderMappingConfig.Load();
            if (config == null)
            {
                snapshot.MappingErrors.Add($"Missing mapping config. Expected at '{UIValidationConventions.ConfigAssetPath}'.");
                return snapshot;
            }

            string configPath = AssetDatabase.GetAssetPath(config);
            snapshot.ResolvedConfigPath = string.IsNullOrEmpty(configPath)
                ? DataFolderMappingConfig.CentralAuthoringProjectConfigAssetPath
                : configPath;

            IReadOnlyList<Type> mappedTypes = config.GetAllMappedTypes();
            for (int i = 0; i < mappedTypes.Count; i++)
            {
                Type mappedType = mappedTypes[i];
                IReadOnlyList<string> concrete = config.ResolveConcreteFoldersForType(mappedType);
                if (concrete == null || concrete.Count == 0)
                    continue;

                string folderPath = concrete[0];
                if (string.IsNullOrEmpty(folderPath))
                    continue;

                snapshot.TypeToFolder[mappedType] = folderPath;
                UILayer layer = UIValidationConventions.ResolveLayerFromPath(folderPath);
                if (layer == UILayer.Unknown)
                    continue;

                snapshot.LayerFolders[layer].Add(folderPath);
            }

            return snapshot;
        }
    }
}
#endif
