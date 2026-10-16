using System;
using Microsoft.CodeAnalysis;

namespace AetherNexus.Analyzers
{
    /// <summary>
    /// Project rules apply to first-party source only: embedded <c>com.aethernexus.*</c> packages and
    /// <c>Assets/Scripts</c>. Asset packs and vendored folders keep upstream code as shipped.
    /// </summary>
    internal static class FirstPartyScope
    {
        private static readonly string[] Roots = { "/Packages/com.aethernexus.", "/Assets/Scripts/" };
        private static readonly string[] Vendored = { "/CoroutineX/", "/RagdollHelper/", "/Plugins/", "/ThirdParty/", "/HighlighterPackage/", "/IndicatorPackage/" };

        public static bool Contains(SyntaxTree tree)
        {
            string path = tree.FilePath;
            if (string.IsNullOrEmpty(path))
                return false;
            path = "/" + path.Replace('\\', '/');

            bool inRoot = false;
            foreach (string root in Roots)
            {
                if (path.IndexOf(root, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    inRoot = true;
                    break;
                }
            }
            if (!inRoot)
                return false;

            foreach (string vendored in Vendored)
            {
                if (path.IndexOf(vendored, StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
            }
            return true;
        }

        public static bool IsEditorCode(SyntaxTree tree, Compilation compilation)
        {
            string name = compilation.AssemblyName ?? string.Empty;
            if (name.EndsWith(".Editor", StringComparison.Ordinal) || name.Contains(".Editor."))
                return true;
            return tree.FilePath.Replace('\\', '/').IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
