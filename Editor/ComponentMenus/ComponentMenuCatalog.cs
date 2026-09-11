#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using AetherNexus.FoundationPlatform.DesignerSurfaces.Editor;

namespace AetherNexus.FoundationPlatform.ComponentMenus.Editor
{
    /// <summary>
    /// Every concrete first-party <see cref="MonoBehaviour"/>, with the menu path it declares today
    /// and the one the pass proposes. This is the index behind the coverage report, the stamper and
    /// the lint, so all three agree on what "unclassified component" means.
    /// </summary>
    internal static class ComponentMenuCatalog
    {
        /// <summary>Vendored source we do not author, so stamping it would only create upstream merge noise.</summary>
        private const string VendoredFolder = "/ThirdParty/";

        internal static List<ComponentMenuEntry> Build()
        {
            var classifier = new ComponentMenuClassifier();
            var scriptPaths = new Dictionary<Type, string>();
            var entries = new List<ComponentMenuEntry>(256);

            foreach (Type type in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
            {
                if (type.IsAbstract || type.IsGenericTypeDefinition)
                    continue;

                string scriptPath = FirstPartyScriptPaths.Resolve(type, scriptPaths);
                if (!FirstPartyScriptPaths.IsFirstParty(scriptPath))
                    continue;

                if (scriptPath.Contains(VendoredFolder))
                    continue;

                entries.Add(BuildEntry(type, scriptPath, classifier));
            }

            entries.Sort(CompareEntries);
            return entries;
        }

        private static ComponentMenuEntry BuildEntry(Type type, string scriptPath, ComponentMenuClassifier classifier)
        {
            string packageRoot = FirstPartyScriptPaths.PackageRootOf(scriptPath);
            var authored = type.GetCustomAttribute<AddComponentMenu>(false);

            if (authored != null)
            {
                return new ComponentMenuEntry(
                    type, scriptPath, packageRoot, authored.componentMenu, true,
                    ComponentMenuVerdict.AlreadySet, authored.componentMenu, "authored by hand");
            }

            ComponentMenuVerdict verdict = classifier.Classify(type, out string reason);
            string proposed = verdict == ComponentMenuVerdict.Internal
                ? string.Empty
                : ProposePath(type, packageRoot, scriptPath, classifier, ref reason);

            return new ComponentMenuEntry(type, scriptPath, packageRoot, null, false, verdict, proposed, reason);
        }

        private static string ProposePath(
            Type type,
            string packageRoot,
            string scriptPath,
            ComponentMenuClassifier classifier,
            ref string reason)
        {
            string nativeRoot = ComponentMenuTaxonomy.TryNativeRoot(type, classifier.RequiredComponentsOf(type));
            if (nativeRoot != null)
            {
                reason = $"{reason}; native root '{nativeRoot}'";
                return ComponentMenuTaxonomy.NativePath(type, nativeRoot);
            }

            return ComponentMenuTaxonomy.ProductPath(type, FirstPartyScriptPaths.PackageIdOf(packageRoot), scriptPath);
        }

        private static int CompareEntries(ComponentMenuEntry a, ComponentMenuEntry b)
        {
            int byPackage = string.CompareOrdinal(a.PackageId, b.PackageId);
            if (byPackage != 0)
                return byPackage;

            int byMenu = string.CompareOrdinal(a.ProposedMenu, b.ProposedMenu);
            return byMenu != 0 ? byMenu : string.CompareOrdinal(a.Type.Name, b.Type.Name);
        }
    }
}
#endif
