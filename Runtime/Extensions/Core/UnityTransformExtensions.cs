using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AetherNexus.FoundationPlatform.Extensions
{

using AetherNexus.FoundationPlatform.Logging;
    
/// <summary>
/// Extensions for transforms and vector3
/// </summary>
public static class UnityTransformExtensions
{
    private static void LogNullExtensionArg(string name)
    {
        DebugX.Logger(LogChannels.Validation).Error("[Foundation:ERROR] UnityTransformExtensions: {ParamName} is null.", name);
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

#region Position...

    public static void SetX(this Transform transform, float x)
    {
        if (transform == null)
        {
            LogNullExtensionArg(nameof(transform));
            throw new ArgumentNullException(nameof(transform));
        }
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
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

#endregion

    public static void SetRotation(this Transform transform, float angle)
    {
        transform.rotation = new Quaternion();
        transform.Rotate(Vector3.forward, angle);
    }

#region GetPositionX
#endregion
#region GetPositionY
#endregion
#region GetPositionZ
#endregion

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

    public static void SetPosition(this Transform transform, Vector3 position,
        VectorExtensions.VectorAxesMask vectorAxesMask)
    {
        transform.position = transform.position.SetValues(position, vectorAxesMask);
    }

    /// <summary>Sets position on all axes.</summary>
    public static void SetPosition(this Transform transform, Vector3 position) => SetPosition(transform, position, VectorExtensions.VectorAxesMask.XYZ);

    public static void SetPosition(this Transform transform, float position,
        VectorExtensions.VectorAxesMask vectorAxesMask)
    {
        transform.SetPosition(new Vector3(position, position, position), vectorAxesMask);
    }

    /// <summary>Sets position on all axes.</summary>
    public static void SetPosition(this Transform transform, float position) => SetPosition(transform, position, VectorExtensions.VectorAxesMask.XYZ);

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

    /// <summary>
    /// Resets transform position, rotation and scale.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="useWorldSpace">Use world space?</param>
    public static void Reset(this Transform transform, bool useWorldSpace)
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

    /// <summary>Resets transform position, rotation and scale in local space.</summary>
    public static void Reset(this Transform transform) => Reset(transform, false);

    // Note: SetLossyScale(this Transform, Vector3) lives in GameObjectUtilityExtensions.cs — kept there
    // as the single source (single-write implementation) to avoid an ambiguous-call (CS0121) between two
    // identical-signature overloads in this same namespace.
}}
