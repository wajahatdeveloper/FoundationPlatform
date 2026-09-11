#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using AetherNexus.FoundationPlatform.DesignerSurfaces.Editor;
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
        /// <summary>Type-name endings that carry no meaning in the fallback initial.</summary>
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
                    $"No designer-facing types found in package '{packageId}'. Expected one of the first-party packages under {FirstPartyScriptPaths.FirstPartyPrefix}*.");

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

            string scriptPath = FirstPartyScriptPaths.Resolve(type, scriptPaths);
            if (!FirstPartyScriptPaths.IsFirstParty(scriptPath))
                return;

            string packageRoot = FirstPartyScriptPaths.PackageRootOf(scriptPath);
            string domain = ResolveDomain(menuPath, packageRoot);
            bool hasIcon = type.GetCustomAttribute<IconAttribute>(false) != null;
            var declared = type.GetCustomAttribute<DesignerIconAttribute>(false);
            DesignerSymbol? symbol = declared == null ? (DesignerSymbol?)null : declared.Symbol;

            entries.Add(new DesignerIconEntry(
                type, scriptPath, packageRoot, menuPath, domain, Letter(type.Name), symbol, isAsset, hasIcon));
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

        /// <summary>
        /// Fallback mark: the initial of the first meaningful PascalCase word. One bold letter
        /// survives the 16px draw; the two-letter monogram it replaces did not.
        /// </summary>
        internal static char Letter(string typeName)
        {
            string trimmed = StripNoiseSuffix(typeName);
            var words = SplitWords(trimmed);
            string first = words.Count > 0 ? words[0] : trimmed;

            if (first.Length == 0)
                throw new InvalidOperationException(
                    $"Type name '{typeName}' yields no letter for its icon. Give the type a [DesignerIcon] symbol.");

            return char.ToUpperInvariant(first[0]);
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

        /// <summary>
        /// PascalCase split, shared with the symbol audit so both read a type name the same way.
        /// Runs of capitals stay together, so <c>UIGridRenderer</c> reads as UI / Grid / Renderer
        /// rather than U / I / Grid / Renderer — the audit matches on words like "ui" and "ik".
        /// </summary>
        internal static List<string> SplitWords(string name)
        {
            var words = new List<string>(4);
            var current = new StringBuilder();

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (!char.IsLetterOrDigit(c))
                    continue;

                if (current.Length > 0 && IsBoundary(name, i))
                {
                    words.Add(current.ToString());
                    current.Clear();
                }

                current.Append(c);
            }

            if (current.Length > 0)
                words.Add(current.ToString());

            return words;
        }

        private static bool IsBoundary(string name, int index)
        {
            char c = name[index];
            char previous = name[index - 1];

            if (char.IsDigit(c) != char.IsDigit(previous))
                return true;

            if (!char.IsUpper(c))
                return false;

            // A capital opens a word unless it continues a run of capitals — and the last capital
            // of a run belongs to the word that follows it (the "R" in "UIRenderer").
            bool continuesRun = char.IsUpper(previous);
            bool startsNextWord = index + 1 < name.Length && char.IsLower(name[index + 1]);
            return !continuesRun || startsNextWord;
        }
    }
}
#endif
