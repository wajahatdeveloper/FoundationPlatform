#if UNITY_EDITOR
using System;

namespace AetherNexus.FoundationPlatform.Editor.Utilities
{
    /// <summary>
    /// Shared asset-path-under-root comparison, used by both <see cref="HierarchyPathPolicy"/> and
    /// the UI Validation conventions — previously two independent copies of the same algorithm.
    /// </summary>
    internal static class PathComparisonUtility
    {
        internal static bool IsPathUnder(string path, string root)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(root))
                return false;

            string normalizedPath = path.Replace('\\', '/');
            string normalizedRoot = root.Replace('\\', '/').TrimEnd('/');
            return normalizedPath.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif
