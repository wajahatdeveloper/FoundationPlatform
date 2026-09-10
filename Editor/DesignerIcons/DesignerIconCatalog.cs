#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using AetherNexus.FoundationPlatform.Utilities.Menus;

namespace AetherNexus.FoundationPlatform.DesignerIcons.Editor
{
    /// <summary>
    /// The set of first-party types a designer actually meets: anything reachable through
    /// <c>Assets ▸ Create</c>, <c>Add Component</c>, or the Feature Finder. This is the single
    /// index behind icon generation, attribute stamping, and the coverage lint, so all three agree
    /// on what "designer-facing" means.
    /// </summary>
    internal static class DesignerIconCatalog
    {
        internal const string FirstPartyPrefix = "Packages/com.aethernexus.";

        /// <summary>Type-name endings that carry no meaning in a two-letter monogram.</summary>
        private static readonly string[] NoiseSuffixes =
        {
            "ScriptableObject", "Definition", "Controller", "Component", "Behaviour", "Settings",
            "Registry", "Manager", "Profile", "Config", "Asset", "Data", "SO",
        };

        internal static List<DesignerIconEntry> Build()
        {
            var scriptPaths = new Dictionary<Type, string>();
            var entries = new List<DesignerIconEntry>(256);
            var seen = new HashSet<Type>();

            foreach (Type type in TypeCache.GetTypesWithAttribute<CreateAssetMenuAttribute>())
                TryAdd(type, entries, seen, scriptPaths);

            foreach (Type type in TypeCache.GetTypesWithAttribute<AddComponentMenu>())
                TryAdd(type, entries, seen, scriptPaths);

            foreach (Type type in TypeCache.GetTypesWithAttribute<DesignerFeatureAttribute>())
                TryAdd(type, entries, seen, scriptPaths);

            entries.Sort((a, b) =>
            {
                int byPackage = string.CompareOrdinal(a.PackageId, b.PackageId);
                if (byPackage != 0) return byPackage;
                int byDomain = string.CompareOrdinal(a.Domain, b.Domain);
                return byDomain != 0 ? byDomain : string.CompareOrdinal(a.Type.Name, b.Type.Name);
            });

            return entries;
        }

        internal static List<DesignerIconEntry> BuildForPackage(string packageId)
        {
            var all = Build();
            var filtered = new List<DesignerIconEntry>(all.Count);
            foreach (var entry in all)
            {
                if (string.Equals(entry.PackageId, packageId, StringComparison.OrdinalIgnoreCase))
                    filtered.Add(entry);
            }

            if (filtered.Count == 0)
                throw new InvalidOperationException(
                    $"No designer-facing types found in package '{packageId}'. Expected one of the first-party packages under {FirstPartyPrefix}*.");

            return filtered;
        }

        private static void TryAdd(Type type, List<DesignerIconEntry> entries, HashSet<Type> seen, Dictionary<Type, string> scriptPaths)
        {
            if (!seen.Add(type))
                return;

            if (type.IsAbstract || type.IsGenericTypeDefinition)
                return;

            bool isAsset = typeof(ScriptableObject).IsAssignableFrom(type);
            bool isComponent = typeof(MonoBehaviour).IsAssignableFrom(type);
            if (!isAsset && !isComponent)
                return;

            string menuPath = ResolveMenuPath(type, out bool hiddenFromMenu);
            if (hiddenFromMenu)
                return;

            string scriptPath = ResolveScriptPath(type, scriptPaths);
            if (scriptPath == null || !scriptPath.StartsWith(FirstPartyPrefix, StringComparison.Ordinal))
                return;

            string packageRoot = PackageRootOf(scriptPath);
            string domain = ResolveDomain(menuPath, packageRoot);
            bool hasIcon = type.GetCustomAttribute<IconAttribute>(false) != null;

            entries.Add(new DesignerIconEntry(
                type, scriptPath, packageRoot, menuPath, domain, Monogram(type.Name), isAsset, hasIcon));
        }

        /// <summary>Menu path as the designer sees it. <paramref name="hiddenFromMenu"/> marks types deliberately kept out of Add Component.</summary>
        private static string ResolveMenuPath(Type type, out bool hiddenFromMenu)
        {
            hiddenFromMenu = false;

            var createAsset = type.GetCustomAttribute<CreateAssetMenuAttribute>(false);
            if (createAsset != null && !string.IsNullOrWhiteSpace(createAsset.menuName))
                return createAsset.menuName;

            var addComponent = type.GetCustomAttribute<AddComponentMenu>(false);
            if (addComponent != null)
            {
                if (string.IsNullOrEmpty(addComponent.componentMenu))
                {
                    hiddenFromMenu = true;
                    return string.Empty;
                }

                return addComponent.componentMenu;
            }

            return string.Empty;
        }

        private static string ResolveDomain(string menuPath, string packageRoot)
        {
            if (!string.IsNullOrEmpty(menuPath))
            {
                int slash = menuPath.IndexOf('/');
                string root = slash < 0 ? menuPath : menuPath.Substring(0, slash);
                string canonical = DesignerIconPalette.Canonicalize(root.Trim());
                if (DesignerIconPalette.HasColor(canonical))
                    return canonical;
            }

            // Menu-less ([DesignerFeature]-only) types, and menu roots with no palette entry, fall
            // back to the owning package's bucket rather than inventing an unclassified colour.
            return DesignerIconPalette.Canonicalize(packageRoot.Substring("Packages/".Length));
        }

        private static string ResolveScriptPath(Type type, Dictionary<Type, string> cache)
        {
            if (cache.TryGetValue(type, out string cached))
                return cached;

            string resolved = null;
            foreach (string guid in AssetDatabase.FindAssets($"{type.Name} t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.Equals(Path.GetFileNameWithoutExtension(path), type.Name, StringComparison.Ordinal))
                    continue;

                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type)
                {
                    resolved = path;
                    break;
                }
            }

            cache[type] = resolved;
            return resolved;
        }

        private static string PackageRootOf(string scriptPath)
        {
            // "Packages/com.aethernexus.x/..." -> "Packages/com.aethernexus.x"
            int second = scriptPath.IndexOf('/', "Packages/".Length);
            return second < 0 ? scriptPath : scriptPath.Substring(0, second);
        }

        /// <summary>One or two uppercase letters: initials of the first two meaningful PascalCase words.</summary>
        internal static string Monogram(string typeName)
        {
            string trimmed = StripNoiseSuffix(typeName);
            var words = SplitWords(trimmed);

            if (words.Count >= 2)
                return $"{char.ToUpperInvariant(words[0][0])}{char.ToUpperInvariant(words[1][0])}";

            string single = words.Count == 1 ? words[0] : trimmed;
            return single.Length >= 2
                ? $"{char.ToUpperInvariant(single[0])}{char.ToUpperInvariant(single[1])}"
                : single.ToUpperInvariant();
        }

        private static string StripNoiseSuffix(string typeName)
        {
            foreach (string suffix in NoiseSuffixes)
            {
                if (typeName.Length > suffix.Length && typeName.EndsWith(suffix, StringComparison.Ordinal))
                    return typeName.Substring(0, typeName.Length - suffix.Length);
            }

            return typeName;
        }

        private static List<string> SplitWords(string name)
        {
            var words = new List<string>(4);
            var current = new StringBuilder();

            foreach (char c in name)
            {
                if (char.IsUpper(c) && current.Length > 0)
                {
                    words.Add(current.ToString());
                    current.Clear();
                }

                if (char.IsLetterOrDigit(c))
                    current.Append(c);
            }

            if (current.Length > 0)
                words.Add(current.ToString());

            return words;
        }
    }
}
#endif
