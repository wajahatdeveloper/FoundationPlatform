#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.AssetImport
{
    public static class AssetImportPathFilters
    {
        public static bool AnyImportedOrMovedPathIsType<T>(string[] paths) where T : Object
        {
            if (paths == null || paths.Length == 0)
                return false;

            for (int i = 0; i < paths.Length; i++)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(paths[i]) == typeof(T))
                    return true;
            }

            return false;
        }

        public static bool AnyKnownPathInSet(string[] paths, HashSet<string> knownPaths)
        {
            if (paths == null || paths.Length == 0 || knownPaths == null || knownPaths.Count == 0)
                return false;

            for (int i = 0; i < paths.Length; i++)
            {
                if (knownPaths.Contains(paths[i]))
                    return true;
            }

            return false;
        }
    }
}
#endif
