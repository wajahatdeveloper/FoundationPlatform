#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using AetherNexus.FoundationPlatform.AetherInspector;
using AetherNexus.FoundationPlatform.Attributes;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities
{
    [CreateAssetMenu(
        fileName = "ProjectContentConfig",
        menuName = "Project/Project Content Config",
        order = 130)]
    [DesignerFeature(
        "Project Content Config",
        "The project's one content layout config: which content areas exist, which read as combined, allowed Shared/Global subfolders, required roots and exempt folders.",
        "project content config content area combined shared global subfolder required root exempt exemption folder layout out of sync hub",
        DesignerFeatureKind.Asset,
        "docs/09-EditorHub.md")]
    [ContentHome("Global/Authoring/**")]
    [Icon("Packages/com.aethernexus.foundationplatform/Editor/Icons/ProjectContentConfig.png")]
    [DesignerIcon(DesignerSymbol.Document)]
    public sealed class ProjectContentConfig : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/Content/Global/Authoring/ProjectContentConfig.asset";
        private const string ContentDomainsRoot = "Assets/Content/Domains";
        private const string ContentSharedRoot = "Assets/Content/Shared";
        private const string ContentGlobalRoot = "Assets/Content/Global";

        [SerializeField]
        [LabelText("Content Areas")]
        [Tooltip("Top-level folders under Assets/Content/Domains/ that exist in this project (PascalCase, single segment). " +
                 "A '*' directly after 'Domains' in a [ContentHome] pattern only expands to areas listed here.")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        private List<string> domains = new();

        [SerializeField]
        [LabelText("Combined Content Areas")]
        [Tooltip("Content areas that read as combined/shared (purple folder) even when only one package maps into them. " +
                 "Package-declared domains carry their own kind; this is the product-side override.")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        private List<string> compositeDomains = new();

        [SerializeField]
        [LabelText("Data Shared Subfolders")]
        [Tooltip("Allowed subfolders under Assets/Content/Shared/ (e.g. Materials, Sprites, Audio).")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        private List<string> dataSharedSubfolders = new();

        [SerializeField]
        [LabelText("Data Global Subfolders")]
        [Tooltip("Allowed subfolders under Assets/Content/Global/ (e.g. Authoring, GameProperties, Resources).")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        private List<string> dataGlobalSubfolders = new();

        [SerializeField]
        [LabelText("Required Asset Folder Roots")]
        [Tooltip("Authoring-only. Each path must be a folder under Assets/. Empty list disables this check.")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        private List<string> requiredAssetFolderRoots = new();

        [SerializeField]
        [LabelText("Auto-Move Out-of-Sync Assets On Import")]
        [Tooltip("When enabled, importing an asset whose type declares a [ContentHome] into the wrong folder silently " +
                 "moves it to its home folder on every import batch. Off by default: prefer the designer-initiated " +
                 "'Fix Out of Sync' action in the Project window / Central Validation instead of an unprompted move.")]
        private bool autoMoveOutOfSyncOnImport;

        [SerializeField]
        [LabelText("Exempt Folders")]
        [Tooltip("Folders under Assets/ excluded, with everything beneath them, from content-home ownership, out-of-sync " +
                 "and unclaimed-type validation. Types whose script lives inside one are not reported as unclaimed.")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        private List<string> exemptFolders = new();

        public IReadOnlyList<string> Domains => domains;
        public IReadOnlyList<string> CompositeDomains => compositeDomains;
        public IReadOnlyList<string> DataSharedSubfolders => dataSharedSubfolders;
        public IReadOnlyList<string> DataGlobalSubfolders => dataGlobalSubfolders;
        public IReadOnlyList<string> RequiredAssetFolderRoots => requiredAssetFolderRoots;
        public bool AutoMoveOutOfSyncOnImport => autoMoveOutOfSyncOnImport;
        public IReadOnlyList<string> ExemptFolders => exemptFolders;

        [Button(ButtonSizes.Medium)]
        private void PopulateFromFileSystemState()
        {
            domains = GetTopLevelFolderNames(ContentDomainsRoot);
            dataSharedSubfolders = GetTopLevelFolderNames(ContentSharedRoot);
            dataGlobalSubfolders = GetTopLevelFolderNames(ContentGlobalRoot);

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public bool IsKnownDomain(string name) => ContainsTrimmed(domains, name);

        public bool IsCompositeDomain(string name) => ContainsTrimmed(compositeDomains, name);

        public IReadOnlyList<string> GetConfiguredDomains()
        {
            var result = new List<string>();
            for (int i = 0; i < domains.Count; i++)
            {
                string domain = domains[i];
                if (string.IsNullOrWhiteSpace(domain))
                    continue;
                string normalized = domain.Trim();
                if (!ContainsTrimmed(result, normalized))
                    result.Add(normalized);
            }

            return result;
        }

        public bool IsPathExempt(string assetPath)
        {
            string normalizedAsset = assetPath.Replace('\\', '/');
            for (int i = 0; i < exemptFolders.Count; i++)
            {
                string raw = exemptFolders[i];
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                string folder = raw.Trim().Replace('\\', '/').TrimEnd('/');
                if (string.Equals(normalizedAsset, folder, StringComparison.OrdinalIgnoreCase) ||
                    normalizedAsset.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsTypeScriptExempt(Type type)
        {
            return ContentHomes.TryGetTypeScriptPath(type, out string scriptPath) && IsPathExempt(scriptPath);
        }

        /// <summary>Callers pass a name already normalized to a single PascalCase segment.</summary>
        public bool TryRegisterDomain(string name, out string error)
        {
            if (IsKnownDomain(name))
            {
                error = "Content area '" + name + "' is already registered in ProjectContentConfig.";
                return false;
            }

            error = string.Empty;
            domains.Add(name);
            domains.Sort(StringComparer.OrdinalIgnoreCase);
            Save();
            return true;
        }

        public bool TryUnregisterDomain(string name, out string error)
        {
            int removed = RemoveTrimmed(domains, name);
            if (removed == 0)
            {
                error = "Content area '" + name + "' is not registered in ProjectContentConfig.";
                return false;
            }

            error = string.Empty;
            Save();
            return true;
        }

        public bool TryRenameDomainInList(string oldName, string newName, out string error)
        {
            int index = IndexOfTrimmed(domains, oldName);
            if (index < 0)
            {
                error = "Content area '" + oldName + "' is not registered in ProjectContentConfig.";
                return false;
            }

            if (IsKnownDomain(newName) && !string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
            {
                error = "Content area '" + newName + "' is already registered in ProjectContentConfig.";
                return false;
            }

            error = string.Empty;
            domains[index] = newName;
            domains.Sort(StringComparer.OrdinalIgnoreCase);
            Save();
            return true;
        }

        /// <summary>
        /// Add or remove a product-side combined override. Package-declared domains do not need this — their
        /// manifest declaration already carries the kind.
        /// </summary>
        public void SetCompositeDomain(string name, bool composite)
        {
            bool changed;
            if (composite)
            {
                changed = !IsCompositeDomain(name);
                if (changed)
                {
                    compositeDomains.Add(name);
                    compositeDomains.Sort(StringComparer.OrdinalIgnoreCase);
                }
            }
            else
            {
                changed = RemoveTrimmed(compositeDomains, name) > 0;
            }

            if (changed)
                Save();
        }

        public void AppendRequiredFolderRootIssues(IList<string> messages)
        {
            const string assetsPrefix = "Assets/";
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < requiredAssetFolderRoots.Count; i++)
            {
                string raw = requiredAssetFolderRoots[i];
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                string path = raw.Trim().Replace('\\', '/');
                if (!seen.Add(path))
                    continue;

                if (!path.StartsWith(assetsPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    messages.Add("[ProjectContentConfig] Required folder root must start with '" + assetsPrefix + "'. Got: '" + path + "'.");
                    continue;
                }

                if (!AssetDatabase.IsValidFolder(path))
                    messages.Add("[ProjectContentConfig] Required folder root does not exist or is not a folder: '" + path + "'.");
            }
        }

        public void AppendExemptFolderIssues(IList<string> messages)
        {
            for (int i = 0; i < exemptFolders.Count; i++)
            {
                string raw = exemptFolders[i];
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                string path = raw.Trim().Replace('\\', '/').TrimEnd('/');
                if (!AssetDatabase.IsValidFolder(path))
                    messages.Add("[ProjectContentConfig] Exempt folder does not exist or is not a folder: '" + path + "'.");
            }
        }

        /// <summary>The project's config, or null when none exists. Throws when more than one exists.</summary>
        public static ProjectContentConfig Load()
        {
            ProjectContentConfig atDefault = AssetDatabase.LoadAssetAtPath<ProjectContentConfig>(DefaultAssetPath);
            if (atDefault != null)
                return atDefault;

            string[] guids = AssetDatabase.FindAssets("t:" + nameof(ProjectContentConfig));
            if (guids.Length == 0)
                return null;
            if (guids.Length > 1)
            {
                var paths = new List<string>();
                for (int i = 0; i < guids.Length; i++)
                    paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
                throw new InvalidOperationException(
                    "[ProjectContentConfig] Exactly one ProjectContentConfig is allowed per project; found " + guids.Length +
                    ": " + string.Join(", ", paths) + ". Keep the one at '" + DefaultAssetPath + "' and delete the rest.");
            }

            return AssetDatabase.LoadAssetAtPath<ProjectContentConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        public static ProjectContentConfig EnsureAsset()
        {
            ProjectContentConfig existing = Load();
            if (existing != null)
                return existing;

            EditorAssetFolders.EnsureFolder(Path.GetDirectoryName(DefaultAssetPath).Replace('\\', '/'));
            ProjectContentConfig created = CreateInstance<ProjectContentConfig>();
            AssetDatabase.CreateAsset(created, DefaultAssetPath);
            AssetDatabase.SaveAssets();
            return created;
        }

        private void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        private static bool ContainsTrimmed(List<string> list, string name) => IndexOfTrimmed(list, name) >= 0;

        private static int IndexOfTrimmed(List<string> list, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return -1;

            string trimmed = name.Trim();
            for (int i = 0; i < list.Count; i++)
            {
                string entry = list[i];
                if (string.IsNullOrWhiteSpace(entry))
                    continue;
                if (string.Equals(entry.Trim(), trimmed, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static int RemoveTrimmed(List<string> list, string name)
        {
            int removed = 0;
            for (int index = IndexOfTrimmed(list, name); index >= 0; index = IndexOfTrimmed(list, name))
            {
                list.RemoveAt(index);
                removed++;
            }

            return removed;
        }

        private static List<string> GetTopLevelFolderNames(string rootPath)
        {
            var result = new List<string>();
            if (!AssetDatabase.IsValidFolder(rootPath))
                return result;

            string[] subfolders = AssetDatabase.GetSubFolders(rootPath);
            for (int i = 0; i < subfolders.Length; i++)
            {
                string folderName = Path.GetFileName(subfolders[i].Replace('\\', '/').TrimEnd('/'));
                if (!string.IsNullOrWhiteSpace(folderName) && !ContainsTrimmed(result, folderName))
                    result.Add(folderName);
            }

            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }
    }
}
#endif
