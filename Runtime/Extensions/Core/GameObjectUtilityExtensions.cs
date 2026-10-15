using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Utility extensions for GameObjects
/// </summary>
namespace AetherNexus.FoundationPlatform.Extensions
{
public static class GameObjectUtilityExtensions
{

#region IsNullOrInactive

    #endregion

    #region IsActive

    /// <summary>
    /// Returns true if the GO is not null and is active
    /// </summary>
    /// <param name="go"></param>
    /// <returns></returns>
    public static bool IsActive(this GameObject go)
    {
        return ((go != null) && (go.activeSelf));
    }

#endregion
#region ActivateAndParent
#endregion
#region HasRigidbody
#endregion
#region HasCharacterController
#endregion
#region HasAnimation

    #endregion

    #region SetLayerRecursively

    /// <summary>
    /// Sets the layer for the game object and all its children
    /// </summary>
    /// <param name="go"></param>
    /// <param name="layer"></param>
    public static void SetLayerRecursively(this GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            t.gameObject.SetLayerRecursively(layer);
    }

#endregion
#region SetCollisionRecursively
#endregion
#region GetCollisionMask
#endregion

    /// <summary>Destroy this GameObject safety(check null).</summary>
    /// <param name="useDestroyImmediate">If in EditMode, should be true or pass !Application.isPlaying.</param>
    /// <param name="detachParent">set to parent = null.</param>
    public static void Destroy(this GameObject self, bool useDestroyImmediate, bool detachParent)
    {
        if (self == null) return;

        if (detachParent)
        {
            self.transform.SetParent(null);
        }

        if (useDestroyImmediate)
        {
            GameObject.DestroyImmediate(self);
        }
        else
        {
            GameObject.Destroy(self);
        }
    }

    /// <summary>Destroy this GameObject safety(check null), without detaching its parent.</summary>
    public static void Destroy(this GameObject self, bool useDestroyImmediate) => Destroy(self, useDestroyImmediate, false);

    /// <summary>Destroy this GameObject safety(check null), using Destroy (not DestroyImmediate) and without detaching its parent.</summary>
    public static void Destroy(this GameObject self) => Destroy(self, false, false);
}
}

