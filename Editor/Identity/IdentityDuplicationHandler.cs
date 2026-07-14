#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using AetherNexus.FoundationPlatform.Identity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Ensures duplicated GameObjects or prefab instances with IdentityComponent get a new unique ID.
/// Tracks known instance IDs so we can identify exactly which component is the new duplicate
/// rather than doing an after-the-fact scan that can pick the wrong "original".
/// </summary>
namespace AetherNexus.FoundationPlatform.Editor.Identity
{
[InitializeOnLoad]
public static class IdentityDuplicationHandler
{
    // Maps Identity value -> the Unity instance ID of the component we consider authoritative.
    // Keyed by string so it survives domain reloads (Identity is a struct, string is serialization-safe).
    private static readonly Dictionary<string, int> s_knownIdentities = new();

    static IdentityDuplicationHandler()
    {
        RebuildSnapshot();
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    /// <summary>
    /// Rebuild the snapshot from scratch. Call after domain reload or scene load.
    /// </summary>
    private static void RebuildSnapshot()
    {
        s_knownIdentities.Clear();

        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabStageScene = prefabStage != null ? prefabStage.scene : default;

        foreach (var c in Object.FindObjectsByType<IdentityComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!IsTracked(c, prefabStageScene)) continue;
            var id = c.Identity;
            if (!id.IsValid) continue;
            // In case of a pre-existing duplicate (e.g. scene saved dirty), just take first found.
            s_knownIdentities.TryAdd(id.Value, c.GetInstanceID());
        }
    }

    private static void OnHierarchyChanged()
    {
        EditorApplication.delayCall -= FixDuplicates;
        EditorApplication.delayCall += FixDuplicates;
    }

    private static void FixDuplicates()
    {
        EditorApplication.delayCall -= FixDuplicates;

        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabStageScene = prefabStage != null ? prefabStage.scene : default;

        var allComponents = Object.FindObjectsByType<IdentityComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(c => c != null && c.gameObject != null && IsTracked(c, prefabStageScene))
            .ToList();

        // --- Step 1: find components whose ID already exists under a DIFFERENT instance ---
        foreach (var c in allComponents)
        {
            var id = c.Identity;
            if (!id.IsValid)
            {
                // No ID yet — not a duplicate, nothing to do.
                continue;
            }

            int instanceId = c.GetInstanceID();

            if (s_knownIdentities.TryGetValue(id.Value, out int knownInstance))
            {
                if (knownInstance != instanceId)
                {
                    // This component has a clashing ID and is NOT the one we originally
                    // registered — it must be the new duplicate.
                    Undo.RecordObject(c, "Regenerate duplicate identity");
                    c.GenerateDesignTimeId();

                    // Register the newly generated ID.
                    s_knownIdentities[c.Identity.Value] = instanceId;
                }
                // else: same instance, same ID — nothing changed.
            }
            else
            {
                // Brand-new ID we haven't seen before — register it.
                s_knownIdentities[id.Value] = instanceId;
            }
        }

        // --- Step 2: prune destroyed components from snapshot ---
        // Build a set of all live instance IDs so stale entries don't linger.
        var liveIds = new HashSet<int>(allComponents.Select(c => c.GetInstanceID()));
        var toRemove = s_knownIdentities
            .Where(kv => !liveIds.Contains(kv.Value))
            .Select(kv => kv.Key)
            .ToList();
        foreach (var key in toRemove)
            s_knownIdentities.Remove(key);
    }

    private static bool IsTracked(IdentityComponent c, UnityEngine.SceneManagement.Scene prefabStageScene)
    {
        return c.gameObject.scene.isLoaded
            && c.gameObject.scene != prefabStageScene
            && !PrefabUtility.IsPartOfPrefabAsset(c);
    }
}

/// <summary>
/// Clears identity on duplicated prefab assets so they do not retain the source prefab's ID.
/// </summary>
public class IdentityPrefabAssetPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        var importedPrefabs = importedAssets
            .Where(p => p.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (importedPrefabs.Count == 0) return;

        var existingIdentities = new HashSet<Messaging.Identity>();
        var allPrefabGuids = AssetDatabase.FindAssets("t:Prefab");
        var importedSet = new HashSet<string>(importedPrefabs);

        foreach (var guid in allPrefabGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (importedSet.Contains(path)) continue;

            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) continue;

            foreach (var comp in root.GetComponentsInChildren<IdentityComponent>(true))
            {
                if (comp != null && comp.Identity.IsValid)
                    existingIdentities.Add(comp.Identity);
            }
        }

        foreach (var path in importedPrefabs)
        {
            GameObject contentsRoot = null;
            try
            {
                contentsRoot = PrefabUtility.LoadPrefabContents(path);
                if (contentsRoot == null) continue;

                var toClear = contentsRoot.GetComponentsInChildren<IdentityComponent>(true)
                    .Where(c => c != null && c.Identity.IsValid && existingIdentities.Contains(c.Identity))
                    .ToList();

                if (toClear.Count == 0) continue;

                foreach (var comp in toClear)
                    comp.ClearIdentity();

                PrefabUtility.SaveAsPrefabAsset(contentsRoot, path);
            }
            finally
            {
                if (contentsRoot != null)
                    PrefabUtility.UnloadPrefabContents(contentsRoot);
            }
        }
    }
}
}
#endif