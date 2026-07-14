#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AetherNexus.FoundationPlatform.Editor.Utilities
{
public static class EditorAssetScanCache
{
    private static bool _hasChanges;
    public static bool HasChanges
    {
        get => _hasChanges;
        set => _hasChanges = value;
    }

    private static readonly HashSet<Type> _registeredTypes = new HashSet<Type>();
    private static readonly Dictionary<Type, List<Object>> _cache = new Dictionary<Type, List<Object>>();
    private static bool _isInitialized;

    public static void RegisterType<T>() where T : Object
    {
        RegisterType(typeof(T));
    }

    public static void RegisterType(Type type)
    {
        if (type == null) return;
        if (_registeredTypes.Add(type))
        {
            if (_isInitialized)
            {
                ScanTypeInternal(type);
            }
        }
    }

    public static IReadOnlyList<T> GetAssets<T>() where T : Object
    {
        var type = typeof(T);
        RegisterType(type);
        InitializeIfNeeded();

        if (_cache.TryGetValue(type, out var list))
        {
            var result = new List<T>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is T typedAsset)
                {
                    result.Add(typedAsset);
                }
            }
            return result;
        }

        return Array.Empty<T>();
    }

    public static IReadOnlyList<Object> GetAssets(Type type)
    {
        if (type == null) return Array.Empty<Object>();
        RegisterType(type);
        InitializeIfNeeded();

        if (_cache.TryGetValue(type, out var list))
        {
            return list;
        }

        return Array.Empty<Object>();
    }

    private static void InitializeIfNeeded()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        foreach (var type in _registeredTypes)
        {
            ScanTypeInternal(type);
        }
    }

    private static void ScanTypeInternal(Type type)
    {
        var list = new List<Object>();
        _cache[type] = list;

        if (typeof(Component).IsAssignableFrom(type))
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null)
                {
                    var comps = go.GetComponentsInChildren(type, true);
                    foreach (var comp in comps)
                    {
                        if (comp != null)
                        {
                            list.Add(comp);
                        }
                    }
                }
            }
        }
        else
        {
            string filter = "t:" + type.Name;
            string[] guids = AssetDatabase.FindAssets(filter);
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                var asset = AssetDatabase.LoadAssetAtPath(path, type);
                if (asset != null)
                {
                    list.Add(asset);
                }
            }
        }
    }

    public static void Clear()
    {
        _cache.Clear();
        _isInitialized = false;
    }

    internal static void HandleAssetChanges(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (importedAssets.Length > 0 || deletedAssets.Length > 0 || movedAssets.Length > 0 || movedFromAssetPaths.Length > 0)
        {
            _hasChanges = true;
        }

        if (!_isInitialized) return;

        if (deletedAssets.Length > 0 || movedFromAssetPaths.Length > 0)
        {
            var pathsToRemove = new HashSet<string>(deletedAssets.Concat(movedFromAssetPaths));
            foreach (var kvp in _cache)
            {
                kvp.Value.RemoveAll(obj => obj == null || pathsToRemove.Contains(AssetDatabase.GetAssetPath(obj)));
            }
        }

        var pathsToProcess = importedAssets.Concat(movedAssets);
        foreach (var path in pathsToProcess)
        {
            if (string.IsNullOrEmpty(path)) continue;

            var mainAsset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (mainAsset == null) continue;

            var mainType = mainAsset.GetType();

            foreach (var regType in _registeredTypes)
            {
                if (regType.IsAssignableFrom(mainType))
                {
                    UpdateAssetInCache(regType, mainAsset);
                }
                else if (mainAsset is GameObject go && typeof(Component).IsAssignableFrom(regType))
                {
                    var comps = go.GetComponentsInChildren(regType, true);
                    foreach (var comp in comps)
                    {
                        UpdateAssetInCache(regType, comp);
                    }
                }
            }
        }
    }

    private static void UpdateAssetInCache(Type regType, Object asset)
    {
        if (!_cache.TryGetValue(regType, out var list))
        {
            list = new List<Object>();
            _cache[regType] = list;
        }

        string path = AssetDatabase.GetAssetPath(asset);
        list.RemoveAll(obj => obj == null || AssetDatabase.GetAssetPath(obj) == path);
        list.Add(asset);
    }
}

public class EditorAssetScanCachePostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        EditorAssetScanCache.HandleAssetChanges(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
    }
}
}
#endif
