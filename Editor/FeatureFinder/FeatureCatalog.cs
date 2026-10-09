#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.FeatureFinder.Editor
{
    /// <summary>
    /// Editor-time index of project-owned features. Menu entries come from <see cref="TypeCache"/>
    /// and dispatch through <c>EditorApplication.ExecuteMenuItem</c>, so the Finder never becomes a
    /// second execution path; everything else arrives through <see cref="IFeatureCatalogSource"/>.
    /// </summary>
    internal static class FeatureCatalog
    {
        // Unity ships several hundred [MenuItem]s of its own and asset packs add more, so ownership is
        // decided by where the declaring assembly lives rather than by menu path: a new menu root needs
        // no registration here.
        private static readonly string[] VendoredAssetRoots =
        {
            "Assets/AssetPacks/",
            "Assets/Plugins/",
            "Assets/Libraries/"
        };

        private static readonly Dictionary<Assembly, bool> s_ownedAssemblies = new Dictionary<Assembly, bool>();

        private static List<FeatureEntry> _entries;
        private static int _version;

        internal static IReadOnlyList<FeatureEntry> Entries
        {
            get
            {
                if (_entries == null) Rebuild();
                return _entries;
            }
        }

        /// <summary>Bumped on every rebuild so a consumer can cache derived state against it.</summary>
        internal static int Version => _version;

        /// <summary>
        /// Drops the index so the next read rebuilds it. Reopening the Finder does not need this -
        /// a domain reload already clears the cache, and scanning TypeCache plus every contributed
        /// source on each open is what made the window slow to appear.
        /// </summary>
        internal static void Invalidate() => _entries = null;

        internal static void Rebuild()
        {
            var entries = new List<FeatureEntry>(256);
            CollectMenuItems(entries);
            CollectSources(entries);

            entries.Sort(CompareForBrowse);
            _entries = entries;
            _version++;
        }

        private static void CollectMenuItems(List<FeatureEntry> entries)
        {
            var seenPaths = new HashSet<string>(StringComparer.Ordinal);

            foreach (var method in TypeCache.GetMethodsWithAttribute<MenuItem>())
            {
                var menuItems = method.GetCustomAttributes<MenuItem>(false);
                var feature = method.GetCustomAttribute<DesignerFeatureAttribute>(false);

                foreach (var menuItem in menuItems)
                {
                    if (menuItem.validate) continue;

                    string path = StripShortcut(menuItem.menuItem);
                    if (!IsOwned(method, path)) continue;
                    if (!seenPaths.Add(path)) continue;

                    entries.Add(feature == null
                        ? new FeatureEntry(path, LeafOf(path), "", "", InferKind(path), "", false, "", 0,
                            BuildMenuInvoke(path))
                        : new FeatureEntry(path, feature.Title, feature.Blurb, feature.Keywords,
                            feature.Kind, feature.Doc, true, "", 0, BuildMenuInvoke(path)));

                    // A tagged method registered under two menu paths (a Tools create that is also a
                    // Window entry, say) would otherwise produce two rows with identical text.
                    if (feature != null) break;
                }
            }
        }

        private static void CollectSources(List<FeatureEntry> entries)
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<IFeatureCatalogSource>())
            {
                if (type.IsAbstract || type.IsInterface) continue;

                var source = (IFeatureCatalogSource)Activator.CreateInstance(type);
                source.Contribute(entries);
            }
        }

        private static Action BuildMenuInvoke(string path)
        {
            return () =>
            {
                if (!EditorApplication.ExecuteMenuItem(path))
                    throw new InvalidOperationException(
                        $"Feature Finder could not execute menu item '{path}'. The catalog is stale or the menu is disabled by a validate function.");
            };
        }

        private static int CompareForBrowse(FeatureEntry a, FeatureEntry b)
        {
            if (a.IsTagged != b.IsTagged) return a.IsTagged ? -1 : 1;
            if (a.SortBias != b.SortBias) return b.SortBias.CompareTo(a.SortBias);
            if (a.MenuPath.Length > 0 || b.MenuPath.Length > 0)
                return string.Compare(a.MenuPath, b.MenuPath, StringComparison.Ordinal);
            return string.Compare(a.Title, b.Title, StringComparison.Ordinal);
        }

        // CONTEXT/* entries need a target object, so they cannot be dispatched from the catalog.
        private static bool IsOwned(MethodInfo method, string path)
        {
            if (path.StartsWith("CONTEXT/", StringComparison.Ordinal)) return false;

            Assembly assembly = method.DeclaringType.Assembly;
            if (s_ownedAssemblies.TryGetValue(assembly, out bool owned)) return owned;

            // Asmdef-less scripts compile into the predefined Assembly-CSharp* assemblies and belong to the project.
            string assemblyName = assembly.GetName().Name;
            string asmdefPath = UnityEditor.Compilation.CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assemblyName);
            if (string.IsNullOrEmpty(asmdefPath))
            {
                owned = assemblyName.StartsWith("Assembly-CSharp", StringComparison.Ordinal);
            }
            else if (asmdefPath.StartsWith("Packages/com.aethernexus.", StringComparison.Ordinal))
            {
                owned = true;
            }
            else
            {
                owned = asmdefPath.StartsWith("Assets/", StringComparison.Ordinal);
                foreach (string root in VendoredAssetRoots)
                {
                    if (asmdefPath.StartsWith(root, StringComparison.Ordinal)) owned = false;
                }
            }

            s_ownedAssemblies[assembly] = owned;
            return owned;
        }

        // "Tools/Domain/Item/Create/Equippable Item %#e" -> drops the trailing shortcut token.
        private static string StripShortcut(string menuItem)
        {
            int space = menuItem.LastIndexOf(' ');
            if (space <= 0 || space == menuItem.Length - 1) return menuItem;

            string tail = menuItem.Substring(space + 1);
            foreach (char c in tail)
            {
                if (c == '%' || c == '#' || c == '&' || c == '_') return menuItem.Substring(0, space);
            }
            return menuItem;
        }

        private static string LeafOf(string path)
        {
            int slash = path.LastIndexOf('/');
            string leaf = slash < 0 ? path : path.Substring(slash + 1);
            return leaf.EndsWith("...", StringComparison.Ordinal)
                ? leaf.Substring(0, leaf.Length - 3)
                : leaf;
        }

        private static DesignerFeatureKind InferKind(string path)
        {
            if (path.StartsWith("Window/", StringComparison.Ordinal)) return DesignerFeatureKind.Window;
            if (path.Contains("/Validation/") || path.Contains("Validate")) return DesignerFeatureKind.Validation;
            if (path.Contains("/Create") || path.Contains("Generate") || path.Contains("Rebuild"))
                return DesignerFeatureKind.Generator;
            return DesignerFeatureKind.Action;
        }
    }
}
#endif
