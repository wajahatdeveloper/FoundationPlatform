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
    /// <summary>
    /// Returns the Vector3 distance between these two GameObjects
    /// </summary>
    /// <param name="go"></param>
    /// <param name="otherGO"></param>
    /// <returns></returns>
    public static float DistanceTo(this GameObject go, GameObject otherGO)
    {
        return Vector3.Distance(go.transform.position, otherGO.transform.position);
    }

    /// <summary>
    /// Returns the Vecto3 distance between these two points
    /// </summary>
    /// <param name="go"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static float DistanceTo(this GameObject go, Vector3 pos)
    {
        return Vector3.Distance(go.transform.position, pos);
    }

    #region IsNullOrInactive

    /// <summary>
    /// Returns true if the GO is null or inactive
    /// </summary>
    /// <param name="go"></param>
    /// <returns></returns>
    public static bool IsNullOrInactive(this GameObject go)
    {
        return ((go == null) || (!go.activeSelf));
    }

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

    /// <summary>
    /// Activates this gameobject, starting with its parent
    /// </summary>
    /// <param name="go"></param>
    public static void ActivateAndParent(this GameObject go)
    {
        // exit if go is null
        if (go == null)
            return;

        // if this object has a parent, activate each parent first
        if (go.transform.parent != null)
            go.transform.parent.gameObject.ActivateAndParent();

        // activate this object
        go.SetActive(true);
    }

    #endregion

    #region HasRigidbody

    /// <summary>
    /// Returns true if the object has a rigid body
    /// </summary>
    /// <param name="go"></param>
    /// <returns></returns>
    public static bool HasRigidbody(this GameObject go)
    {
        return (go.GetComponent<Rigidbody>() != null);
    }

    #endregion

    #region HasCharacterController

    /// <summary>
    /// Returns true if the object has a character controller
    /// </summary>
    /// <param name="go"></param>
    /// <returns></returns>
    public static bool HasCharacterController(this GameObject go)
    {
        return (go.GetComponent<CharacterController>() != null);
    }

    #endregion

    #region HasAnimation

    /// <summary>
    /// Returns true if the object has an animation
    /// </summary>
    /// <param name="go"></param>
    /// <returns></returns>
    public static bool HasAnimation(this GameObject go)
    {
        return (go.GetComponent<UnityEngine.Animation>() != null);
    }

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

    /// <summary>
    /// Enables or disables colliders on the game object and all its children
    /// </summary>
    /// <param name="go"></param>
    /// <param name="enabled"></param>
    public static void SetCollisionRecursively(this GameObject go, bool enabled)
    {
        Collider GCollide = go.GetComponent<Collider>();
        if (GCollide != null)
            GCollide.enabled = enabled;

        foreach (Transform t in go.transform)
            t.gameObject.SetCollisionRecursively(enabled);
    }

    #endregion

    #region GetCollisionMask

    /// <summary>
    /// Returns the collision mask of the game object (all layers which this object can collide with)
    /// </summary>
    /// <param name="go"></param>
    /// <param name="layer">OPTIONAL. If omitted, it uses the layer of the calling GameObject, which is the most common/intuitive case (for me, at least). But you can specify a layer and it'll hand you the collision mask for that layer instead.</param>
    /// <returns></returns>
    public static int GetCollisionMask(this GameObject go, int layer = -1)
    {
        // get the layer if one was not sent
        if (layer == -1)
            layer = go.layer;

        // get the mask on this layer
        int mask = 0;
        for (int i = 0; i < 32; i++)
            mask |= (Physics.GetIgnoreLayerCollision(layer, i) ? 0 : 1) << i;

        return mask;
    }

    #endregion

    /// <summary>
    /// Sets the active state of this <paramref name="gameObject"/> and it's first level parent.
    /// </summary>
    public static void SetActiveWithParent(this GameObject gameObject, bool value)
    {
        gameObject.SetActive(value);
        var parent = gameObject.transform.parent;
        if (parent != null)
            parent.gameObject.SetActive(value);
    }

    /// <summary>
    /// Sets the active state of this <paramref name="gameObject"/> and it's first level children.
    /// </summary>
    public static void SetActiveWithChildren(this GameObject gameObject, bool value)
    {
        gameObject.SetActive(value);
        foreach (Transform child in gameObject.transform)
        {
            child.gameObject.SetActive(value);
        }
    }

    /// <summary>
    /// Sets the active state of this <paramref name="gameObject"/> and all of ancestors (parent, grandparent, parent of grandparent... etc).
    /// </summary>
    /// <remarks>
    /// This method loops through the hierarchy, thus eliminating recursive calls.
    /// </remarks>
    public static void SetActiveWithAncestors(this GameObject gameObject, bool value)
    {
        var t = gameObject.transform;
        while (t != null)
        {
            t.gameObject.SetActive(value);
            t = t.parent;
        }
    }

    /// <summary>
    /// Sets the active state of this <paramref name="gameObject"/> and all of it's children hierarchy (children, grandchildren, children of grandchildren... etc).
    /// </summary>
    /// <remarks>
    /// This method keeps a list of all children hierarchy as it loops through, thus eliminating recursive calls.
    /// </remarks>
    public static void SetActiveWithDescendants(this GameObject gameObject, bool value)
    {
        Transform firstLevel = SetActiveTSCH(gameObject.transform, value);
        if (firstLevel.childCount == 0) return;

        var queue = new List<Transform> { firstLevel };
        while (queue.Count > 0)
        {
            for (int i = queue.Count - 1; i >= 0; i--)
            {
                Transform t = SetActiveTSCH(queue[i], value);
                queue.RemoveAt(i);

                if (t.childCount > 0) queue.AddRange(t.Cast<Transform>());
            }
        }
    }

    /// <summary>
    /// SetActive Through Single Child Hierarchy <para/>
    /// 'SetActive's transform; if transform has only one child, switches to child; repeats the process. <para/>
    /// Continues until switching to a transform with no child or more than one child. <para/>
    /// Returns the transform it stopped.
    /// </summary>
    private static Transform SetActiveTSCH(Transform beginWith, bool value)
    {
        Transform t = beginWith;
        t.gameObject.SetActive(value);

        while (t.childCount == 1)
        {
            t = t.GetChild(0);
            t.gameObject.SetActive(value);
        }

        return t;
    }

    public static void SetTransformX(this GameObject obj, float n)
    {
        obj.transform.position = new Vector3(n, obj.transform.position.y, obj.transform.position.z);
    }

    public static void SetTransformY(this GameObject obj, float n)
    {
        obj.transform.position = new Vector3(obj.transform.position.x, n, obj.transform.position.z);
    }

    public static void SetTransformZ(this GameObject obj, float n)
    {
        obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y, n);
    }

    //recursive calls
    private static void InternalMoveToLayer(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            InternalMoveToLayer(child, layer);
    }

    /// <summary>
    /// Move root and all children to the specified layer
    /// </summary>
    /// <param name="root"></param>
    /// <param name="layer"></param>
    public static void MoveToLayer(this GameObject root, int layer)
    {
        InternalMoveToLayer(root.transform, layer);
    }

    /// <summary>
    /// is the object's layer in the specified layermask
    /// </summary>
    /// <param name="gameObject"></param>
    /// <param name="mask"></param>
    /// <returns></returns>
    public static bool IsInLayerMask(this GameObject gameObject, LayerMask mask)
    {
        return ((mask.value & (1 << gameObject.layer)) > 0);
    }

    public static void ReverseActiveState(this GameObject go)
    {
        go.SetActive(!go.activeSelf);
    }

    /// <summary>Destroy this GameObject safety(check null).</summary>
    /// <param name="useDestroyImmediate">If in EditMode, should be true or pass !Application.isPlaying.</param>
    /// <param name="detachParent">set to parent = null.</param>
    public static void Destroy(this GameObject self, bool useDestroyImmediate = false, bool detachParent = false)
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

    public static void DestroySelf(this GameObject @this)
    {
        GameObject.Destroy(@this);
    }

    public static void DestroySelf(this GameObject @this, float t)
    {
        GameObject.Destroy(@this, t);
    }

    /// <summary>
    /// Finde the closest gameobject of the current one based on it's tag
    /// </summary>
    /// <typeparam name="T">Type of object to find</typeparam>
    /// <param name="obj">Object wich is searching</param>
    /// <param name="tag">Tag of the searched object</param>
    /// <param name="maxDistance">Max distance that will be searched</param>
    /// <returns>Returns a single GameObject or null</returns>
    public static T FindNearestByTag<T>(this GameObject obj, string tag, float maxDistance = float.PositiveInfinity)
        where T : MonoBehaviour
    {
        var objects = GameObject.FindGameObjectsWithTag(tag);

        if (objects != null)
        {
            List<T> selectedObjects = new List<T>();
            foreach (var item in objects)
            {
                if (item.TryGetComponent<T>(out var component))
                    selectedObjects.Add(component);
            }

            return FindNearests<T>(obj, selectedObjects, maxDistance);
        }

        return null;
    }

    /// <summary>
    /// Finde the closest gameobject of the current one based on it's Type
    /// </summary>
    /// <typeparam name="T">Type of object to find</typeparam>
    /// <param name="obj">Object wich is searching</param>
    /// <param name="maxDistance">Max distance that will be searched</param>
    /// <returns>Returns a single GameObject or null</returns>
    public static T FindNearestByType<T>(this GameObject obj, float maxDistance = float.PositiveInfinity)
        where T : MonoBehaviour
    {
        var objects = GameObject.FindObjectsByType<T>(FindObjectsSortMode.None);
        if (objects != null)
        {
            List<T> selectedObjects = new List<T>();
            foreach (var item in objects)
            {
                if (item.TryGetComponent<T>(out var component))
                    selectedObjects.Add(component);
            }

            return FindNearests<T>(obj, selectedObjects, maxDistance);
        }

        return null;
    }

    /// <summary>
    /// Searchs on a list of GameObjects wich one it's closest to <see cref="obj"/>
    /// </summary>
    /// <typeparam name="T">Type of object to find</typeparam>
    /// <param name="obj">Object wich is searching</param>
    /// <param name="objects">Object list to be filtered</param>
    /// <param name="maxDistance">Max distance that will be searched</param>
    /// <returns>Returns the closests GameObject of obj or null</returns>
    public static T FindNearests<T>(this GameObject obj, List<T> objects, float maxDistance = float.PositiveInfinity)
        where T : MonoBehaviour
    {
        if (objects.Count == 0)
            return null;

        T nearestObject = null;
        foreach (T item in objects)
        {
            var dist = Vector3.Distance(obj.transform.position, item.transform.position);

            if (dist <= maxDistance)
            {
                if (nearestObject == null)
                {
                    nearestObject = item;
                }
                else
                {
                    var dist2 = Vector3.Distance(obj.transform.position, nearestObject.transform.position);
                    nearestObject = dist < dist2 ? item : nearestObject;
                }
            }
        }

        return nearestObject;
    }

    /// <summary>
    /// Sets the lossy scale of the source Transform.
    /// </summary>
    public static Transform SetLossyScale(this Transform source,
        Vector3 targetLossyScale)
    {
        source.localScale = source.lossyScale.Pow(-1).ScaleBy(targetLossyScale)
            .ScaleBy(source.localScale);
        return source;
    }
}
}

