#if UNITY_EDITOR
using System;

namespace FoundationPlatform.Editor.Utilities
{
    public static class HierarchyPathPolicy
    {
        public const string DataRoot = "Assets/Data";
        public const string ScriptsRoot = "Assets/Scripts";

        public static bool IsUnderDataRoot(string path)
        {
            return IsPathUnderRoot(path, DataRoot);
        }

        public static bool IsUnderScriptsRoot(string path)
        {
            return IsPathUnderRoot(path, ScriptsRoot);
        }

        public static bool TryClassify(
            string assetPath,
            DataFolderMappingConfig config,
            out HierarchyRoot root,
            out HierarchyBucket bucket,
            out string domain,
            out string reason)
        {
            root = HierarchyRoot.Data;
            bucket = HierarchyBucket.Domains;
            domain = string.Empty;
            reason = string.Empty;

            if (config == null)
                throw new InvalidOperationException("[HierarchyPathPolicy] DataFolderMappingConfig is required.");

            return config.TryClassifyHierarchyPath(assetPath, out root, out bucket, out domain, out reason);
        }

        private static bool IsPathUnderRoot(string path, string root)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string normalizedPath = path.Replace('\\', '/');
            string normalizedRoot = root.Replace('\\', '/').TrimEnd('/');
            return normalizedPath.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif
