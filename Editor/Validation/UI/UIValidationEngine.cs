#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Validation.UI
{
    internal enum UIValidationSeverity
    {
        Warning = 0,
        Error = 1
    }

    internal static class UIValidationRuleIds
    {
        public const string ConfigMissingOrInvalid = "UIV000";
        public const string ScriptOutsideMappedFolders = "UIV001";
        public const string PrefabOutsideMappedFolders = "UIV002";
        public const string PanelRootReferencesOrchestration = "UIV003";
        public const string WidgetRootReferencesPanelOrOrchestration = "UIV004";
        public const string ReverseLayerReference = "UIV005";
        public const string InvalidNamingSuffix = "UIV006";
        public const string SerializedDomainDependencyOnRoot = "UIV007";
        public const string MismatchedPrefabFolderLayer = "UIV008";
        public const string ServiceLocatorPattern = "UIV009";
        public const string BroadPublicMutation = "UIV010";
        public const string PrefabCompositionDepthRisk = "UIV011";
        public const string MixedNamingStyle = "UIV012";
    }

    internal sealed class UIValidationIssue
    {
        public string RuleId;
        public UIValidationSeverity Severity;
        public string Path;
        public string Message;
        public string FixHint;
    }

    internal sealed class UIValidationResult
    {
        public readonly List<UIValidationIssue> Issues = new();
        public int ScannedPathCount;
        public double ElapsedMs;
        public string ResolvedConfigPath;
    }

    internal static class UIValidationEngine
    {
        private static readonly string[] ScriptExtensions = { ".cs" };
        private static readonly string[] PrefabExtensions = { ".prefab" };
        private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

        public static UIValidationResult ValidatePaths(IEnumerable<string> paths, bool fullScan)
        {
            DateTime startedAt = DateTime.UtcNow;
            UIValidationResult result = new();
            UIValidationConfigBridgeSnapshot snapshot = UIValidationConfigBridge.BuildSnapshot();
            result.ResolvedConfigPath = snapshot.ResolvedConfigPath;

            for (int i = 0; i < snapshot.MappingErrors.Count; i++)
            {
                AddIssue(result, UIValidationRuleIds.ConfigMissingOrInvalid, UIValidationSeverity.Error,
                    UIValidationConventions.ConfigAssetPath,
                    snapshot.MappingErrors[i],
                    "Repair CentralAuthoringProjectConfig or PackageIntegrationManifest typeMappings.");
            }

            List<string> normalizedPaths = NormalizeCandidatePaths(paths, fullScan);
            result.ScannedPathCount = normalizedPaths.Count;
            for (int i = 0; i < normalizedPaths.Count; i++)
            {
                string path = normalizedPaths[i];
                if (UIValidationConventions.IsThirdPartyPath(path))
                    continue;

                string extension = Path.GetExtension(path);
                if (IsScript(extension))
                {
                    ValidateScriptPath(path, result);
                    ValidateScriptWarnings(path, result);
                    continue;
                }

                if (IsPrefab(extension))
                {
                    ValidatePrefabPath(path, result);
                    ValidatePrefabAsset(path, result);
                }
            }

            SortIssuesDeterministically(result.Issues);
            result.ElapsedMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;
            return result;
        }

        internal static List<string> BuildFullScanPaths()
        {
            HashSet<string> paths = new(PathComparer);
            for (int i = 0; i < UIValidationConventions.UserScriptLayerFolders.Length; i++)
            {
                AddByGlob("t:MonoScript", UIValidationConventions.UserScriptLayerFolders[i], paths);
            }

            for (int i = 0; i < UIValidationConventions.UserPrefabLayerFolders.Length; i++)
            {
                AddByGlob("t:Prefab", UIValidationConventions.UserPrefabLayerFolders[i], paths);
            }

            return paths.OrderBy(p => p, PathComparer).ToList();
        }

        private static void AddByGlob(string filter, string folder, HashSet<string> paths)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                return;

            string[] guids = AssetDatabase.FindAssets(filter, new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrEmpty(path))
                    paths.Add(path);
            }
        }

        private static List<string> NormalizeCandidatePaths(IEnumerable<string> paths, bool fullScan)
        {
            HashSet<string> normalized = new(PathComparer);
            if (fullScan)
            {
                List<string> allPaths = BuildFullScanPaths();
                for (int i = 0; i < allPaths.Count; i++)
                    normalized.Add(allPaths[i]);
            }

            if (paths != null)
            {
                foreach (string path in paths)
                {
                    if (string.IsNullOrEmpty(path))
                        continue;

                    string normalizedPath = path.Replace('\\', '/');
                    if (!normalizedPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!UIValidationConventions.IsCandidateUiPath(normalizedPath))
                        continue;

                    normalized.Add(normalizedPath);
                }
            }

            return normalized.OrderBy(p => p, PathComparer).ToList();
        }

        private static bool IsScript(string extension)
        {
            for (int i = 0; i < ScriptExtensions.Length; i++)
            {
                if (string.Equals(extension, ScriptExtensions[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool IsPrefab(string extension)
        {
            for (int i = 0; i < PrefabExtensions.Length; i++)
            {
                if (string.Equals(extension, PrefabExtensions[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void ValidateScriptPath(string path, UIValidationResult result)
        {
            if (!UIValidationConventions.IsUserUiScriptRoot(path))
                return;

            bool insideAllowedFolder = false;
            for (int i = 0; i < UIValidationConventions.UserScriptLayerFolders.Length; i++)
            {
                if (!UIValidationConventions.IsPathUnder(path, UIValidationConventions.UserScriptLayerFolders[i]))
                    continue;

                insideAllowedFolder = true;
                break;
            }

            if (!insideAllowedFolder)
            {
                AddIssue(result, UIValidationRuleIds.ScriptOutsideMappedFolders, UIValidationSeverity.Error, path,
                    "UI script is outside mapped layer folders.",
                    "Move this script under UIElements, Widgets, Panels, or Orchestration.");
                return;
            }

            UILayer layer = UIValidationConventions.ResolveLayerFromPath(path);
            if (!HasValidSuffix(layer, Path.GetFileNameWithoutExtension(path)))
            {
                AddIssue(result, UIValidationRuleIds.InvalidNamingSuffix, UIValidationSeverity.Error, path,
                    $"Script name does not match required suffix for layer '{layer}'.",
                    "Rename file to match configured suffix conventions.");
            }
        }

        private static void ValidateScriptWarnings(string path, UIValidationResult result)
        {
            string contents;
            try
            {
                contents = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                AddIssue(result, UIValidationRuleIds.ConfigMissingOrInvalid, UIValidationSeverity.Warning, path,
                    $"Unable to inspect script text: {ex.Message}",
                    "Ensure script file is readable.");
                return;
            }

            if (contents.IndexOf("ServiceLocator", StringComparison.OrdinalIgnoreCase) >= 0
                || contents.IndexOf("FindObjectOfType", StringComparison.OrdinalIgnoreCase) >= 0
                || contents.IndexOf("FindAnyObjectByType", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AddIssue(result, UIValidationRuleIds.ServiceLocatorPattern, UIValidationSeverity.Warning, path,
                    "Detected service-locator style access in a UI script.",
                    "Route dependencies through orchestration and explicit bind/render flows.");
            }

            UILayer layer = UIValidationConventions.ResolveLayerFromPath(path);
            if ((layer == UILayer.Widget || layer == UILayer.Panel) && HasBroadMutationPattern(contents))
            {
                AddIssue(result, UIValidationRuleIds.BroadPublicMutation, UIValidationSeverity.Warning, path,
                    "Panel/Widget script appears to expose broad public mutation API.",
                    "Prefer narrow Bind/Render-style entry points.");
            }

            if ((layer == UILayer.Panel || layer == UILayer.Widget) && HasMixedNamingStyle(path))
            {
                AddIssue(result, UIValidationRuleIds.MixedNamingStyle, UIValidationSeverity.Warning, path,
                    "Detected mixed naming style for UI layer type.",
                    "Use canonical suffixes only (Panel, Widget, View, UIManager/Presenter).");
            }
        }

        private static bool HasBroadMutationPattern(string scriptContents)
        {
            int publicVoidCount = 0;
            using StringReader reader = new(scriptContents);
            while (reader.ReadLine() is { } line)
            {
                string trimmed = line.Trim();
                if (!trimmed.StartsWith("public void ", StringComparison.Ordinal))
                    continue;
                if (trimmed.StartsWith("public void Bind", StringComparison.Ordinal)
                    || trimmed.StartsWith("public void Render", StringComparison.Ordinal)
                    || trimmed.StartsWith("public void Show", StringComparison.Ordinal)
                    || trimmed.StartsWith("public void Hide", StringComparison.Ordinal))
                {
                    continue;
                }

                publicVoidCount++;
                if (publicVoidCount >= 3)
                    return true;
            }

            return false;
        }

        private static bool HasMixedNamingStyle(string path)
        {
            UILayer layer = UIValidationConventions.ResolveLayerFromPath(path);
            if (layer == UILayer.Orchestration)
                return false;

            string fileName = Path.GetFileNameWithoutExtension(path);
            return fileName.EndsWith("Screen", StringComparison.Ordinal)
                   || fileName.EndsWith("Window", StringComparison.Ordinal)
                   || fileName.EndsWith("Page", StringComparison.Ordinal);
        }

        private static void ValidatePrefabPath(string path, UIValidationResult result)
        {
            if (!UIValidationConventions.IsPathUnder(path, "Assets/Data/UI/Prefabs"))
                return;

            bool insideAllowedFolder = false;
            for (int i = 0; i < UIValidationConventions.UserPrefabLayerFolders.Length; i++)
            {
                if (!UIValidationConventions.IsPathUnder(path, UIValidationConventions.UserPrefabLayerFolders[i]))
                    continue;
                insideAllowedFolder = true;
                break;
            }

            if (!insideAllowedFolder)
            {
                AddIssue(result, UIValidationRuleIds.PrefabOutsideMappedFolders, UIValidationSeverity.Error, path,
                    "UI prefab is outside mapped prefab layer folders.",
                    "Move this prefab under UIElements, Widgets, or Panels mapped folders.");
            }

            UILayer layer = UIValidationConventions.ResolveLayerFromPath(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (!HasValidSuffix(layer, fileName))
            {
                AddIssue(result, UIValidationRuleIds.InvalidNamingSuffix, UIValidationSeverity.Error, path,
                    $"Prefab name does not match required suffix for layer '{layer}'.",
                    "Rename prefab to match layer naming conventions.");
            }
        }

        private static void ValidatePrefabAsset(string path, UIValidationResult result)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                return;

            UILayer ownerLayer = UIValidationConventions.ResolveLayerFromPath(path);
            Component[] rootComponents = prefab.GetComponents<Component>();
            if (rootComponents == null || rootComponents.Length == 0)
                return;

            ValidateRootDependencies(path, ownerLayer, rootComponents, result);
            ValidateDepth(path, prefab.transform, result);
        }

        private static void ValidateRootDependencies(string path, UILayer ownerLayer, Component[] rootComponents, UIValidationResult result)
        {
            for (int i = 0; i < rootComponents.Length; i++)
            {
                Component component = rootComponents[i];
                if (component == null)
                    continue;

                MonoScript script = MonoScript.FromMonoBehaviour(component as MonoBehaviour);

                Type componentType = component.GetType();
                string scriptPath = script != null ? AssetDatabase.GetAssetPath(script) : string.Empty;
                UILayer componentLayer = UIValidationConventions.ResolveLayerFromPath(scriptPath);

                if (ownerLayer == UILayer.Panel && componentLayer == UILayer.Orchestration)
                {
                    AddError(result, UIValidationRuleIds.PanelRootReferencesOrchestration, path,
                        $"Panel prefab root references orchestration component '{componentType.Name}'.",
                        "Move orchestration ownership to UIManager/Presenter.");
                }

                if (ownerLayer == UILayer.Widget && (componentLayer == UILayer.Panel || componentLayer == UILayer.Orchestration))
                {
                    AddError(result, UIValidationRuleIds.WidgetRootReferencesPanelOrOrchestration, path,
                        $"Widget prefab root references '{componentType.Name}' from higher layer.",
                        "Keep widget roots limited to Widget/UIElement components.");
                }

                if (ownerLayer == UILayer.UIElement && componentLayer != UILayer.Unknown && componentLayer != UILayer.UIElement)
                {
                    AddError(result, UIValidationRuleIds.ReverseLayerReference, path,
                        $"UIElement prefab references higher layer component '{componentType.Name}'.",
                        "UIElement prefabs must only depend on UIElement-level components.");
                }

                if (ownerLayer == UILayer.Widget && componentLayer == UILayer.Panel)
                {
                    AddError(result, UIValidationRuleIds.MismatchedPrefabFolderLayer, path,
                        "Widget prefab root references a Panel component.",
                        "Move panel composition to panel prefabs only.");
                }

                if ((ownerLayer == UILayer.Panel || ownerLayer == UILayer.Widget) && IsDomainServiceComponent(scriptPath, componentType))
                {
                    AddError(result, UIValidationRuleIds.SerializedDomainDependencyOnRoot, path,
                        $"Root component '{componentType.Name}' appears to be a domain/app service dependency.",
                        "Remove domain/service components from panel/widget prefab roots and bind through orchestration.");
                }
            }
        }

        private static bool IsDomainServiceComponent(string scriptPath, Type type)
        {
            if (!string.IsNullOrEmpty(scriptPath) && UIValidationConventions.IsPathUnder(scriptPath, "Assets/Scripts/Domains"))
                return true;

            string fullName = type.FullName ?? string.Empty;
            return fullName.IndexOf(".Domains.", StringComparison.OrdinalIgnoreCase) >= 0
                   || type.Name.EndsWith("Service", StringComparison.Ordinal);
        }

        private static void ValidateDepth(string path, Transform root, UIValidationResult result)
        {
            const int depthWarningThreshold = 9;
            int depth = MeasureDepth(root, 0);
            if (depth < depthWarningThreshold)
                return;

            AddIssue(result, UIValidationRuleIds.PrefabCompositionDepthRisk, UIValidationSeverity.Warning, path,
                $"Prefab hierarchy depth is {depth}, which may indicate coupling risk.",
                "Split deep composition into simpler widgets/panels where possible.");
        }

        private static int MeasureDepth(Transform node, int depth)
        {
            int max = depth;
            for (int i = 0; i < node.childCount; i++)
            {
                int childDepth = MeasureDepth(node.GetChild(i), depth + 1);
                if (childDepth > max)
                    max = childDepth;
            }

            return max;
        }

        private static bool HasValidSuffix(UILayer layer, string nameWithoutExtension)
        {
            if (!UIValidationConventions.SuffixesByLayer.TryGetValue(layer, out string[] suffixes))
                return true;

            for (int i = 0; i < suffixes.Length; i++)
            {
                if (nameWithoutExtension.EndsWith(suffixes[i], StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static void AddError(UIValidationResult result, string ruleId, string path, string message, string fixHint)
        {
            AddIssue(result, ruleId, UIValidationSeverity.Error, path, message, fixHint);
        }

        private static void AddIssue(UIValidationResult result, string ruleId, UIValidationSeverity defaultSeverity, string path, string message, string fixHint)
        {
            UIValidationSeverity resolvedSeverity = UIValidationPolicy.ResolveSeverity(ruleId, defaultSeverity);
            result.Issues.Add(new UIValidationIssue
            {
                RuleId = ruleId,
                Severity = resolvedSeverity,
                Path = path,
                Message = message,
                FixHint = fixHint
            });
        }

        private static void SortIssuesDeterministically(List<UIValidationIssue> issues)
        {
            issues.Sort((a, b) =>
            {
                int severity = b.Severity.CompareTo(a.Severity);
                if (severity != 0)
                    return severity;

                int path = string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
                if (path != 0)
                    return path;

                return string.Compare(a.RuleId, b.RuleId, StringComparison.Ordinal);
            });
        }
    }
}
#endif
