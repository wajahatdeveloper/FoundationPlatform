#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using AetherNexus.FoundationPlatform.DesignerSurfaces.Editor;
using AetherNexus.FoundationPlatform.Utilities.Menus;

namespace AetherNexus.FoundationPlatform.CreateMenus.Editor
{
    /// <summary>
    /// Decides whether a first-party <c>ScriptableObject</c> is an asset a designer authors, or one
    /// of the two kinds that deliberately have no create entry: a section of a larger config asset,
    /// and a single instance an editor generator owns. A type that matches no signal is reported as
    /// unclassified rather than quietly excluded.
    /// </summary>
    internal sealed class CreateMenuClassifier
    {
        /// <summary>Base type that marks a section authored inside its owning config asset.</summary>
        private const string ConfigSectionBase = "GamePropertySectionBase";

        /// <summary>Editor infrastructure that happens to derive from <c>ScriptableObject</c>.</summary>
        private static readonly HashSet<string> EditorInfrastructureBases = new()
        {
            "Editor", "EditorWindow", "ScriptableWizard", "AssetImporter", "AssetPostprocessor",
            "ScriptableSingleton`1", "AssetImporterEditor", "EditorTool", "EditorToolContext",
        };

        private static readonly Regex CreationSite = new(
            @"CreateInstance\s*(?:<\s*(?<generic>[A-Za-z_][A-Za-z0-9_]*)\s*>|\(\s*typeof\s*\(\s*(?<typeOf>[A-Za-z_][A-Za-z0-9_]*)\s*\))",
            RegexOptions.Compiled);

        private readonly Dictionary<string, CreationSites> _creationSites;

        internal CreateMenuClassifier()
        {
            _creationSites = ScanCreationSites();
        }

        internal CreateMenuVerdict Classify(Type type, out string reason)
        {
            string configSection = FindBase(type, ConfigSectionBase);
            if (configSection != null)
            {
                reason = $"section of a config asset ({configSection})";
                return CreateMenuVerdict.ConfigSection;
            }

            if (_creationSites.TryGetValue(type.Name, out CreationSites sites) && sites.EditorOnly)
            {
                reason = $"created only by editor code ({sites.FirstEditorSite})";
                return CreateMenuVerdict.GeneratorOwned;
            }

            if (type.GetCustomAttribute<DesignerFeatureAttribute>(false) != null)
            {
                reason = "tagged [DesignerFeature], so a designer is meant to find it";
                return CreateMenuVerdict.Authoring;
            }

            reason = "no create menu, no config-section base, no generator owner";
            return CreateMenuVerdict.Unclassified;
        }

        /// <summary>True for editor infrastructure types, which are never project assets.</summary>
        internal static bool IsEditorInfrastructure(Type type, out string baseName)
        {
            for (Type current = type.BaseType; current != null; current = current.BaseType)
            {
                if (EditorInfrastructureBases.Contains(current.Name))
                {
                    baseName = current.Name;
                    return true;
                }
            }

            baseName = null;
            return false;
        }

        private static string FindBase(Type type, string baseName)
        {
            for (Type current = type.BaseType; current != null; current = current.BaseType)
            {
                if (string.Equals(current.Name, baseName, StringComparison.Ordinal))
                    return current.Name;
            }

            return null;
        }

        /// <summary>
        /// One text pass over first-party source, recording every <c>CreateInstance</c> site per type
        /// name and whether any of them sits in runtime code. A type created only under an
        /// <c>Editor/</c> folder has a tool deciding how many instances exist.
        /// </summary>
        private static Dictionary<string, CreationSites> ScanCreationSites()
        {
            var sites = new Dictionary<string, CreationSites>(256);

            foreach (string file in Directory.EnumerateFiles(
                         FirstPartyScriptPaths.PackagesFolder, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                if (!FirstPartyScriptPaths.IsFirstParty(normalized))
                    continue;

                string text = File.ReadAllText(file);
                if (text.IndexOf("CreateInstance", StringComparison.Ordinal) < 0)
                    continue;

                bool isEditorFile = FirstPartyScriptPaths.IsEditorPath(normalized);

                foreach (Match match in CreationSite.Matches(text))
                {
                    string typeName = match.Groups["generic"].Success
                        ? match.Groups["generic"].Value
                        : match.Groups["typeOf"].Value;

                    if (!sites.TryGetValue(typeName, out CreationSites existing))
                        existing = new CreationSites();

                    existing.Record(isEditorFile, normalized);
                    sites[typeName] = existing;
                }
            }

            return sites;
        }

        private struct CreationSites
        {
            private int _editorCount;
            private int _runtimeCount;

            public string FirstEditorSite { get; private set; }

            public bool EditorOnly => _editorCount > 0 && _runtimeCount == 0;

            public void Record(bool isEditorFile, string path)
            {
                if (isEditorFile)
                {
                    _editorCount++;
                    FirstEditorSite ??= path;
                    return;
                }

                _runtimeCount++;
            }
        }
    }
}
#endif
