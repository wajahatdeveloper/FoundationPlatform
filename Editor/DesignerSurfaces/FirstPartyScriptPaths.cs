#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.DesignerSurfaces.Editor
{
    /// <summary>
    /// Maps a <see cref="Type"/> back to the first-party script file that declares it. Shared by
    /// every pass that stamps an attribute onto a type's own source, so "is this ours" and "which
    /// package owns it" have one answer.
    /// </summary>
    internal static class FirstPartyScriptPaths
    {
        internal const string PackagesPrefix = "Packages/";
        internal const string FirstPartyPrefix = PackagesPrefix + "com.aethernexus.";

        /// <summary>Project-relative folder the embedded first-party packages live in.</summary>
        internal const string PackagesFolder = "Packages";

        private const string EditorFolder = "/Editor/";

        /// <summary>True when the script compiles into an editor-only assembly by folder convention.</summary>
        internal static bool IsEditorPath(string scriptPath)
        {
            return scriptPath.IndexOf(EditorFolder, StringComparison.Ordinal) >= 0;
        }

        /// <summary>Asset path of the <c>MonoScript</c> declaring <paramref name="type"/>, or null when none matches.</summary>
        internal static string Resolve(Type type, Dictionary<Type, string> cache)
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

        internal static bool IsFirstParty(string scriptPath)
        {
            return scriptPath != null && scriptPath.StartsWith(FirstPartyPrefix, StringComparison.Ordinal);
        }

        /// <summary>"Packages/com.aethernexus.x/..." -> "Packages/com.aethernexus.x".</summary>
        internal static string PackageRootOf(string scriptPath)
        {
            int second = scriptPath.IndexOf('/', PackagesPrefix.Length);
            return second < 0 ? scriptPath : scriptPath.Substring(0, second);
        }

        /// <summary>"Packages/com.aethernexus.x" -> "com.aethernexus.x".</summary>
        internal static string PackageIdOf(string packageRoot)
        {
            return packageRoot.Substring(PackagesPrefix.Length);
        }
    }
}
#endif
