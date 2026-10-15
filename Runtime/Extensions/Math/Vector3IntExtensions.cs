using System.Collections.Generic;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class Vector3IntExtensions
{
    /// <summary>
    /// Sets value to vector's axis.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="axis">Axis index of the vector.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>Changed copy of the vector.</returns>
    public static Vector3Int With(this Vector3Int vector, int axis, int value)
    {
        vector[axis] = value;
        return vector;
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
    public static Vector3Int With(this Vector3Int vector, int axis1, int value1, int axis2, int value2)
    {
        vector[axis1] = value1;
        vector[axis2] = value2;

        return vector;
    }

    /// <summary>
    /// Inverts vector.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>Inverted vector.</returns>
    public static Vector3Int Negate(this Vector3Int vector) => new(-vector.x, -vector.y, -vector.z);

    /// <summary>
    /// Gets <see cref="Vector2"/> by axes.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="axis1">First axis.</param>
    /// <param name="axis2">Second axis.</param>
    /// <returns><see cref="Vector2"/> vector.</returns>
    public static Vector2Int Get(this Vector3Int vector, int axis1, int axis2) => new(vector[axis1], vector[axis2]);

    /// <summary>
    /// Gets vector with swapped axes.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="axis1">First axis.</param>
    /// <param name="axis2">Second axis.</param>
    /// <param name="axis3">Third axis.</param>
    /// <returns><see cref="Vector2"/> vector.</returns>
    public static Vector3Int Get(this Vector3Int vector, int axis1, int axis2, int axis3) =>
        new(vector[axis1], vector[axis2], vector[axis3]);

    private static void Compare(Vector3Int vector, ref int index, int compareIndex, int result)
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
    public static Vector3Int Clamp(this Vector3Int vector, int min, int max)
    {
        return new Vector3Int(Mathf.Clamp(vector.x, min, max), Mathf.Clamp(vector.y, min, max),
            Mathf.Clamp(vector.z, min, max));
    }

    /// <summary>
    /// Creates and returns a vector whose components are divided by the value.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="other">Vector on which divide</param>
    /// <returns>Divided vector.</returns>
    public static Vector3Int Divide(this Vector3Int vector, Vector3Int other)
    {
        return new Vector3Int(vector.x / other.x, vector.y / other.y, vector.z / other.z);
    }

    /// <summary>
    /// Gets closest point info from <paramref name="points"/> list.
    /// </summary>
    /// <param name="point">Origin point.</param>
    /// <param name="points">Compared points.</param>
    /// <returns>Closest point tuple info.</returns>
    public static (Vector3Int point, int index) GetClosestPoint(this Vector3Int point, params Vector3Int[] points)
    {
        return GetClosestPoint(point, (IEnumerable<Vector3Int>)points);
    }

    /// <summary>
    /// Gets closest point info from <paramref name="points"/> list.
    /// </summary>
    /// <param name="point">Origin point.</param>
    /// <param name="points">Compared points.</param>
    /// <returns>Closest point tuple info.</returns>
    public static (Vector3Int point, int index) GetClosestPoint(this Vector3Int point, IEnumerable<Vector3Int> points)
    {
        var enumerator = points.GetEnumerator();

        var index = -1;
        var closestIndex = -1;
        var closestPoint = Vector3Int.zero;
        var closestDistance = float.MaxValue;

        while (enumerator.MoveNext())
        {
            ++index;
            var distance = Vector3Int.Distance(point, enumerator.Current);

            if (distance < closestDistance)
            {
                closestIndex = index;
                closestDistance = distance;
                closestPoint = enumerator.Current;
            }
        }

        return (closestPoint, closestIndex);
    }

    public static Vector3Int WithX(this Vector3Int vec, int x)
    {
        return new Vector3Int(x, vec.y, vec.z);
    }

    public static Vector3Int WithY(this Vector3Int vec, int y)
    {
        return new Vector3Int(vec.x, y, vec.z);
    }

    public static Vector3Int Invert(this Vector3Int vec)
    {
        return new Vector3Int(-vec.x, -vec.y, -vec.z);
    }

    public static Vector3Int Abs(this Vector3Int vec)
    {
        return new Vector3Int(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
    }
}}
