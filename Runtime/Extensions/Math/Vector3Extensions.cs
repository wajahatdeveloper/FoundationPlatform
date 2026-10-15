using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class VectorExtensions
{

    /// <summary>
    /// Sets value to vector's axis.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="axis">Axis index of the vector.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>Changed copy of the vector.</returns>
    public static Vector3 With(this in Vector3 vector, int axis, float value)
    {
        var result = vector;
        result[axis] = value;
        return result;
    }

    /// <summary>
    /// Sets values to vector's axes.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="axis1">First axis index of the vector.</param>
    /// <param name="value1">First value to set.</param>
    /// <param name="axis2">Second axis index of the vector.</param>
    /// <param name="value2">Second value to set.</param>
    /// <returns>Changed copy of the vector.</returns>
    public static Vector3 With(this in Vector3 vector, int axis1, float value1, int axis2, float value2)
    {
        var result = vector;
        result[axis1] = value1;
        result[axis2] = value2;

        return result;
    }

    /// <summary>
    /// Inverts vector.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>Inverted vector.</returns>
    public static Vector3 Negate(this Vector3 vector) => new(-vector.x, -vector.y, -vector.z);

    /// <summary>
    /// Gets <see cref="Vector2"/> by axes.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="axis1">First axis.</param>
    /// <param name="axis2">Second axis.</param>
    /// <returns><see cref="Vector2"/> vector.</returns>
    public static Vector2 Get(this Vector3 vector, int axis1, int axis2) => new(vector[axis1], vector[axis2]);

    /// <summary>
    /// Gets vector with swapped axes.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="axis1">First axis.</param>
    /// <param name="axis2">Second axis.</param>
    /// <param name="axis2">Third axis.</param>
    /// <returns><see cref="Vector3"/> vector.</returns>
    public static Vector3 Get(this Vector3 vector, int axis1, int axis2, int axis3) =>
        new(vector[axis1], vector[axis2], vector[axis3]);

    private static void Compare(Vector3 vector, ref int index, int compareIndex, int result)
    {
        if (vector[compareIndex].CompareTo(vector[index]) == result)
            index = compareIndex;
    }

    /// <summary>
    /// Creates new vector with clamped components.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="min">The minimum floating value to campare agains.</param>
    /// <param name="max">The maximum floating value to campare agains.</param>
    /// <returns>Clamped vector.</returns>
    public static Vector3 Clamp(this in Vector3 vector, float min, float max)
    {
        return new Vector3(Mathf.Clamp(vector.x, min, max), Mathf.Clamp(vector.y, min, max),
            Mathf.Clamp(vector.z, min, max));
    }

    /// <summary>
    /// Creates and returns a vector whose components are limited to 0 and 1.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns>Clamped vector.</returns>
    public static Vector3 Clamp01(this Vector3 vector)
    {
        return new Vector3(Mathf.Clamp01(vector.x), Mathf.Clamp01(vector.y), Mathf.Clamp01(vector.z));
    }

    /// <summary>
    /// Creates and returns a vector whose components are divided by the value.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="other">Vector on which divide</param>
    /// <returns>Divided vector.</returns>
    public static Vector3 Divide(this Vector3 vector, Vector3 other)
    {
        return new Vector3(vector.x / other.x, vector.y / other.y, vector.z / other.z);
    }

    /// <summary>
    /// Get the closest point on a ray.
    /// </summary>
    /// <param name="point">A point in space.</param>
    /// <param name="origin">Start point of ray.</param>
    /// <param name="direction">Ray direction. Must be normalized.</param>
    /// <returns>Tuple which contains closest point on line and distance from <paramref name="origin"/> to calculated point.</returns>
    /// <summary>
    /// Gets closest point info from <paramref name="points"/> list.
    /// </summary>
    /// <param name="point">Origin point.</param>
    /// <param name="points">Compared points.</param>
    /// <returns>Closest point tuple info.</returns>
    public static (Vector3 point, int index) GetClosestPoint(this Vector3 point, params Vector3[] points)
    {
        return GetClosestPoint(point, (IEnumerable<Vector3>)points);
    }

    /// <summary>
    /// Gets closest point info from <paramref name="points"/> list.
    /// </summary>
    /// <param name="point">Origin point.</param>
    /// <param name="points">Compared points.</param>
    /// <returns>Closest point tuple info.</returns>
    public static (Vector3 point, int index) GetClosestPoint(this Vector3 point, IEnumerable<Vector3> points)
    {
        var enumerator = points.GetEnumerator();

        var index = -1;
        var closestIndex = -1;
        var closestPoint = Vector3.zero;
        var closestDistance = float.MaxValue;

        while (enumerator.MoveNext())
        {
            ++index;
            var distance = Vector3.Distance(point, enumerator.Current);

            if (distance < closestDistance)
            {
                closestIndex = index;
                closestDistance = distance;
                closestPoint = enumerator.Current;
            }
        }

        return (closestPoint, closestIndex);
    }


    #region Set X/Y/Z

    // Set X

    public static Vector3 SetX(this Vector3 vector, float x)
    {
        return new Vector3(x, vector.y, vector.z);
    }

    // Set Y

    public static Vector3 SetY(this Vector3 vector, float y)
    {
        return new Vector3(vector.x, y, vector.z);
    }

    #endregion


    #region Offset X/Y/Z

    public static Vector3 Offset(this Vector3 vector, Vector2 offset)
    {
        return new Vector3(vector.x + offset.x, vector.y + offset.y, vector.z);
    }


    public static Vector3 OffsetX(this Vector3 vector, float x)
    {
        return new Vector3(vector.x + x, vector.y, vector.z);
    }


    public static Vector3 OffsetY(this Vector3 vector, float y)
    {
        return new Vector3(vector.x, vector.y + y, vector.z);
    }


    public static Vector3 OffsetZ(this Vector3 vector, float z)
    {
        return new Vector3(vector.x, vector.y, vector.z + z);
    }

#endregion
#region Clamp X/Y

    #endregion


    #region Invert

    #endregion


    #region Convert

    public static Vector2 ToVector2(this Vector3 vector)
    {
        return new Vector2(vector.x, vector.y);
    }

#endregion
#region Snap
#endregion
#region Average

    #endregion


    #region Approximately

    public static bool Approximately(this Vector3 vector, Vector3 compared, float threshold)
    {
        var xDiff = Mathf.Abs(vector.x - compared.x);
        var yDiff = Mathf.Abs(vector.y - compared.y);
        var zDiff = Mathf.Abs(vector.z - compared.z);

        return xDiff <= threshold && yDiff <= threshold && zDiff <= threshold;
    }

    /// <summary>Checks approximate equality using a 0.1 threshold.</summary>
    public static bool Approximately(this Vector3 vector, Vector3 compared) => Approximately(vector, compared, 0.1f);

#endregion
#region Get Closest

    #endregion


    #region To

    /// <summary>
    /// Get vector from source to destination
    /// </summary>
    public static Vector3 To(this Vector3 source, Vector3 destination) =>
        destination - source;

    /// <summary>
    /// Get vector from source to target
    /// </summary>
    public static Vector3 To(this Component source, Component target) =>
        source.transform.position.To(target.transform.position);

    /// <summary>
    /// Get vector from source to target
    /// </summary>
    public static Vector3 To(this Component source, GameObject target) =>
        source.transform.position.To(target.transform.position);

    /// <summary>
    /// Get vector from source to target
    /// </summary>
    public static Vector3 To(this GameObject source, Component target) =>
        source.transform.position.To(target.transform.position);

    /// <summary>
    /// Get vector from source to target
    /// </summary>
    public static Vector3 To(this GameObject source, GameObject target) =>
        source.transform.position.To(target.transform.position);

    /// <summary>
    /// Get vector from source to target
    /// </summary>
    public static Vector3 To(this Vector3 source, GameObject target) =>
        source.To(target.transform.position);

    /// <summary>
    /// Get vector from source to target
    /// </summary>
    public static Vector3 To(this Vector3 source, Component target) =>
        source.To(target.transform.position);

    /// <summary>
    /// Get vector from source to destination
    /// </summary>
    public static Vector3 To(this GameObject source, Vector3 destination) =>
        source.transform.position.To(destination);

    /// <summary>
    /// Get vector from source to destination
    /// </summary>
    public static Vector3 To(this Component source, Vector3 destination) =>
        source.transform.position.To(destination);

    #endregion


    #region Pow

    /// <summary>
    /// Raise each component of the source Vector3 to the specified power.
    /// </summary>
    public static Vector3 Pow(this Vector3 source, float exponent) =>
        new Vector3(Mathf.Pow(source.x, exponent),
            Mathf.Pow(source.y, exponent),
            Mathf.Pow(source.z, exponent));

#endregion
#region ScaleBy

    #endregion

    public static Vector3 down(this Vector3 obj)
    {
        return -Vector3.up;
    }

    public static Vector3 left(this Vector3 obj)
    {
        return -Vector3.right;
    }

    public static Vector3 backward(this Vector3 obj)
    {
        return -Vector3.forward;
    }

    /// <summary>
    /// Absolute value of components
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static Vector3 Abs(this Vector3 v)
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    /// <summary>
    /// Gets the center of two points
    /// </summary>
    /// <param name="vec1"></param>
    /// <param name="vec2"></param>
    /// <returns></returns>
    public static Vector3 Center(this Vector3 vec1, Vector3 vec2)
    {
        return new Vector3((vec1.x + vec2.x) / 2, (vec1.y + vec2.y) / 2, (vec1.z + vec2.z) / 2);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="vec"></param>
    /// <returns></returns>
    public static bool IsNaN(this Vector3 vec)
    {
        return float.IsNaN(vec.x) || float.IsNaN(vec.y) || float.IsNaN(vec.z);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    public static Vector3 Center(this Vector3[] points)
    {
        Vector3 ret = Vector3.zero;
        foreach (var p in points)
        {
            ret += p;
        }

        ret /= points.Length;
        return ret;
    }

    public static void Deconstruct(this Vector3 v3, out float x, out float y, out float z)
    {
        x = v3.x;
        y = v3.y;
        z = v3.z;
    }

    #region Add

    /// <summary>
    /// Adds two Vector3s
    /// </summary>
    /// <param name="v3">source vector3</param>
    /// <param name="value">second vector3</param>
    /// <remarks>
    /// Suggested by: aaro4130
    /// Link: http://forum.unity3d.com/members/aaro4130.22011/
    /// </remarks>
    public static Vector3 Add(this Vector3 v3, Vector3 value)
    {
        return v3 + value;
    }

    /// <summary>
    /// Adds the values to a vector3
    /// </summary>
    /// <param name="v3">source vector3</param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <remarks>
    /// Suggested by: aaro4130
    /// Link: http://forum.unity3d.com/members/aaro4130.22011/
    /// </remarks>
    public static Vector3 Add(this Vector3 v3, float x, float y, float z)
    {
        return v3 + new Vector3(x, y, z);
    }

    // Add

    #endregion

    #region Subtract

    /// <summary>
    /// Subtracts two Vector3s
    /// </summary>
    /// <param name="v3">source vector3</param>
    /// <param name="value">second vector3</param>
    /// <returns></returns>
    /// <remarks>
    /// Suggested by: aaro4130
    /// Link: http://forum.unity3d.com/members/aaro4130.22011/
    /// </remarks>
    public static Vector3 Subtract(this Vector3 v3, Vector3 value)
    {
        return v3 - value;
    }

    /// <summary>
    /// Subtracts the values from a vector 3
    /// </summary>
    /// <param name="v3">source vector3</param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    /// <remarks>
    /// Suggested by: aaro4130
    /// Link: http://forum.unity3d.com/members/aaro4130.22011/
    /// </remarks>
    public static Vector3 Subtract(this Vector3 v3, float x, float y, float z)
    {
        return v3 - new Vector3(x, y, z);
    }

#endregion

    public static Vector3 RotateAround(this Vector3 point, Vector3 pivot, Quaternion rotation) =>
        rotation * (point - pivot) + pivot;

    public static Vector3 Transform(this Vector3 vector, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        vector = Vector3.Scale(vector, new Vector3(scale.x, scale.y, scale.z));
        vector = vector.RotateAround(Vector3.zero, rotation);
        vector += position;
        return vector;
    }

    [Flags]
    public enum VectorAxesMask
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4,
        XY = X | Y,
        XZ = X | Z,
        YZ = Y | Z,
        XYZ = X | Y | Z
    }

    public static Vector3 SetValues(this Vector3 vector, Vector3 values, VectorAxesMask vectorAxesMask)
    {
        if ((vectorAxesMask & VectorAxesMask.X) != VectorAxesMask.None)
        {
            vector.x = values.x;
        }

        if ((vectorAxesMask & VectorAxesMask.Y) != VectorAxesMask.None)
        {
            vector.y = values.y;
        }

        if ((vectorAxesMask & VectorAxesMask.Z) != VectorAxesMask.None)
        {
            vector.z = values.z;
        }

        return vector;
    }

    public static Vector3 SetValues(this Vector3 vector, float value, VectorAxesMask vectorAxesMask)
    {
        return vector.SetValues(new Vector3(value, value, value), vectorAxesMask);
    }

    public static Vector3 ClampMagnitude(this Vector3 vector, float min, float max)
    {
        var result = vector;
        var sqrMagnitude = vector.sqrMagnitude;
        var num = min * min;
        var num2 = max * max;
        if (sqrMagnitude < num)
        {
            result = vector.normalized * min;
        }
        else if (sqrMagnitude > num2)
        {
            result = vector.normalized * max;
        }

        return result;
    }

#region ToV3String
#endregion
#region RotateAroundY
#endregion
#region StringToBytes
#endregion
#region BytesToString
#endregion

    public static Vector3 WithX(this Vector3 vec, float x)
    {
        return new Vector3(x, vec.y, vec.z);
    }

    public static Vector3 WithY(this Vector3 vec, float y)
    {
        return new Vector3(vec.x, y, vec.z);
    }

    public static Vector3 Invert(this Vector3 vec)
    {
        return new Vector3(-vec.x, -vec.y, -vec.z);
    }

#if UNITY_MATHEMATICS
#endif
}}
