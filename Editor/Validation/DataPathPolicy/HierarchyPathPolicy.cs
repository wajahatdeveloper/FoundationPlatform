#if UNITY_EDITOR
namespace AetherNexus.FoundationPlatform.Editor.Utilities
{
    public static class HierarchyPathPolicy
    {
        public const string DataRoot = "Assets/Content";
        public const string ScriptsRoot = "Assets/Scripts";

        public static bool IsUnderDataRoot(string path)
        {
            return IsPathUnderRoot(path, DataRoot);
        }

        public static bool IsUnderScriptsRoot(string path)
        {
            return IsPathUnderRoot(path, ScriptsRoot);
        }

        private static bool IsPathUnderRoot(string path, string root) => PathComparisonUtility.IsPathUnder(path, root);
    }
}
#endif
