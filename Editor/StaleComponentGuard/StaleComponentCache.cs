#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using HierarchyX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AetherNexus.FoundationPlatform.StaleComponentGuard.Editor
{
    /// <summary>
    /// Live lookup that connects on-disk <see cref="StaleComponentScanner"/> findings to the objects the
    /// editor is showing — the open scene(s) for the hierarchy row + inspector, and any inspected asset.
    ///
    /// The disk scan is refreshed only when it can actually change: scene open/save, asset import
    /// (<see cref="EditorApplication.projectChanged"/>), and script recompile — NOT on every
    /// <see cref="EditorApplication.hierarchyChanged"/> (far too frequent, and an already-loaded object's
    /// orphan data only changes when its asset is rewritten). Findings are keyed by
    /// (assetGUID, localFileId) via <see cref="GlobalObjectId"/>, so a component maps to its finding with
    /// no per-object bookkeeping.
    /// </summary>
    [InitializeOnLoad]
    public static class StaleComponentCache
    {
        private static bool _sceneDirty = true;

        // (assetGUID + ":" + localFileId) -> finding. Covers open scenes (proactive) + inspected assets (on demand).
        private static readonly Dictionary<string, StaleFinding> ByGuidFile = new(StringComparer.Ordinal);
        // GameObject instance ids that own at least one stale component in an open scene (for the row decorator).
        private static readonly HashSet<int> StaleGoInstanceIds = new();
        // Asset GUIDs already scanned on demand for the inspector, so repeated repaints don't rescan.
        private static readonly HashSet<string> ScannedAssetGuids = new(StringComparer.Ordinal);
        // Findings in the open scene(s) only, plus each one's owning GameObject instance id (for the panel/window).
        private static readonly List<StaleFinding> SceneFindings = new();
        private static readonly Dictionary<string, int> GoByFindingKey = new(StringComparer.Ordinal);
        // Stale scene component -> its finding, keyed by the live component's instance id. Lets the inspector
        // badge answer for scene objects without a GlobalObjectId lookup on every repaint.
        private static readonly Dictionary<int, StaleFinding> SceneFindingByComponentId = new();

        static StaleComponentCache()
        {
            EditorSceneManager.sceneOpened += (_, __) => Invalidate();
            EditorSceneManager.sceneSaved += _ => Invalidate();
            EditorSceneManager.newSceneCreated += (_, __, ___) => Invalidate();
            EditorApplication.projectChanged += Invalidate;
            AssemblyReloadEvents.afterAssemblyReload += Invalidate;
        }

        /// <summary>Mark everything stale-scan-related for recompute and repaint the surfaces.</summary>
        public static void Invalidate()
        {
            _sceneDirty = true;
            ScannedAssetGuids.Clear();
            HierarchyXRegistry.InvalidateCache(); // decoration cache only clears on hierarchyChanged otherwise
        }

        // ---- Queries ---------------------------------------------------------------------------

        /// <summary>Stale components in the currently open scene(s). Cheap after the first compute.</summary>
        public static IReadOnlyList<StaleFinding> GetSceneFindings()
        {
            EnsureSceneFresh();
            return SceneFindings;
        }

        /// <summary>Ping + select the GameObject that owns the finding's stale component, if resolvable.</summary>
        public static void SelectInScene(StaleFinding finding)
        {
            EnsureSceneFresh();
            var sceneGuid = AssetDatabase.AssetPathToGUID(finding.AssetPath);
            if (GoByFindingKey.TryGetValue(Key(sceneGuid, (ulong)finding.ComponentFileId), out var id))
            {
#if UNITY_2023_1_OR_NEWER
                var obj = EditorUtility.EntityIdToObject(id);
#else
                var obj = EditorUtility.InstanceIDToObject(id);
#endif
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }
        }

        /// <summary>True if <paramref name="go"/> owns a stale component in the open scene(s).</summary>
        public static bool IsStale(GameObject go)
        {
            if (!StaleComponentGuardSettings.Enabled || go == null)
                return false;
            EnsureSceneFresh();
            return StaleGoInstanceIds.Contains(go.GetInstanceID());
        }

        /// <summary>Finding for a specific inspected component (scene object or asset), if it is stale.</summary>
        public static bool TryGet(UnityEngine.Object component, out StaleFinding finding)
        {
            finding = default;
            if (!StaleComponentGuardSettings.Enabled || component == null)
                return false;
            EnsureSceneFresh();

            // Fast path: a stale scene component is keyed by its live instance id — no GlobalObjectId needed.
            if (SceneFindingByComponentId.TryGetValue(component.GetInstanceID(), out finding))
                return true;

            // Otherwise it may be an inspected asset (prefab/.asset) not in an open scene. One slow lookup.
            var gid = GlobalObjectId.GetGlobalObjectIdSlow(component);
            if (gid.identifierType != 1 && gid.identifierType != 3) // not an imported/source asset object
                return false;

            var guid = gid.assetGUID.ToString();
            if (string.IsNullOrEmpty(guid))
                return false;

            if (ByGuidFile.TryGetValue(Key(guid, gid.targetObjectId), out finding))
                return true;

            if (ScannedAssetGuids.Add(guid)) // scan this asset once, then cache
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var f in StaleComponentScanner.ScanAsset(path))
                    ByGuidFile[Key(guid, (ulong)f.ComponentFileId)] = f;
                return ByGuidFile.TryGetValue(Key(guid, gid.targetObjectId), out finding);
            }

            return false;
        }

        // ---- Open-scene rebuild ----------------------------------------------------------------

        private static void EnsureSceneFresh()
        {
            if (!_sceneDirty)
                return;
            _sceneDirty = false;

            ByGuidFile.Clear();
            StaleGoInstanceIds.Clear();
            ScannedAssetGuids.Clear();
            SceneFindings.Clear();
            GoByFindingKey.Clear();
            SceneFindingByComponentId.Clear();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
                    continue;

                var sceneGuid = AssetDatabase.AssetPathToGUID(scene.path);
                if (string.IsNullOrEmpty(sceneGuid))
                    continue;

                var findings = StaleComponentScanner.ScanAsset(scene.path);
                if (findings.Count == 0)
                    continue;

                foreach (var f in findings)
                {
                    ByGuidFile[Key(sceneGuid, (ulong)f.ComponentFileId)] = f;
                    SceneFindings.Add(f);
                }

                MapSceneToInstances(scene, sceneGuid);
            }
        }

        // Resolve stale component fileIds in this scene to their live GameObjects (row decorator + panel select).
        private static void MapSceneToInstances(Scene scene, string sceneGuid)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var comps = root.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < comps.Length; i++)
                {
                    var c = comps[i];
                    if (c == null)
                        continue; // missing script — handled elsewhere
                    var gid = GlobalObjectId.GetGlobalObjectIdSlow(c);
                    if (gid.identifierType == 0)
                        continue;
                    var key = Key(sceneGuid, gid.targetObjectId);
                    if (ByGuidFile.TryGetValue(key, out var finding))
                    {
                        StaleGoInstanceIds.Add(c.gameObject.GetInstanceID());
                        GoByFindingKey[key] = c.gameObject.GetInstanceID();
                        SceneFindingByComponentId[c.GetInstanceID()] = finding;
                    }
                }
            }
        }

        private static string Key(string guid, ulong fileId) => guid + ":" + fileId;
    }
}
#endif
