using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class Vector2Extensions
{
    /// <summary>
    /// Sets value to vector's axis.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="axis">Axis index of the vector.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>Changed copy of the vector.</returns>
    public static Vector2 With(this Vector2 vector, int axis, float value)
    {
        vector[axis] = value;
        return vector;
    }

    /// <summary>
    /// Inverts vector.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>Inverted vector.</returns>
    public static Vector2 Negate(this Vector2 vector) => new(-vector.x, -vector.y);

    /// <summary>
    /// Creates new vector with clamped components.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="min">The minimum floating value to campare agains.</param>
    /// <param name="max">The maximum floating value to campare agains.</param>
    /// <returns>Clamped vector.</returns>
    public static Vector2 Clamp(this Vector2 vector, float min, float max)
    {
        return new Vector2(Mathf.Clamp(vector.x, min, max), Mathf.Clamp(vector.y, min, max));
    }

    /// <summary>
    /// Creates and returns a vector whose components are limited to 0 and 1.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns>Clamped vector.</returns>
    public static Vector2 Clamp01(this Vector2 vector)
    {
        return new Vector2(Mathf.Clamp01(vector.x), Mathf.Clamp01(vector.y));
    }

    /// <summary>
    /// Creates and returns a vector whose components are divided by the value.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="other">Vector on which divide</param>
    /// <returns>Divided vector.</returns>
    public static Vector2 Divide(this Vector2 vector, Vector2 other) => vector / other;

    /// <summary>
    /// Gets closest point info from <paramref name="points"/> list.
    /// </summary>
    /// <param name="point">Origin point.</param>
    /// <param name="points">Compared points.</param>
    /// <returns>Closest point tuple info.</returns>
    public static (Vector2 point, int index) GetClosestPoint(this Vector2 point, params Vector2[] points)
    {
        return GetClosestPoint(point, (IEnumerable<Vector2>)points);
    }

    /// <summary>
    /// Gets closest point info from <paramref name="points"/> list.
    /// </summary>
    /// <param name="point">Origin point.</param>
    /// <param name="points">Compared points.</param>
    /// <returns>Closest point tuple info.</returns>
    public static (Vector2 point, int index) GetClosestPoint(this Vector2 point, IEnumerable<Vector2> points)
    {
        var index = -1;
        var closestIndex = -1;
        var closestPoint = Vector2.zero;
        var closestDistance = float.MaxValue;

        foreach (var current in points)
        {
            ++index;
            var distance = Vector2.Distance(point, current);

            if (distance < closestDistance)
            {
                closestIndex = index;
                closestDistance = distance;
                closestPoint = current;
            }
        }

        return (closestPoint, closestIndex);
    }

    public static Vector2 SetX(this Vector2 vector, float x)
    {
        return new Vector2(x, vector.y);
    }

    public static Vector2 SetY(this Vector2 vector, float y)
    {
        return new Vector2(vector.x, y);
    }

    public static Vector2 OffsetX(this Vector2 vector, float x)
    {
        return new Vector2(vector.x + x, vector.y);
    }

    public static Vector2 OffsetY(this Vector2 vector, float y)
    {
        return new Vector2(vector.x, vector.y + y);
    }

    public static float Angle(this Vector2 direction)
    {
        return direction.y > 0
            ? Vector2.Angle(new Vector2(1, 0), direction)
            : -Vector2.Angle(new Vector2(1, 0), direction);
    }

    public static bool Approximately(this Vector2 vector, Vector2 compared, float threshold)
    {
        var xDiff = Mathf.Abs(vector.x - compared.x);
        var yDiff = Mathf.Abs(vector.y - compared.y);

        return xDiff <= threshold && yDiff <= threshold;
    }

    /// <summary>Checks approximate equality using a 0.1 threshold.</summary>
    public static bool Approximately(this Vector2 vector, Vector2 compared) => Approximately(vector, compared, 0.1f);

    /// <summary>
    /// Get vector from source to destination
    /// </summary>
    public static Vector2 To(this Vector2 source, Vector2 destination) =>
        destination - source;

    /// <summary>
    /// Raise each component of the source Vector2 to the specified power.
    /// </summary>
    public static Vector2 Pow(this Vector2 source, float exponent) =>
        new Vector2(Mathf.Pow(source.x, exponent),
            Mathf.Pow(source.y, exponent));

    public static Vector2 Rotate(this Vector2 vector, float angle, Vector2 pivot)
    {
        Vector2 rotated = Quaternion.Euler(new Vector3(0f, 0f, angle)) * (vector - pivot);
        return rotated + pivot;
    }

    public static void Deconstruct(this Vector2 v2, out float x, out float y)
    {
        x = v2.x;
        y = v2.y;
    }

    public static Vector2 SetValues(this Vector2 vector, Vector2 values, VectorExtensions.VectorAxesMask vectorAxesMask)
    {
        if ((vectorAxesMask & VectorExtensions.VectorAxesMask.X) != VectorExtensions.VectorAxesMask.None)
        {
            vector.x = values.x;
        }

        if ((vectorAxesMask & VectorExtensions.VectorAxesMask.Y) != VectorExtensions.VectorAxesMask.None)
        {
            vector.y = values.y;
        }

        return vector;
    }

    public static Vector2 SetValues(this Vector2 vector, float value, VectorExtensions.VectorAxesMask vectorAxesMask)
    {
        return vector.SetValues(new Vector2(value, value), vectorAxesMask);
    }

    public static Vector2 ClampMagnitude(this Vector2 vector, float min, float max)
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

    public static Vector2 Clamp(this Vector2 v, Vector2 min, Vector2 max)
    {
        return new Vector2(Mathf.Clamp(v.x, min.x, max.x), Mathf.Clamp(v.y, min.y, max.y));
    }

    public static Vector2 Clamp(this Vector2 v, Rect rect)
    {
        return new Vector2(Mathf.Clamp(v.x, rect.xMin, rect.xMax), Mathf.Clamp(v.y, rect.yMin, rect.yMax));
    }

    public static Vector2 Clamp(this Vector2 v, float xMin, float yMin, float xMax, float yMax)
    {
        return new Vector2(Mathf.Clamp(v.x, xMin, xMax), Mathf.Clamp(v.y, yMin, yMax));
    }

    public static Vector2 Rotate(this Vector2 v, float radian)
    {
        var num = Mathf.Sin(radian);
        var num2 = Mathf.Cos(radian);
        var x = v.x;
        var y = v.y;
        v.x = num2 * x - num * y;
        v.y = num * x + num2 * y;
        return v;
    }

    public static Vector2 WithX(this Vector2 vec, float x)
    {
        return new Vector2(x, vec.y);
    }

    public static Vector2 WithY(this Vector2 vec, float y)
    {
        return new Vector2(vec.x, y);
    }

    public static Vector2 Invert(this Vector2 vec)
    {
        return new Vector2(-vec.x, -vec.y);
    }

    public static Vector2 Abs(this Vector2 vec)
    {
        return new Vector2(Mathf.Abs(vec.x), Mathf.Abs(vec.y));
    }
}}
