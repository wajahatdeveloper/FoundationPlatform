using System.Collections.Generic;
using System;
using System.Linq;
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;
using Object = UnityEngine.Object;

/* *****************************************************************************
 * File:    UnityTransformExtensions.cs
 * Author:  Philip Pierce - Monday, September 29, 2014
 * Description:
 *  Extensions for transforms and vector3
 *  
 * History:
 *  Monday, September 29, 2014 - Created
 * ****************************************************************************/

/// <summary>
/// Extensions for transforms and vector3
/// </summary>
public static class UnityTransformExtensions
{
    private static void LogNullExtensionArg(string name)
    {
        DebugX.Logger(LogChannels.Validation).Error("[Foundation:ERROR] UnityTransformExtensions: {ParamName} is null.", name);
    }

    public static void SetLayerRecursive(this GameObject o, int layer)
    {
        SetLayerInternal(o.transform, layer);
    }

    private static void SetLayerInternal(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        foreach (Transform o in t) SetLayerInternal(o, layer);
    }

    public static void SetXZ(this Transform transform, float x, float z)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.position = transform.position.SetXZ(x, z);
    }

    public static void SetYZ(this Transform transform, float y, float z)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.position = transform.position.SetYZ(y, z);
    }

    /// <summary>
    /// Set position to Vector3.zero.
    /// </summary>
    public static void ResetPosition(this Transform transform)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.position = Vector3.zero;
    }

    public static void OffsetX(this Transform transform, float x)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.position = transform.position.OffsetX(x);
    }

    public static void OffsetY(this Transform transform, float y)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.position = transform.position.OffsetY(y);
    }

    public static void OffsetZ(this Transform transform, float z)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.position = transform.position.OffsetZ(z);
    }

    public static void OffsetXY(this Transform transform, float x, float y)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.position = transform.position.OffsetXY(x, y);
    }

    public static void OffsetXZ(this Transform transform, float x, float z)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.position = transform.position.OffsetXZ(x, z);
    }

    public static void OffsetYZ(this Transform transform, float y, float z)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.position = transform.position.OffsetYZ(y, z);
    }

    /// <summary>
    /// Snap position to grid of snapValue
    /// </summary>
    public static void SnapPosition(this Transform transform, float snapValue)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.position = transform.position.SnapValue(snapValue);
    }

    
    #region Position...

    public static void SetXYZ(this Transform transform, float x, float y, float z)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.position = new Vector3(x, y, z);
    }

    public static void SetLocalXYZ(this Transform transform, float x, float y, float z)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.localPosition = new Vector3(x, y, z);
    }

    public static void SetXY(this Transform transform, float x, float y)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.position = new Vector3(x, y, transform.position.z);
    }

    public static void SetLocalXY(this Transform transform, float x, float y)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.localPosition = new Vector3(x, y, transform.localPosition.z);
    }

    public static void SetX(this Transform transform, float x)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }

    public static void SetLocalX(this Transform transform, float x)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.localPosition = new Vector3(x, transform.localPosition.y, transform.localPosition.z);
    }

    public static void SetY(this Transform transform, float y)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    public static void SetLocalY(this Transform transform, float y)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.localPosition = new Vector3(transform.localPosition.x, y, transform.localPosition.z);
    }

    public static void SetZ(this Transform transform, float z)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.position = new Vector3(transform.position.x, transform.position.y, z);
    }

    public static void SetLocalZ(this Transform transform, float z)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, z);
    }

    public static Vector2 Position2(this Transform transform)
    {
        return transform.position;
    }

    public static void IncLocalX(this Transform transform, float dx)
    {
        transform.localPosition = new Vector3(transform.localPosition.x + dx, transform.localPosition.y,
            transform.localPosition.z);
    }

    public static void IncLocalY(this Transform transform, float dy)
    {
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y + dy,
            transform.localPosition.z);
    }

    public static void IncLocalZ(this Transform transform, float dz)
    {
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y,
            transform.localPosition.z + dz);
    }

    #endregion

    public static void SetScaleX(this Transform transform, float scaleX)
    {
        transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
    }

    public static void SetScaleY(this Transform transform, float scaleY)
    {
        transform.localScale = new Vector3(transform.localScale.x, scaleY, transform.localScale.z);
    }

    public static void SetScaleZ(this Transform transform, float scaleZ)
    {
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, scaleZ);
    }

    public static void SetRotation(this Transform transform, float angle)
    {
        transform.rotation = new Quaternion();
        transform.Rotate(Vector3.forward, angle);
    }

    #region GetPositionX

    /// <summary>
    /// Returns X of position
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static float GetPositionX(this Transform t)
    {
        return t.position.x;
    }

    #endregion

    #region GetPositionY

    /// <summary>
    /// Returns Y of position
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static float GetPositionY(this Transform t)
    {
        return t.position.y;
    }

    #endregion

    #region GetPositionZ

    /// <summary>
    /// Returns Z of position
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static float GetPositionZ(this Transform t)
    {
        return t.position.z;
    }

    #endregion

    /// <summary>
    /// Returns the Vecto3 distance between these two transforms
    /// </summary>
    /// <param name="start"></param>
    /// <param name="dest"></param>
    /// <remarks>
    /// Suggested by: Vipsu
    /// Link: http://forum.unity3d.com/members/vipsu.138664/
    /// </remarks>
    public static float DistanceTo(this Transform start, Transform dest)
    {
        if (start == null)
        {
            LogNullExtensionArg(nameof(start));
            throw new ArgumentNullException(nameof(start));
        }
        if (dest == null)
        {
            LogNullExtensionArg(nameof(dest));
            throw new ArgumentNullException(nameof(dest));
        }
        return Vector3.Distance(start.position, dest.position);
    }

    /// <summary>
    /// Returns the 2D position of given transform on XY plane. Uses manual conversion.
    /// </summary>
    public static Vector2 Position2D(this Transform transform)
    {
        var p = transform.position;
        return new Vector2(p.x, p.y);
    }

    /// <summary>
    /// Returns the 2D local position of given transform on XY plane. Uses manual conversion.
    /// </summary>
    public static Vector2 LocalPosition2D(this Transform transform)
    {
        var lp = transform.localPosition;
        return new Vector2(lp.x, lp.y);
    }

    /// <summary>
    /// Moves the transform to a Vector2 on XY coordinates.
    /// </summary>
    public static void SetPosition2D(this Transform transform, Vector2 newPosition)
    {
        transform.position = new Vector3(newPosition.x, newPosition.y, 0);
    }

    /// <summary>
    /// Moves the transform to a Vector2 on local XY coordinates.
    /// </summary>
    public static void SetLocalPosition2D(this Transform transform, Vector2 newLocalPosition)
    {
        transform.localPosition = new Vector3(newLocalPosition.x, newLocalPosition.y, 0);
    }

    /// <summary>
    /// Deep search the heirarchy of the specified transform for the name. Uses width-first search.
    /// </summary>
    /// <param name="t"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Transform DeepSearch(this Transform t, string name)
    {
        if (t == null)
        {
            LogNullExtensionArg(nameof(t));
            throw new ArgumentNullException(nameof(t));
        }
        Transform dt = t.Find(name);
        if (dt != null)
        {
            return dt;
        }

        foreach (Transform child in t)
        {
            dt = child.DeepSearch(name);
            if (dt != null)
                return dt;
        }

        return null;
    }

    public static void DestroyAllChildren(this Transform t)
    {
        if (t == null)
        {
            LogNullExtensionArg(nameof(t));
            throw new ArgumentNullException(nameof(t));
        }
        foreach (Transform child in t)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// opposite of up
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static Vector3 down(this Transform t)
    {
        return -t.up;
    }

    /// <summary>
    /// opposite of right
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static Vector3 left(this Transform t)
    {
        return -t.right;
    }

    /// <summary>
    /// opposite of forward
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static Vector3 backward(this Transform t)
    {
        return -t.forward;
    }

    /// <summary>
    /// Rotates the transform so the forward vector points at target's current position.
    /// </summary>
    /// <param name="transform">Transform.</param>
    /// <param name="target">Target.</param>
    public static void LookAt2D(this Transform transform, Transform target)
    {
        transform.LookAt2D((Vector2)target.position);
    }

    /// <summary>
    /// Rotates the transform so the forward vector points at worldPosition.
    /// </summary>
    /// <param name="transform">Transform.</param>
    /// <param name="worldPosition">World position.</param>
    public static void LookAt2D(this Transform transform, Vector3 worldPosition)
    {
        transform.LookAt2D((Vector2)worldPosition);
    }

    /// <summary>
    /// Rotates the transform so the forward vector points at worldPosition.
    /// </summary>
    /// <param name="transform">Transform.</param>
    /// <param name="worldPosition">World position.</param>
    public static void LookAt2D(this Transform transform, Vector2 worldPosition)
    {
        Vector2 distance = worldPosition - (Vector2)transform.position;
        transform.eulerAngles = new Vector3(
            transform.eulerAngles.x,
            transform.eulerAngles.y,
            Mathf.Atan2(distance.y, distance.x) * Mathf.Rad2Deg);
    }

    public static void SetPosition(this Transform transform, Vector3 position,
        VectorExtensions.VectorAxesMask vectorAxesMask = VectorExtensions.VectorAxesMask.XYZ)
    {
        transform.position = transform.position.SetValues(position, vectorAxesMask);
    }

    public static void SetPosition(this Transform transform, float position,
        VectorExtensions.VectorAxesMask vectorAxesMask = VectorExtensions.VectorAxesMask.XYZ)
    {
        transform.SetPosition(new Vector3(position, position, position), vectorAxesMask);
    }

    public static void SetLocalPosition(this Transform transform, Vector3 position,
        VectorExtensions.VectorAxesMask vectorAxesMask = VectorExtensions.VectorAxesMask.XYZ)
    {
        transform.localPosition = transform.localPosition.SetValues(position, vectorAxesMask);
    }

    public static void SetLocalPosition(this Transform transform, float position,
        VectorExtensions.VectorAxesMask vectorAxesMask = VectorExtensions.VectorAxesMask.XYZ)
    {
        transform.SetLocalPosition(new Vector3(position, position, position), vectorAxesMask);
    }

    public static void SetEulerAngles(this Transform transform, Vector3 angles,
        VectorExtensions.VectorAxesMask vectorAxesMask = VectorExtensions.VectorAxesMask.XYZ)
    {
        transform.eulerAngles = transform.eulerAngles.SetValues(angles, vectorAxesMask);
    }

    public static void SetEulerAngles(this Transform transform, float angle,
        VectorExtensions.VectorAxesMask vectorAxesMask = VectorExtensions.VectorAxesMask.XYZ)
    {
        transform.SetEulerAngles(new Vector3(angle, angle, angle), vectorAxesMask);
    }

    public static void SetLocalEulerAngles(this Transform transform, Vector3 angles,
        VectorExtensions.VectorAxesMask vectorAxesMask = VectorExtensions.VectorAxesMask.XYZ)
    {
        transform.localEulerAngles = transform.localEulerAngles.SetValues(angles, vectorAxesMask);
    }

    public static void SetLocalEulerAngles(this Transform transform, float angle,
        VectorExtensions.VectorAxesMask vectorAxesMask = VectorExtensions.VectorAxesMask.XYZ)
    {
        transform.SetLocalEulerAngles(new Vector3(angle, angle, angle), vectorAxesMask);
    }

    public static void SetLocalScale(this Transform transform, Vector3 scale,
        VectorExtensions.VectorAxesMask vectorAxesMask = VectorExtensions.VectorAxesMask.XYZ)
    {
        transform.localScale = transform.localScale.SetValues(scale, vectorAxesMask);
    }

    public static void SetLocalScale(this Transform transform, float scale,
        VectorExtensions.VectorAxesMask vectorAxesMask = VectorExtensions.VectorAxesMask.XYZ)
    {
        transform.SetLocalScale(new Vector3(scale, scale, scale), vectorAxesMask);
    }

    public static Transform[] GetChildren(this Transform parent)
    {
        if (parent == null)
        {
            LogNullExtensionArg(nameof(parent));
            throw new ArgumentNullException(nameof(parent));
        }
        var array = new Transform[parent.childCount];
        for (var i = 0; i < parent.childCount; i++)
        {
            array[i] = parent.GetChild(i);
        }

        return array;
    }

    public static Transform[] GetChildrenRecursive(this Transform parent)
    {
        if (parent == null)
        {
            LogNullExtensionArg(nameof(parent));
            throw new ArgumentNullException(nameof(parent));
        }
        var list = new List<Transform>();
        var children = parent.GetChildren();
        for (var i = 0; i < children.Length; i++)
        {
            var transform = children[i];
            list.Add(transform);
            if (transform.childCount > 0)
            {
                list.AddRange(transform.GetChildrenRecursive());
            }
        }

        return list.ToArray();
    }

    public static Transform FindChild(this Transform parent, Predicate<Transform> predicate)
    {
        if (parent == null)
        {
            LogNullExtensionArg(nameof(parent));
            throw new ArgumentNullException(nameof(parent));
        }
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (predicate(child))
            {
                return child;
            }
        }

        return null;
    }

    public static Transform FindChildRecursive(this Transform parent, string childName)
    {
        return parent.FindChildRecursive(child => child.name == childName);
    }

    public static Transform FindChildRecursive(this Transform parent, Predicate<Transform> predicate)
    {
        var childrenRecursive = parent.GetChildrenRecursive();
        for (var i = 0; i < childrenRecursive.Length; i++)
        {
            var transform = childrenRecursive[i];
            if (predicate(transform))
            {
                return transform;
            }
        }

        return null;
    }

    public static Transform[] FindChildren(this Transform parent, string childName)
    {
        return parent.FindChildren(child => child.name == childName);
    }

    public static Transform[] FindChildren(this Transform parent, Predicate<Transform> predicate)
    {
        var list = new List<Transform>();
        var children = parent.GetChildren();
        for (var i = 0; i < children.Length; i++)
        {
            var transform = children[i];
            if (predicate(transform))
            {
                list.Add(transform);
            }
        }

        return list.ToArray();
    }

    public static Transform[] FindChildrenRecursive(this Transform parent, string childName)
    {
        return parent.FindChildrenRecursive(child => child.name == childName);
    }

    public static Transform[] FindChildrenRecursive(this Transform parent, Predicate<Transform> predicate)
    {
        var list = new List<Transform>();
        var childrenRecursive = parent.GetChildrenRecursive();
        for (var i = 0; i < childrenRecursive.Length; i++)
        {
            var transform = childrenRecursive[i];
            if (predicate(transform))
            {
                list.Add(transform);
            }
        }

        return list.ToArray();
    }

    /// <summary>
    /// Sets x position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="x">Value to set.</param>
    public static void SetPositionX(this Transform transform, float x) =>
        transform.position = transform.position.WithX(x);

    /// <summary>
    /// Sets y position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="y">Value to set.</param>
    public static void SetPositionY(this Transform transform, float y) =>
        transform.position = transform.position.WithY(y);

    /// <summary>
    /// Sets z position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="z">Value to set.</param>
    public static void SetPositionZ(this Transform transform, float z) =>
        transform.position = transform.position.WithZ(z);

    /// <summary>
    /// Sets x and y position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="y">Value to set.</param>
    public static void SetPositionXY(this Transform transform, float x, float y) =>
        transform.position = transform.position.WithXY(x, y);

    /// <summary>
    /// Sets x and y position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="position">Position to set.</param>
    public static void SetPositionXY(this Transform transform, Vector2 position) =>
        transform.position = transform.position.WithXY(position);

    /// <summary>
    /// Sets x and z position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="z">Value to set.</param>
    public static void SetPositionXZ(this Transform transform, float x, float z) =>
        transform.position = transform.position.WithXZ(x, z);

    /// <summary>
    /// Sets x and z position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="position">Position to set.</param>
    public static void SetPositionXZ(this Transform transform, Vector2 position) =>
        transform.position = transform.position.WithXZ(position);

    /// <summary>
    /// Sets y and z position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="y">Value to set.</param>
    /// <param name="z">Value to set.</param>
    public static void SetPositionYZ(this Transform transform, float y, float z) =>
        transform.position = transform.position.WithYZ(y, z);

    /// <summary>
    /// Sets y and z position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="position">Position to set.</param>
    public static void SetPositionYZ(this Transform transform, Vector2 position) =>
        transform.position = transform.position.WithYZ(position);

    /// <summary>
    /// Sets local x position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="x">Value to set.</param>
    public static void SetLocalPositionX(this Transform transform, float x) =>
        transform.localPosition = transform.localPosition.WithX(x);

    /// <summary>
    /// Sets local y position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="y">Value to set.</param>
    public static void SetLocalPositionY(this Transform transform, float y) =>
        transform.localPosition = transform.localPosition.WithY(y);

    /// <summary>
    /// Sets local z position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="z">Value to set.</param>
    public static void SetLocalPositionZ(this Transform transform, float z) =>
        transform.localPosition = transform.localPosition.WithZ(z);

    /// <summary>
    /// Sets local x and y position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="y">Value to set.</param>
    public static void SetLocalPositionXY(this Transform transform, float x, float y) =>
        transform.localPosition = transform.localPosition.WithXY(x, y);

    /// <summary>
    /// Sets local x and y position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="position">Position to set.</param>
    public static void SetLocalPositionXY(this Transform transform, Vector2 position) =>
        transform.localPosition = transform.localPosition.WithXY(position);

    /// <summary>
    /// Sets local x and z position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="z">Value to set.</param>
    public static void SetLocalPositionXZ(this Transform transform, float x, float z) =>
        transform.localPosition = transform.localPosition.WithXZ(x, z);

    /// <summary>
    /// Sets local x and z position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="position">Position to set.</param>
    public static void SetLocalPositionXZ(this Transform transform, Vector2 position) =>
        transform.localPosition = transform.localPosition.WithXZ(position);

    /// <summary>
    /// Sets local y and z position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="y">Value to set.</param>
    /// <param name="z">Value to set.</param>
    public static void SetLocalPositionYZ(this Transform transform, float y, float z) =>
        transform.localPosition = transform.localPosition.WithYZ(y, z);

    /// <summary>
    /// Sets local y and z position of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="position">Position to set.</param>
    public static void SetLocalPositionYZ(this Transform transform, Vector2 position) =>
        transform.localPosition = transform.localPosition.WithYZ(position);

    /// <summary>
    /// Sets euler angles x value of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="x">Value to set.</param>
    public static void SetEulerAnglesX(this Transform transform, float x) =>
        transform.eulerAngles = transform.eulerAngles.WithX(x);

    /// <summary>
    /// Sets euler angles y value of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="y">Value to set.</param>
    public static void SetEulerAnglesY(this Transform transform, float y) =>
        transform.eulerAngles = transform.eulerAngles.WithY(y);

    /// <summary>
    /// Sets euler angles z value of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="z">Value to set.</param>
    public static void SetEulerAnglesZ(this Transform transform, float z) =>
        transform.eulerAngles = transform.eulerAngles.WithZ(z);

    /// <summary>
    /// Sets euler angles x and y values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="y">Value to set.</param>
    public static void SetEulerAnglesXY(this Transform transform, float x, float y) =>
        transform.eulerAngles = transform.eulerAngles.WithXY(x, y);

    /// <summary>
    /// Sets euler angles x and y values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="eulers">Angles to set.</param>
    public static void SetEulerAnglesXY(this Transform transform, Vector2 eulers) =>
        transform.eulerAngles = transform.eulerAngles.WithXY(eulers);

    /// <summary>
    /// Sets euler angles x and z values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="z">Value to set.</param>
    public static void SetEulerAnglesXZ(this Transform transform, float x, float z) =>
        transform.eulerAngles = transform.eulerAngles.WithXZ(x, z);

    /// <summary>
    /// Sets euler angles x and z values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="eulers">Angles to set.</param>
    public static void SetEulerAnglesXZ(this Transform transform, Vector2 eulers) =>
        transform.eulerAngles = transform.eulerAngles.WithXZ(eulers);

    /// <summary>
    /// Sets euler angles y and z values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="y">Value to set.</param>
    /// <param name="z">Value to set.</param>
    public static void SetEulerAnglesYZ(this Transform transform, float y, float z) =>
        transform.eulerAngles = transform.eulerAngles.WithYZ(y, z);

    /// <summary>
    /// Sets euler angles y and z values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="eulers">Angles to set.</param>
    public static void SetEulerAnglesYZ(this Transform transform, Vector2 eulers) =>
        transform.eulerAngles = transform.eulerAngles.WithYZ(eulers);

    /// <summary>
    /// Sets local euler angles x value of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="x">Value to set.</param>
    public static void SetLocalEulerAnglesX(this Transform transform, float x) =>
        transform.localEulerAngles = transform.localEulerAngles.WithX(x);

    /// <summary>
    /// Sets local euler angles y value of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="y">Value to set.</param>
    public static void SetLocalEulerAnglesY(this Transform transform, float y) =>
        transform.localEulerAngles = transform.localEulerAngles.WithY(y);

    /// <summary>
    /// Sets local euler angles z value of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="z">Value to set.</param>
    public static void SetLocalEulerAnglesZ(this Transform transform, float z) =>
        transform.localEulerAngles = transform.localEulerAngles.WithZ(z);

    /// <summary>
    /// Sets local euler angles x and y values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="y">Value to set.</param>
    public static void SetLocalEulerAnglesXY(this Transform transform, float x, float y) =>
        transform.localEulerAngles = transform.localEulerAngles.WithXY(x, y);

    /// <summary>
    /// Sets local euler angles x and y values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="eulers">Angles to set.</param>
    public static void SetLocalEulerAnglesXY(this Transform transform, Vector2 eulers) =>
        transform.localEulerAngles = transform.localEulerAngles.WithXY(eulers);

    /// <summary>
    /// Sets local euler angles x and z values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="z">Value to set.</param>
    public static void SetLocalEulerAnglesXZ(this Transform transform, float x, float z) =>
        transform.localEulerAngles = transform.localEulerAngles.WithXZ(x, z);

    /// <summary>
    /// Sets local euler angles x and z values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="eulers">Angles to set.</param>
    public static void SetLocalEulerAnglesXZ(this Transform transform, Vector2 eulers) =>
        transform.localEulerAngles = transform.localEulerAngles.WithXZ(eulers);

    /// <summary>
    /// Sets local euler angles y and z values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="y">Value to set.</param>
    /// <param name="z">Value to set.</param>
    public static void SetLocalEulerAnglesYZ(this Transform transform, float y, float z) =>
        transform.localEulerAngles = transform.localEulerAngles.WithYZ(y, z);

    /// <summary>
    /// Sets local euler angles y and z values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="eulers">Angles to set.</param>
    public static void SetLocalEulerAnglesYZ(this Transform transform, Vector2 eulers) =>
        transform.localEulerAngles = transform.localEulerAngles.WithYZ(eulers);

    /// <summary>
    /// Sets local scale x value of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="x">Value to set.</param>
    public static void SetLocalScaleX(this Transform transform, float x) =>
        transform.localScale = transform.localScale.WithX(x);

    /// <summary>
    /// Sets local scale y value of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="y">Value to set.</param>
    public static void SetLocalScaleY(this Transform transform, float y) =>
        transform.localScale = transform.localScale.WithY(y);

    /// <summary>
    /// Sets local scale z value of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="z">Value to set.</param>
    public static void SetLocalScaleZ(this Transform transform, float z) =>
        transform.localScale = transform.localScale.WithZ(z);

    /// <summary>
    /// Sets local scale x and y values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="y">Value to set.</param>
    public static void SetLocalScaleXY(this Transform transform, float x, float y) =>
        transform.localScale = transform.localScale.WithXY(x, y);

    /// <summary>
    /// Sets local scale x and y values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="scale">Scale to set.</param>
    public static void SetLocalScaleXY(this Transform transform, Vector2 scale) =>
        transform.localScale = transform.localScale.WithXY(scale);

    /// <summary>
    /// Sets local scale x and z values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="z">Value to set.</param>
    public static void SetLocalScaleXZ(this Transform transform, float x, float z) =>
        transform.localScale = transform.localScale.WithXZ(x, z);

    /// <summary>
    /// Sets local scale x and z values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="scale">Scale to set.</param>
    public static void SetLocalScaleXZ(this Transform transform, Vector2 scale) =>
        transform.localScale = transform.localScale.WithXZ(scale);

    /// <summary>
    /// Sets local scale y and z values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="y">Value to set.</param>
    /// <param name="z">Value to set.</param>
    public static void SetLocalScaleYZ(this Transform transform, float y, float z) =>
        transform.localScale = transform.localScale.WithYZ(y, z);

    /// <summary>
    /// Sets local scale y and z values of transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="scale">Scale to set.</param>
    public static void SetLocalScaleYZ(this Transform transform, Vector2 scale) =>
        transform.localScale = transform.localScale.WithYZ(scale);

    /// <summary>
    /// Sets uniform local scale.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="scale">Uniform scale to set.</param>
    public static void SetLocalScale(this Transform transform, float scale) =>
        transform.localScale = transform.localScale = new Vector3(scale, scale, scale);

    /// <summary>
    /// Resets transform position, rotation and scale.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="useWorldSpace">Use world space?</param>
    public static void Reset(this Transform transform, bool useWorldSpace = false)
    {
        if (useWorldSpace)
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        else
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Sets sibling index to previous.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    public static void SetToPreviousSibling(this Transform transform) =>
        transform.SetSiblingIndex(Mathf.Max(transform.GetSiblingIndex() - 1, 0));

    /// <summary>
    /// Sets sibling index to next.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    public static void SetToNextSibling(this Transform transform)
    {
        var maxSiblingIndex =
            (transform.parent != null ? transform.parent.childCount : transform.gameObject.scene.rootCount) - 1;
        transform.SetSiblingIndex(Mathf.Min(transform.GetSiblingIndex() + 1, maxSiblingIndex));
    }

    /// <summary>
    /// Returns the previous transform next to the current object. If there is no previous one, then the current transform is returned.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <returns>Previous object's transform.</returns>
    public static Transform GetPreviousSiblingTransform(this Transform transform)
    {
        if (transform.GetSiblingIndex() == 0)
            return null;

        if (transform.parent)
            return transform.parent.GetChild(transform.GetSiblingIndex() - 1);
        else
            return transform.gameObject.scene.GetRootGameObjects()[transform.GetSiblingIndex() - 1].transform;
    }

    /// <summary>
    /// Returns the next transform next to the current object. If there is no next one, then the current transform is returned.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <returns>Next object's transform.</returns>
    public static Transform GetNextSiblingTransform(this Transform transform)
    {
        if (transform.parent)
        {
            if (transform.GetSiblingIndex() == transform.parent.childCount - 1)
                return null;

            return transform.parent.GetChild(transform.GetSiblingIndex() + 1);
        }

        if (transform.GetSiblingIndex() == transform.gameObject.scene.rootCount - 1)
            return null;

        return transform.gameObject.scene.GetRootGameObjects()[transform.GetSiblingIndex() + 1].transform;
    }

    /// <summary>
    /// Returns all sibling objects (its transform components).
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="includeThis">Include this <paramref name="transform"/> object.</param>
    /// <returns>Sibling objects transforms.</returns>
    public static List<Transform> GetAllSiblingObjects(this Transform transform, bool includeThis = true)
    {
        List<Transform> siblings;

        if (transform.parent)
        {
            siblings = new List<Transform>(transform.parent.childCount);

            for (int i = 0; i < transform.parent.childCount; i++)
            {
                var child = transform.parent.GetChild(i);

                if (includeThis || !includeThis && child != transform)
                    siblings.Add(child);
            }
        }
        else
        {
            var childs = transform.gameObject.scene.GetRootGameObjects();
            siblings = new List<Transform>(childs.Length);

            for (int i = 0; i < childs.Length; i++)
            {
                var child = childs[i].transform;

                if (includeThis || !includeThis && child != transform)
                    siblings.Add(child);
            }
        }

        return siblings;
    }

    /// <summary>
    /// Gets a random child.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <returns>Random child.</returns>
    public static Transform GetRandomChild(this Transform transform) =>
        transform.GetChild(UnityEngine.Random.Range(0, transform.childCount));

    /// <summary>
    /// Adds childs to transform.
    /// </summary>
    /// <param name="childs">Childs to add.</param>
    /// <param name="transform">Target transform.</param>
    public static void AddChilds(this Transform transform, params Transform[] childs) =>
        AddChilds(transform, (IEnumerable<Transform>)childs);

    /// <summary>
    /// Adds childs to transform.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="childs">Childs to add.</param>
    public static void AddChilds(this Transform transform, IEnumerable<Transform> childs)
    {
        foreach (var child in childs)
            child.parent = transform;
    }

    /// <summary>
    /// Destroy all childs.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    public static void DestroyChilds(this Transform transform) =>
        transform.GetChildren().ToList().ForEach(child => Object.Destroy(child.gameObject));

    /// <summary>
    /// Destroy all childs by coindition.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="predicate">Condition.</param>
    public static void DestroyChildsWhere(this Transform transform, Predicate<Transform> predicate)
    {
        var filtered = transform.GetChildren().Where(c => predicate(c));

        foreach (var child in filtered)
            Object.Destroy(child.gameObject);
    }

    /// <summary>
    /// Returns the first child transform. Will return null if the transform has no children.
    /// </summary>
    /// <param name="transform">The transform to get the first child of.</param>
    /// <returns>The first child.</returns>
    public static Transform FirstChild(this Transform transform)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }

        if (transform.childCount == 0)
            return null;

        return transform.GetChild(0);
    }

    /// <summary>
    /// Returns the last child transform. Will return null if the transform has no children.
    /// </summary>
    /// <param name="transform">The transform to get the last child of.</param>
    /// <returns>The last child.</returns>
    public static Transform LastChild(this Transform transform)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }

        if (transform.childCount == 0)
            return null;

        return transform.GetChild(transform.childCount - 1);
    }

    /// <summary>
    /// Destroy child by index.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    public static void DestroyChild(this Transform transform, int index) =>
        Object.Destroy(transform.GetChild(index).gameObject);

    /// <summary>
    /// Destroy first child.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    public static void DestroyFirstChild(this Transform transform) => DestroyChild(transform, 0);

    /// <summary>
    /// Destroy last child.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    public static void DestroyLastChild(this Transform transform) => DestroyChild(transform, transform.childCount - 1);

    /// <summary>
    /// Sets <see cref="Transform.lossyScale"/> value.
    /// </summary>
    /// <param name="transform">Transform component.</param>
    /// <param name="lossyScale">New lossyScale value.</param>
    public static void SetLossyScale(this Transform transform, Vector3 lossyScale)
    {
        transform.localScale = Vector3.one;
        var currentLossyScale = transform.lossyScale;
        transform.localScale = new Vector3(lossyScale.x / currentLossyScale.x, lossyScale.y / currentLossyScale.y,
            lossyScale.z / currentLossyScale.z);
    }
}