#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.Attributes;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.Editor.Utilities
{
    /// <summary>
    /// The one resolver for <see cref="ContentHomeAttribute"/>: which types declare a home, the asset-path
    /// patterns a type resolves to (its own declaration or its nearest declaring base), whether a path sits
    /// inside them, and the concrete folders they expand to.
    /// </summary>
    public static class ContentHomes
    {
        public const string ContentRoot = "Assets/Content";

        private static List<Type> s_declaringTypes;
        private static readonly Dictionary<Type, IReadOnlyList<string>> s_patternsByDeclaringType = new();
        private static readonly Dictionary<Type, string> s_scriptPathByType = new();

        /// <summary>Types that declare <see cref="ContentHomeAttribute"/> themselves, sorted by full name.</summary>
        public static IReadOnlyList<Type> DeclaringTypes
        {
            get
            {
                if (s_declaringTypes != null)
                    return s_declaringTypes;

                s_declaringTypes = new List<Type>();
                foreach (Type type in TypeCache.GetTypesWithAttribute<ContentHomeAttribute>())
                {
                    if (Attribute.IsDefined(type, typeof(ContentHomeAttribute), false))
                        s_declaringTypes.Add(type);
                }

                s_declaringTypes.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));
                return s_declaringTypes;
            }
        }

        public static bool TryGetDeclaringType(Type type, out Type declaringType)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                if (!Attribute.IsDefined(current, typeof(ContentHomeAttribute), false))
                    continue;
                declaringType = current;
                return true;
            }

            declaringType = null;
            return false;
        }

        public static bool HasHome(Type type) => TryGetDeclaringType(type, out _);

        /// <summary>Asset-path patterns (rooted at <c>Assets/Content</c>, or unbounded) for the type; empty when it has no home.</summary>
        public static IReadOnlyList<string> GetPatterns(Type type)
        {
            if (!TryGetDeclaringType(type, out Type declaringType))
                return Array.Empty<string>();

            if (s_patternsByDeclaringType.TryGetValue(declaringType, out IReadOnlyList<string> cached))
                return cached;

            var attribute = (ContentHomeAttribute)Attribute.GetCustomAttribute(declaringType, typeof(ContentHomeAttribute), false);
            var patterns = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < attribute.Patterns.Length; i++)
            {
                string assetPattern = ToAssetPattern(attribute.Patterns[i].Trim());
                if (seen.Add(assetPattern))
                    patterns.Add(assetPattern);
            }

            s_patternsByDeclaringType.Add(declaringType, patterns);
            return patterns;
        }

        public static string ToAssetPattern(string relativePattern)
        {
            return relativePattern.StartsWith(ContentHomeAttribute.AnywhereToken, StringComparison.Ordinal)
                ? relativePattern
                : ContentRoot + "/" + relativePattern;
        }

        public static bool TypeHasAnyWildcardPattern(Type type)
        {
            IReadOnlyList<string> patterns = GetPatterns(type);
            for (int i = 0; i < patterns.Count; i++)
            {
                if (patterns[i].IndexOf('*') >= 0)
                    return true;
            }

            return false;
        }

        public static bool IsPathInsideHome(Type type, string assetPath, IReadOnlyList<string> knownDomains)
        {
            IReadOnlyList<string> patterns = GetPatterns(type);
            for (int i = 0; i < patterns.Count; i++)
            {
                if (HierarchyPatternMatcher.Match(assetPath, patterns[i], knownDomains, out _, out _))
                    return true;
            }

            return false;
        }

        /// <summary>Existing project folders (never under <c>Packages/</c>) one pattern expands to.</summary>
        public static List<string> ExpandExistingFolders(string assetPattern, IReadOnlyList<string> knownDomains)
        {
            var folders = new List<string>();
            foreach (string folder in HierarchyPatternMatcher.ExpandConcreteFolders(assetPattern, knownDomains))
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    continue;
                if (folder.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!ContainsIgnoreCase(folders, folder))
                    folders.Add(folder);
            }

            return folders;
        }

        public static List<string> ResolveConcreteFolders(Type type, IReadOnlyList<string> knownDomains)
        {
            var resolved = new List<string>();
            IReadOnlyList<string> patterns = GetPatterns(type);
            for (int i = 0; i < patterns.Count; i++)
            {
                List<string> folders = ExpandExistingFolders(patterns[i], knownDomains);
                for (int f = 0; f < folders.Count; f++)
                {
                    if (!ContainsIgnoreCase(resolved, folders[f]))
                        resolved.Add(folders[f]);
                }
            }

            return resolved;
        }

        /// <summary>The single folder a new asset of the type files into; false when there are none or several.</summary>
        public static bool TryGetSuggestedFolder(Type type, IReadOnlyList<string> knownDomains, out string folderPath, out string reason)
        {
            folderPath = null;
            reason = string.Empty;

            if (!HasHome(type))
            {
                reason = "Type '" + type.FullName + "' declares no [ContentHome].";
                return false;
            }

            List<string> concrete = ResolveConcreteFolders(type, knownDomains);
            if (concrete.Count == 1)
            {
                folderPath = concrete[0];
                return true;
            }

            reason = concrete.Count == 0
                ? "No existing folder matches the [ContentHome] of '" + type.FullName + "'."
                : "Multiple folders match the [ContentHome] of '" + type.FullName + "'.";
            return false;
        }

        public static bool TryGetTypeScriptPath(Type type, out string scriptPath)
        {
            if (s_scriptPathByType.TryGetValue(type, out scriptPath))
                return scriptPath != null;

            string[] guids = AssetDatabase.FindAssets(type.Name + " t:MonoScript");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null || script.GetClass() != type)
                    continue;

                scriptPath = path.Replace('\\', '/');
                s_scriptPathByType[type] = scriptPath;
                return true;
            }

            s_scriptPathByType[type] = null;
            return false;
        }

        private static bool ContainsIgnoreCase(List<string> list, string value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], value, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
#endif
