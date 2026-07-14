using System;
using UnityEngine;

/// <summary>
/// Unity-null-safe helpers for resolving components without using the null-coalescing operator,
/// which uses reference identity and ignores Unity's fake-null lifetime.
/// </summary>
namespace AetherNexus.FoundationPlatform.Extensions
{
public static class ComponentExtensions
{
    /// <summary>
    /// <see cref="Component.GetComponent{T}"/> on self, then <see cref="Component.GetComponentInParent{T}"/> if missing.
    /// </summary>
    public static T GetComponentInSelfOrParents<T>(this Component self) where T : Component
    {
        if (self == null) return null;
        var c = self.GetComponent<T>();
        if (c != null) return c;
        return self.GetComponentInParent<T>();
    }

    /// <summary>
    /// <see cref="Component.GetComponent{T}"/> on self, then <see cref="Component.GetComponentInChildren{T}(bool)"/> if missing.
    /// </summary>
    public static T GetComponentInSelfOrChildren<T>(this Component self, bool includeInactive = false) where T : Component
    {
        if (self == null) return null;
        var c = self.GetComponent<T>();
        if (c != null) return c;
        return self.GetComponentInChildren<T>(includeInactive);
    }
    
    public static T GetComponentInSelfOrParents<T>(this GameObject self) where T : Component
    {
        if (self == null) return null;
        var c = self.GetComponent<T>();
        if (c != null) return c;
        return self.GetComponentInParent<T>();
    }
    
    public static T GetComponentInSelfOrChildren<T>(this GameObject self, bool includeInactive = false) where T : Component
    {
        if (self == null) return null;
        var c = self.GetComponent<T>();
        if (c != null) return c;
        return self.GetComponentInChildren<T>(includeInactive);
    }

    /// <summary>
    /// Self and parents (see <see cref="GetComponentInSelfOrParents{T}"/>), then descendants if still missing. Use when
    /// the target component may be on a child (e.g. <c>CombatComponent</c> on root, <c>CharacterAnimator</c> on model).
    /// </summary>
    public static T GetComponentInSelfParentsOrChildren<T>(this Component self, bool includeInactive = false) where T : Component
    {
        if (self == null) return null;
        var c = self.GetComponentInSelfOrParents<T>();
        if (c != null) return c;
        return self.GetComponentInChildren<T>(includeInactive);
    }

    /// <summary>
    /// Resolves a <see cref="Component"/> by type on <paramref name="go"/>, then parents, then children.
    /// </summary>
    public static Component GetComponentInSelfParentsOrChildren(this GameObject go, Type type, bool includeInactive = false)
    {
        if (go == null)
            return null;
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        var component = go.GetComponent(type);
        if (component != null)
            return component;

        component = go.GetComponentInParent(type, includeInactive);
        if (component != null)
            return component;

        return go.GetComponentInChildren(type, includeInactive);
    }
}
}
