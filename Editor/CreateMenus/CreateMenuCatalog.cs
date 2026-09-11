#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using AetherNexus.FoundationPlatform.DesignerSurfaces.Editor;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.CreateMenus.Editor
{
    /// <summary>
    /// Every concrete first-party <see cref="ScriptableObject"/> with the create entry it declares
    /// today and the classifier's verdict on whether it should have one. Single index behind the
    /// coverage lint, so "authoring type with no create entry" has one definition.
    /// </summary>
    internal static class CreateMenuCatalog
    {
        /// <summary>Vendored source we do not author.</summary>
        private const string VendoredFolder = "/ThirdParty/";

        internal static List<CreateMenuEntry> Build()
        {
            var classifier = new CreateMenuClassifier();
            var scriptPaths = new Dictionary<Type, string>();
            var entries = new List<CreateMenuEntry>(128);

            foreach (Type type in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
            {
                if (type.IsAbstract || type.IsGenericTypeDefinition)
                    continue;

                if (CreateMenuClassifier.IsEditorInfrastructure(type, out _))
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

        private static CreateMenuEntry BuildEntry(Type type, string scriptPath, CreateMenuClassifier classifier)
        {
            string packageRoot = FirstPartyScriptPaths.PackageRootOf(scriptPath);
            var authored = type.GetCustomAttribute<CreateAssetMenuAttribute>(false);

            if (authored != null)
            {
                return new CreateMenuEntry(
                    type, scriptPath, packageRoot, authored.menuName,
                    CreateMenuVerdict.AlreadySet, "authored by hand");
            }

            CreateMenuVerdict verdict = classifier.Classify(type, out string reason);
            return new CreateMenuEntry(type, scriptPath, packageRoot, null, verdict, reason);
        }

        private static int CompareEntries(CreateMenuEntry a, CreateMenuEntry b)
        {
            int byPackage = string.CompareOrdinal(a.PackageId, b.PackageId);
            if (byPackage != 0)
                return byPackage;

            int byVerdict = a.Verdict.CompareTo(b.Verdict);
            return byVerdict != 0 ? byVerdict : string.CompareOrdinal(a.Type.Name, b.Type.Name);
        }
    }
}
#endif
