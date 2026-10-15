using System.Collections.Generic;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class Vector2IntExtensions
{
    /// <summary>
    /// Sets value to vector's axis.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="axis">Axis index of the vector.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>Changed copy of the vector.</returns>
    public static Vector2Int With(this Vector2Int vector, int axis, int value)
    {
        vector[axis] = value;
        return vector;
    }

    /// <summary>
    /// Inverts vector.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>Inverted vector.</returns>
    public static Vector2Int Negate(this Vector2Int vector) => new(-vector.x, -vector.y);

    /// <summary>
    /// Creates new vector with clamped components.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="min">The minimum floating value to campare agains.</param>
    /// <param name="max">The maximum floating value to campare agains.</param>
    /// <returns>Clamped vector.</returns>
    public static Vector2Int Clamp(this Vector2Int vector, int min, int max)
    {
        return new Vector2Int(Mathf.Clamp(vector.x, min, max), Mathf.Clamp(vector.y, min, max));
    }

    /// <summary>
    /// Create new vector with divided by value components.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="other">Vector on which divide</param>
    /// <returns>Divided vector.</returns>
    public static Vector2Int Divide(this Vector2Int vector, Vector2Int other)
    {
        return new Vector2Int(vector.x / other.x, vector.y / other.y);
    }

    /// <summary>
    /// Gets closest point info from <paramref name="points"/> list.
    /// </summary>
    /// <param name="point">Origin point.</param>
    /// <param name="points">Compared points.</param>
    /// <returns>Closest point tuple info.</returns>
    public static (Vector2Int point, int index) GetClosestPoint(this Vector2Int point, params Vector2Int[] points)
    {
        return GetClosestPoint(point, (IEnumerable<Vector2Int>)points);
    }

    /// <summary>
    /// Gets closest point info from <paramref name="points"/> list.
    /// </summary>
    /// <param name="point">Origin point.</param>
    /// <param name="points">Compared points.</param>
    /// <returns>Closest point tuple info.</returns>
    public static (Vector2Int point, int index) GetClosestPoint(this Vector2Int point, IEnumerable<Vector2Int> points)
    {
        var enumerator = points.GetEnumerator();

        var index = -1;
        var closestIndex = -1;
        var closestPoint = Vector2Int.zero;
        var closestDistance = float.MaxValue;

        while (enumerator.MoveNext())
        {
            ++index;
            var distance = Vector2Int.Distance(point, enumerator.Current);

            if (distance < closestDistance)
            {
                closestIndex = index;
                closestDistance = distance;
                closestPoint = enumerator.Current;
            }
        }

        return (closestPoint, closestIndex);
    }

    public static Vector2 ToVector2(this Vector2Int vector)
    {
        return new Vector2(vector.x, vector.y);
    }

    public static Vector2Int WithX(this Vector2Int vec, int x)
    {
        return new Vector2Int(x, vec.y);
    }

    public static Vector2Int WithY(this Vector2Int vec, int y)
    {
        return new Vector2Int(vec.x, y);
    }

    public static Vector2Int Invert(this Vector2Int vec)
    {
        return new Vector2Int(-vec.x, -vec.y);
    }

    public static Vector2Int Abs(this Vector2Int vec)
    {
        return new Vector2Int(Mathf.Abs(vec.x), Mathf.Abs(vec.y));
    }

    public static Vector2 ToVector2(this Vector3Int v)
    {
        return new Vector2(v.x, v.y);
    }
}}
