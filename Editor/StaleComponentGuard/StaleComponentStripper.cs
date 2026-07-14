#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoundationPlatform.StaleComponentGuard.Editor
{
    /// <summary>
    /// The opt-in fix: re-serialize an asset so Unity rewrites it without the orphan keys (Unity already
    /// dropped them from the in-memory object on load; saving persists the clean form). Destructive — the
    /// old values are gone for good — so every path is behind a confirm dialog. Use
    /// <c>[FormerlySerializedAs]</c> on the renamed field first if the data must be migrated instead.
    /// </summary>
    public static class StaleComponentStripper
    {
        /// <summary>Confirm, then strip the one asset this finding belongs to.</summary>
        public static void StripWithConfirm(StaleFinding finding)
        {
            if (string.IsNullOrEmpty(finding.AssetPath))
                return;
            if (!EditorUtility.DisplayDialog(
                    "Strip stale data?",
                    $"Permanently discard serialized data for fields no longer defined by\n{finding.TypeName}\n\n" +
                    $"Asset: {finding.AssetPath}\nFields: {finding.OrphanList}\n\n" +
                    "This re-saves the asset. The old values cannot be recovered.",
                    "Strip", "Cancel"))
                return;

            if (StripAsset(finding.AssetPath))
                StaleComponentCache.Invalidate();
        }

        /// <summary>Confirm once, then strip every distinct asset in the set (batched).</summary>
        public static void StripAllWithConfirm(IEnumerable<StaleFinding> findings)
        {
            var paths = findings.Where(f => !string.IsNullOrEmpty(f.AssetPath))
                                .Select(f => f.AssetPath)
                                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                                .ToList();
            if (paths.Count == 0)
                return;
            if (!EditorUtility.DisplayDialog(
                    "Strip ALL stale data?",
                    $"Permanently discard orphan serialized data across {paths.Count} asset(s) by re-saving them.\n\n" +
                    "The old values cannot be recovered. Continue?",
                    "Strip All", "Cancel"))
                return;

            // Scenes must be saved through the scene manager; other assets go in an AssetDatabase batch.
            var assetPaths = paths.Where(p => !p.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase)).ToList();
            var scenePaths = paths.Where(p => p.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase)).ToList();

            bool any = false;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var p in assetPaths)
                    any |= StripAsset(p);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            foreach (var p in scenePaths)
                any |= StripScene(p);

            if (any)
                StaleComponentCache.Invalidate();
        }

        // ---- Per-asset dispatch ----------------------------------------------------------------

        private static bool StripAsset(string path)
        {
            var ext = Path.GetExtension(path);
            if (ext.Equals(".unity", System.StringComparison.OrdinalIgnoreCase))
                return StripScene(path);
            if (ext.Equals(".prefab", System.StringComparison.OrdinalIgnoreCase))
                return StripPrefab(path);
            return StripScriptableObject(path);
        }

        private static bool StripPrefab(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                return false;
            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool StripScriptableObject(string path)
        {
            var obj = AssetDatabase.LoadMainAssetAtPath(path);
            if (obj == null)
                return false;
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssetIfDirty(obj);
            return true;
        }

        private static bool StripScene(string path)
        {
            // Already open? Save in place.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.isLoaded && string.Equals(s.path, path, System.StringComparison.OrdinalIgnoreCase))
                {
                    EditorSceneManager.MarkSceneDirty(s);
                    return EditorSceneManager.SaveScene(s);
                }
            }

            // Not open: preserve the current setup, open→save→restore.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                EditorSceneManager.MarkSceneDirty(scene);
                return EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (setup != null && setup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }
    }
}
#endif
