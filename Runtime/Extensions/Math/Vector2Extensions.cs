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

    public static Vector2 MakePixelPerfect(this Vector2 position)
    {
        return new Vector2((int)position.x, (int)position.y);
    }

    /// <summary>
    /// Inverts value of specified axis.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <param name="axis">Target axis.</param>
    /// <returns>Vector with inverted axis value.</returns>
    public static Vector2 WithNegate(this Vector2 vector, int axis) => vector.With(axis, -vector[axis]);

    /// <summary>
    /// Inverts x axis value.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>Vector with inverted axis value.</returns>
    public static Vector2 WithNegateX(this Vector2 vector) => vector.WithNegate(0);

    /// <summary>
    /// Inverts y axis value.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>Vector with inverted axis value.</returns>
    public static Vector2 WithNegateY(this Vector2 vector) => vector.WithNegate(1);

    /// <summary>
    /// Inverts vector.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>Inverted vector.</returns>
    public static Vector2 Negate(this Vector2 vector) => new(-vector.x, -vector.y);

    /// <summary>
    /// Gets inverted vector.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns>Inverted vector.</returns>
    public static Vector2 GetYX(this Vector2 vector) => new(vector.y, vector.x);

    /// <summary>
    /// Inserts value to x axis and extends vector to 3-dimensional.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="x">Value to set.</param>
    /// <returns>3-dimensional vector.</returns>
    public static Vector3 InsertX(this Vector2 vector, float x = 0) => new(x, vector.x, vector.y);

    /// <summary>
    /// Inserts value to y axis and extends vector to 3-dimensional.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="y">Value to set.</param>
    /// <returns>3-dimensional vector.</returns>
    public static Vector3 InsertY(this Vector2 vector, float y = 0) => new(vector.x, y, vector.y);

    /// <summary>
    /// Inserts value to z axis and extends vector to 3-dimensional.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="z">Value to set.</param>
    /// <returns>3-dimensional vector.</returns>
    public static Vector3 InsertZ(this Vector2 vector, float z = 0) => new(vector.x, vector.y, z);

    /// <summary>
    /// Gets max component index from vector.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns>Vector's max component index.</returns>
    public static int MaxComponentIndex(this Vector2 vector) => vector.x >= vector.y ? 0 : 1;

    /// <summary>
    /// Gets max component from vector.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns>Vector's max component</returns>
    public static float MaxComponent(this Vector2 vector) => vector[vector.MaxComponentIndex()];

    /// <summary>
    /// Gets min component index from vector.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns>Vector's min component index.</returns>
    public static int MinComponentIndex(this Vector2 vector) => vector.x <= vector.y ? 0 : 1;

    /// <summary>
    /// Gets min component from vector.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns>Vector's min component</returns>
    public static float MinComponent(this Vector2 vector) => vector[vector.MinComponentIndex()];

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
    /// Checks if the vector components are equals.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns><see langword="true"/> if vector's components are equals.</returns>
    public static bool IsUniform(this Vector2 vector) => vector.x.Approximately(vector.y);

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

    /// <summary>
    /// Get the closest point on a ray.
    /// </summary>
    /// <param name="point">A point in space.</param>
    /// <param name="origin">Start point of ray.</param>
    /// <param name="direction">Ray direction. Must be normalized.</param>
    /// <returns>Tuple which contains closest point on line and distance from <paramref name="origin"/> to calculated point.</returns>
    public static (Vector2 point, float distance) GetClosestPointOnRay(this Vector2 point, Vector2 origin,
        Vector2 direction)
    {
        var distance = Vector2.Dot(point - origin, direction);
        return (origin + direction * distance, distance);
    }

    /// <summary>
    /// Get the closest point on a line segment.
    /// </summary>
    /// <param name="point">A point in space.</param>
    /// <param name="start">Start of line segment.</param>
    /// <param name="end">End of line segment.</param>
    /// <returns>Tuple which contains closest point on line and distance from <paramref name="start"/> to calculated point.</returns>
    public static (Vector2 point, float distance) GetClosestPointOnSegment(this Vector2 point, Vector2 start,
        Vector2 end)
    {
        var direction = end - start;
        var lineMagnitude = direction.magnitude;
        direction.Normalize();

        var distance = Mathf.Clamp(Vector2.Dot(point - start, direction), 0f, lineMagnitude);
        return (start + direction * distance, distance);
    }

    /// <summary>
    /// Finds the closest <see cref="Vector2"/> in <paramref name="allTargets"/> on XY plane.
    /// </summary>
    public static Vector2 FindClosest2D(this Vector2 origin, IList<Vector2> allTargets)
    {
        if (allTargets == null)
        {
            throw new ArgumentNullException("allTargets");
        }

        switch (allTargets.Count)
        {
            case 0: return Vector2.zero;
            case 1: return allTargets[0];
        }

        float closestDistance = Mathf.Infinity;
        var closest = Vector2.zero;

        foreach (var iteratingTarget in allTargets)
        {
            float distanceSqr = (iteratingTarget - origin).sqrMagnitude;

            if (distanceSqr < closestDistance)
            {
                closestDistance = distanceSqr;
                closest = iteratingTarget;
            }
        }

        return closest;
    }

    /// <summary>
    /// Finds the closest <see cref="UnityEngine.Transform"/> in <paramref name="allTargets"/> on XY plane.
    /// </summary>
    public static Transform FindClosest2D(this Vector2 origin, IList<Transform> allTargets)
    {
        if (allTargets == null)
        {
            throw new ArgumentNullException("allTargets");
        }

        switch (allTargets.Count)
        {
            case 0: return null;
            case 1: return allTargets[0];
        }

        float closestDistance = Mathf.Infinity;
        Transform closest = null;

        foreach (var iteratingTarget in allTargets)
        {
            float distanceSqr = (iteratingTarget.Position2D() - origin).sqrMagnitude;

            if (distanceSqr < closestDistance)
            {
                closestDistance = distanceSqr;
                closest = iteratingTarget;
            }
        }

        return closest;
    }

    /// <summary>
    /// Finds the closest <see cref="GameObject"/> in <paramref name="allTargets"/> on XY plane.
    /// </summary>
    public static GameObject FindClosest2D(this Vector2 origin, IList<GameObject> allTargets)
    {
        if (allTargets == null)
        {
            throw new ArgumentNullException("allTargets");
        }

        switch (allTargets.Count)
        {
            case 0: return null;
            case 1: return allTargets[0];
        }

        float closestDistance = Mathf.Infinity;
        GameObject closest = null;

        foreach (var iteratingTarget in allTargets)
        {
            float distanceSqr = (iteratingTarget.transform.Position2D() - origin).sqrMagnitude;

            if (distanceSqr < closestDistance)
            {
                closestDistance = distanceSqr;
                closest = iteratingTarget;
            }
        }

        return closest;
    }

    /// <summary>
    /// <para>Returns the 2D center of all the points given.</para>
    /// <para>If <paramref name="weighted"/> is true, center point will be closer to the area that points are denser; if false, center will be the geometric exact center of bounding box of points.</para>
    /// </summary>
    public static Vector2 FindCenter2D(this IList<Vector2> points, bool weighted)
    {
        switch (points.Count)
        {
            case 0: return Vector2.zero;
            case 1: return points[0];
        }

        if (weighted)
        {
            return points.Aggregate(Vector2.zero, (current, point) => current + point) / points.Count;
        }

        var bound = new Bounds { center = points[0] };
        foreach (var point in points)
        {
            bound.Encapsulate(point);
        }

        return bound.center;
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

    public static Vector2 OffsetXY(this Vector2 vector, float x, float y)
    {
        return new Vector2(vector.x + x, vector.y + y);
    }

    public static float Angle(this Vector2 direction)
    {
        return direction.y > 0
            ? Vector2.Angle(new Vector2(1, 0), direction)
            : -Vector2.Angle(new Vector2(1, 0), direction);
    }

    public static Vector2 ClampX(this Vector2 vector, float min, float max)
    {
        return vector.SetX(Mathf.Clamp(vector.x, min, max));
    }

    public static Vector2 ClampY(this Vector2 vector, float min, float max)
    {
        return vector.SetY(Mathf.Clamp(vector.y, min, max));
    }

    public static Vector2 InvertX(this Vector2 vector)
    {
        return new Vector2(-vector.x, vector.y);
    }

    public static Vector2 InvertY(this Vector2 vector)
    {
        return new Vector2(vector.x, -vector.y);
    }

    public static Vector3 ToVector3(this Vector2 vector)
    {
        return new Vector3(vector.x, vector.y);
    }

    public static Vector2Int ToVector2Int(this Vector2 vector)
    {
        return new Vector2Int(Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y));
    }

    /// <summary>
    /// Snap to grid of snapValue
    /// </summary>
    public static Vector2 SnapValue(this Vector2 val, float snapValue)
    {
        return new Vector2(
            MathX.Snap(val.x, snapValue),
            MathX.Snap(val.y, snapValue));
    }

    /// <summary>
    /// Snap to one unit grid
    /// </summary>
    public static Vector2 SnapToOne(this Vector2 vector)
    {
        return new Vector2(Mathf.Round(vector.x), Mathf.Round(vector.y));
    }

    public static Vector2 AverageVector(this Vector2[] vectors)
    {
        if (vectors.IsNullOrEmpty()) return Vector2.zero;

        float x = 0f, y = 0f;
        for (var i = 0; i < vectors.Length; i++)
        {
            x += vectors[i].x;
            y += vectors[i].y;
        }

        return new Vector2(x / vectors.Length, y / vectors.Length);
    }

    public static bool Approximately(this Vector2 vector, Vector2 compared, float threshold = 0.1f)
    {
        var xDiff = Mathf.Abs(vector.x - compared.x);
        var yDiff = Mathf.Abs(vector.y - compared.y);

        return xDiff <= threshold && yDiff <= threshold;
    }

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

    /// <summary>
    /// Immutably returns the result of the source vector multiplied with
    /// another vector component-wise.
    /// </summary>
    public static Vector2 ScaleBy(this Vector2 source, Vector2 right) =>
        Vector2.Scale(source, right);

    public static Vector2 Rotate(this Vector2 vector, float angle, Vector2 pivot = default(Vector2))
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

    public static Vector3 ToVector3(this Vector2 v, float z)
    {
        return new Vector3(v.x, v.y, z);
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

    public static Vector2 MoveToward(this Vector2 v, Vector2 target, ref float speed, float maxSpeed, float accel,
        float deccel, float deltaTime, out bool finished)
    {
        var vector = target - v;
        var num = 1;
        var b = maxSpeed;
        var num2 = speed / deccel;
        var num3 = speed * num2 / 2f;
        if (num3 * num3 > vector.sqrMagnitude)
        {
            num = -1;
        }
        else
        {
            var magnitude = vector.magnitude;
            var num4 = Mathf.Sqrt(magnitude * 2f / deccel);
            b = num4 * deccel;
        }

        speed = Mathf.Clamp(speed + ((num != 1) ? (-deccel * deltaTime) : (accel * deltaTime)), 0f,
            Mathf.Min(maxSpeed, b));
        var b2 = vector.normalized * (speed * deltaTime);
        if (b2.sqrMagnitude >= vector.sqrMagnitude)
        {
            speed = 0f;
            v = target;
            finished = true;
            return v;
        }

        v += b2;
        finished = false;
        return v;
    }

    public static Vector2 MoveToward(this Vector2 v, Vector2 target, ref Vector2 speed, float maxSpeed, float accel,
        float deltatime, out bool finished)
    {
        var a = target - v;
        a.Normalize();
        speed += a * accel * deltatime;
        if (speed.sqrMagnitude > maxSpeed * maxSpeed)
        {
            speed = speed.normalized * maxSpeed;
        }

        var vector = v + speed * deltatime;
        Vector3 v2 = speed;
        var f = Vector2.Dot(v2, (target - v).normalized);
        var f2 = Vector2.Dot(v2, (target - vector).normalized);
        finished = (Mathf.Sign(f) != Mathf.Sign(f2));
        v = vector;
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

    public static Vector2 AddX(this Vector2 vec, float x)
    {
        return new Vector2(vec.x + x, vec.y);
    }

    public static Vector2 AddY(this Vector2 vec, float y)
    {
        return new Vector2(vec.x, vec.y + y);
    }

    public static Vector2 Invert(this Vector2 vec)
    {
        return new Vector2(-vec.x, -vec.y);
    }

    public static Vector2 Abs(this Vector2 vec)
    {
        return new Vector2(Mathf.Abs(vec.x), Mathf.Abs(vec.y));
    }

    public static Vector3Int ToVector3Int(this Vector2 v)
    {
        return new Vector3Int((int)v.x, (int)v.y, 0);
    }
}}
