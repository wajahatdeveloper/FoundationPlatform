#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AetherNexus.FoundationPlatform.Editor.Utilities
{
public static class EditorSceneScanCache
{
    private static bool _hasChanges;
    public static bool HasChanges
    {
        get => _hasChanges;
        set => _hasChanges = value;
    }

    private static readonly HashSet<Type> _registeredTypes = new HashSet<Type>();
    private static readonly Dictionary<Type, List<Component>> _cache = new Dictionary<Type, List<Component>>();
    private static bool _isInitialized;
    private static bool _isDirty;

    static EditorSceneScanCache()
    {
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private static void OnHierarchyChanged()
    {
        _isDirty = true;
        _hasChanges = true;
    }

    public static void RegisterType<T>() where T : Component
    {
        RegisterType(typeof(T));
    }

    public static void RegisterType(Type type)
    {
        if (type == null) return;
        if (!typeof(Component).IsAssignableFrom(type))
        {
            Debug.LogError($"Type {type.FullName} must be a Component to scan in scene cache.");
            return;
        }

        if (_registeredTypes.Add(type))
        {
            _isDirty = true;
        }
    }

    public static IReadOnlyList<T> GetSceneComponents<T>() where T : Component
    {
        var type = typeof(T);
        RegisterType(type);
        InitializeIfNeeded();

        if (_cache.TryGetValue(type, out var list))
        {
            var result = new List<T>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is T typedComp && typedComp != null)
                {
                    result.Add(typedComp);
                }
            }
            return result;
        }

        return Array.Empty<T>();
    }

    private static void InitializeIfNeeded()
    {
        if (_isInitialized && !_isDirty) return;
        _isInitialized = true;
        _isDirty = false;

        foreach (var type in _registeredTypes)
        {
            if (!_cache.TryGetValue(type, out var list))
            {
                list = new List<Component>();
                _cache[type] = list;
            }
            else
            {
                list.Clear();
            }
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root == null) continue;

                foreach (var type in _registeredTypes)
                {
                    var comps = root.GetComponentsInChildren(type, true);
                    if (comps != null && comps.Length > 0)
                    {
                        _cache[type].AddRange(comps);
                    }
                }
            }
        }
    }

    public static void Clear()
    {
        _cache.Clear();
        _isInitialized = false;
        _isDirty = false;
    }
}
}
#endif
