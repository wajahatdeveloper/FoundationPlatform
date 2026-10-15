using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Collection/IEnumerable extensions for GameObjects
/// </summary>
namespace AetherNexus.FoundationPlatform.Extensions
{
public static class GameObjectCollectionExtensions
{

#region IEnumerables

    /// <summary>Returns a collection of the child GameObjects of every GameObject in the source collection.</summary>
    public static IEnumerable<GameObject> Children(this IEnumerable<GameObject> source)
    {
        foreach (var item in source)
        {
            var e = item.Children().GetEnumerator();
            while (e.MoveNext())
            {
                yield return e.Current;
            }
        }
    }

    /// <summary>Destroy every GameObject in the source collection safety(check null).</summary>
    /// <param name="useDestroyImmediate">If in EditMode, should be true or pass !Application.isPlaying.</param>
    /// <param name="detachParent">set to parent = null.</param>
    public static void Destroy(this IEnumerable<GameObject> source, bool useDestroyImmediate,
        bool detachParent)
    {
        if (detachParent)
        {
            var l = new List<GameObject>(source); // avoid halloween problem
            var e = l.GetEnumerator(); // get struct enumerator for avoid unity's compiler bug(avoid boxing)
            while (e.MoveNext())
            {
                e.Current.Destroy(useDestroyImmediate, true);
            }
        }
        else
        {
            foreach (var item in source)
            {
                item.Destroy(useDestroyImmediate, false); // doesn't detach.
            }
        }
    }

    /// <summary>Destroy every GameObject in the source collection safety(check null), without detaching the parent.</summary>
    public static void Destroy(this IEnumerable<GameObject> source, bool useDestroyImmediate) => Destroy(source, useDestroyImmediate, false);

    /// <summary>Destroy every GameObject in the source collection safety(check null), using Destroy (not DestroyImmediate).</summary>
    public static void Destroy(this IEnumerable<GameObject> source) => Destroy(source, false, false);

    #endregion
}
}

