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
    /// Smoothes a Vector3 that represents euler angles.
    /// </summary>
    /// <param name="current">The current Vector3 value.</param>
    /// <param name="target">The target Vector3 value.</param>
    /// <param name="velocity">A refernce Vector3 used internally.</param>
    /// <param name="smoothTime">The time to smooth, in seconds.</param>
    /// <returns>The smoothed Vector3 value.</returns>
    public static Vector3 SmoothDampEuler(this in Vector3 current, Vector3 target, ref Vector3 velocity,
        float smoothTime)
    {
        Vector3 v;

        v.x = Mathf.SmoothDampAngle(current.x, target.x, ref velocity.x, smoothTime);
        v.y = Mathf.SmoothDampAngle(current.y, target.y, ref velocity.y, smoothTime);
        v.z = Mathf.SmoothDampAngle(current.z, target.z, ref velocity.z, smoothTime);

        return v;
    }

    /// <summary>
    /// Calculate evently distributed point position on circle.
    /// </summary>
    /// <param name="index">Index of point.</param>
    /// <param name="radius">Radius of the circle.</param>
    /// <param name="count">Total count of points in circle.</param>
    /// <returns>Calculated evently distributed point.</returns>
    public static Vector2 EventlyDistributedPointOnCircle(int index, float radius, int count)
    {
        var k = index + 0.5f;
        var r = Mathf.Sqrt((k) / count);
        var theta = Mathf.PI * (1f + Mathf.Sqrt(5f)) * k;

        var x = r * Mathf.Cos(theta) * radius;
        var y = r * Mathf.Sin(theta) * radius;

        return new Vector2(x, y);
    }

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
    /// Sets values to vector's x and y axis.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="y">Value to set.</param>
    /// <returns>Changed copy of the vector.</returns>
    public static Vector3 WithXY(this Vector3 vector, float x, float y) => With(vector, 0, x, 1, y);

    /// <summary>
    /// Sets value to vector's x and y axis.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>Changed copy of the vector.</returns>
    public static Vector3 WithXY(this Vector3 vector, Vector2 value) => With(vector, 0, value.x, 1, value.y);

    /// <summary>
    /// Sets value to vector's x and z axis.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="x">Value to set.</param>
    /// <param name="z">Value to set.</param>
    /// <returns>Changed copy of the vector.</returns>
    public static Vector3 WithXZ(this Vector3 vector, float x, float z) => With(vector, 0, x, 2, z);

    /// <summary>
    /// Sets value to vector's x and z axis.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>Changed copy of the vector.</returns>
    public static Vector3 WithXZ(this Vector3 vector, Vector2 value) => With(vector, 0, value.x, 2, value.y);

    /// <summary>
    /// Sets value to vector's y and z axis.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="y">Value to set.</param>
    /// <param name="z">Value to set.</param>
    /// <returns>Changed copy of the vector.</returns>
    public static Vector3 WithYZ(this Vector3 vector, float y, float z) => With(vector, 1, y, 2, z);

    /// <summary>
    /// Sets value to vector's y and z axis.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>Changed copy of the vector.</returns>
    public static Vector3 WithYZ(this Vector3 vector, Vector2 value) => With(vector, 1, value.x, 2, value.y);

    /// <summary>
    /// Inverts value of specified axis.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <param name="axis">Target axis.</param>
    /// <returns>Vector with inverted axis value.</returns>
    public static Vector3 WithNegate(this Vector3 vector, int axis) => vector.With(axis, -vector[axis]);

    /// <summary>
    /// Inverts x axis value.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>Vector with inverted axis value.</returns>
    public static Vector3 WithNegateX(this Vector3 vector) => WithNegate(vector, 0);

    /// <summary>
    /// Inverts y axis value.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>Vector with inverted axis value.</returns>
    public static Vector3 WithNegateY(this Vector3 vector) => WithNegate(vector, 1);

    /// <summary>
    /// Inverts z axis value.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>Vector with inverted axis value.</returns>
    public static Vector3 WithNegateZ(this Vector3 vector) => WithNegate(vector, 2);

    /// <summary>
    /// Inverts values of specified axes.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <param name="axis1">First axis.</param>
    /// <param name="axis2">Second axis.</param>
    /// <returns>Vector with inverted axes values.</returns>
    public static Vector3 WithNegate(this Vector3 vector, int axis1, int axis2)
    {
        vector[axis1] = -vector[axis1];
        vector[axis2] = -vector[axis2];

        return vector;
    }

    /// <summary>
    /// Inverts x and y axes values.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>Vector with inverted axes values.</returns>
    public static Vector3 WithNegateXY(this Vector3 vector) => vector.WithNegate(0, 1);

    /// <summary>
    /// Inverts x and z axes values.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>Vector with inverted axes values.</returns>
    public static Vector3 WithNegateXZ(this Vector3 vector) => vector.WithNegate(0, 2);

    /// <summary>
    /// Inverts y and z axes values.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>Vector with inverted axes values.</returns>
    public static Vector3 WithNegateYZ(this Vector3 vector) => vector.WithNegate(1, 2);

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
    /// Gets <see cref="Vector2"/> by x and y axes.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns><see cref="Vector2"/> vector.</returns>
    public static Vector2 GetXY(this Vector3 vector) => Get(vector, 0, 1);

    /// <summary>
    /// Gets <see cref="Vector2"/> by x and z axes.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns><see cref="Vector2"/> vector.</returns>
    public static Vector2 GetXZ(this Vector3 vector) => Get(vector, 0, 2);

    /// <summary>
    /// Gets <see cref="Vector2"/> by y and x axes.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns><see cref="Vector2"/> vector.</returns>
    public static Vector2 GetYX(this Vector3 vector) => Get(vector, 1, 0);

    /// <summary>
    /// Gets <see cref="Vector2"/> by y and z axes.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns><see cref="Vector2"/> vector.</returns>
    public static Vector2 GetYZ(this Vector3 vector) => Get(vector, 1, 2);

    /// <summary>
    /// Gets <see cref="Vector2"/> by z and x axes.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns><see cref="Vector2"/> vector.</returns>
    public static Vector2 GetZX(this Vector3 vector) => Get(vector, 2, 0);

    /// <summary>
    /// Gets <see cref="Vector2"/> by z and y axes.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns><see cref="Vector2"/> vector.</returns>
    public static Vector2 GetZY(this Vector3 vector) => Get(vector, 2, 1);

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

    /// <summary>
    /// Gets vector with order XZY.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns><see cref="Vector3"/> vector.</returns>
    public static Vector3 GetXZY(this Vector3 vector) => Get(vector, 0, 2, 1);

    /// <summary>
    /// Gets vector with order YXZ.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns><see cref="Vector3"/> vector.</returns>
    public static Vector3 GetYXZ(this Vector3 vector) => Get(vector, 1, 0, 2);

    /// <summary>
    /// Gets vector with order YZX.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns><see cref="Vector3"/> vector.</returns>
    public static Vector3 GetYZX(this Vector3 vector) => Get(vector, 1, 2, 0);

    /// <summary>
    /// Gets vector with order ZXY.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns><see cref="Vector3"/> vector.</returns>
    public static Vector3 GetZXY(this Vector3 vector) => Get(vector, 2, 0, 1);

    /// <summary>
    /// Gets vector with order ZYX.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns><see cref="Vector3"/> vector.</returns>
    public static Vector3 GetZYX(this Vector3 vector) => Get(vector, 2, 1, 0);

    /// <summary>
    /// Inserts value to x axis and extends vector to 4-dimensional.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="x">Target vector.</param>
    /// <returns><see cref="Vector3"/> vector.</returns>
    public static Vector4 InsertX(this Vector3 vector, float x) => new(x, vector.x, vector.y, vector.z);

    /// <summary>Inserts a 0 value to x axis and extends vector to 4-dimensional.</summary>
    public static Vector4 InsertX(this Vector3 vector) => InsertX(vector, 0);

    /// <summary>
    /// Inserts value to y axis and extends vector to 4-dimensional.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="y">Target vector.</param>
    /// <returns><see cref="Vector4"/> vector.</returns>
    public static Vector4 InsertY(this Vector3 vector, float y) => new(vector.x, y, vector.y, vector.z);

    /// <summary>Inserts a 0 value to y axis and extends vector to 4-dimensional.</summary>
    public static Vector4 InsertY(this Vector3 vector) => InsertY(vector, 0);

    /// <summary>
    /// Inserts value to z axis and extends vector to 4-dimensional.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="z">Target vector.</param>
    /// <returns><see cref="Vector4"/> vector.</returns>
    public static Vector4 InsertZ(this Vector3 vector, float z) => new(vector.x, vector.y, z, vector.z);

    /// <summary>Inserts a 0 value to z axis and extends vector to 4-dimensional.</summary>
    public static Vector4 InsertZ(this Vector3 vector) => InsertZ(vector, 0);

    /// <summary>
    /// Inserts value to w axis and extends vector to 4-dimensional.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <param name="w">Target vector.</param>
    /// <returns><see cref="Vector4"/> vector.</returns>
    public static Vector4 InsertW(this Vector3 vector, float w) => new(vector.x, vector.y, vector.z, w);

    /// <summary>Inserts a 0 value to w axis and extends vector to 4-dimensional.</summary>
    public static Vector4 InsertW(this Vector3 vector) => InsertW(vector, 0);

    private static void Compare(Vector3 vector, ref int index, int compareIndex, int result)
    {
        if (vector[compareIndex].CompareTo(vector[index]) == result)
            index = compareIndex;
    }

    private static int CompareAllComponents(Vector3 vector, int result)
    {
        var index = 0;

        Compare(vector, ref index, 1, result);
        Compare(vector, ref index, 2, result);

        return index;
    }

    /// <summary>
    /// Gets max component index from vector.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns>Vector's max component index.</returns>
    public static int MaxComponentIndex(this Vector3 vector) => CompareAllComponents(vector, 1);

    /// <summary>
    /// Gets min component from vector.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns>Vector's min component</returns>
    public static float MaxComponent(this Vector3 vector) => vector[MaxComponentIndex(vector)];

    /// <summary>
    /// Gets min component index from vector.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns>Vector's min component index.</returns>
    public static int MinComponentIndex(this Vector3 vector) => CompareAllComponents(vector, -1);

    /// <summary>
    /// Gets min component from vector.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns>Vector's min component</returns>
    public static float MinComponent(this Vector3 vector) => vector[MinComponentIndex(vector)];

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
    /// Calculate average vector from a Span of vectors (high performance).
    /// </summary>
    /// <param name="vectors">Span of vectors to average.</param>
    /// <returns>Average vector.</returns>
    public static Vector3 AverageVector(this System.ReadOnlySpan<Vector3> vectors)
    {
        if (vectors.IsEmpty) return Vector3.zero;
        
        float x = 0f, y = 0f, z = 0f;
        for (int i = 0; i < vectors.Length; i++)
        {
            x += vectors[i].x;
            y += vectors[i].y;
            z += vectors[i].z;
        }
        
        return new Vector3(x / vectors.Length, y / vectors.Length, z / vectors.Length);
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
    /// Checks if the vector components are equals.
    /// </summary>
    /// <param name="vector">Target vector.</param>
    /// <returns><see langword="true"/> if vector's components are equals.</returns>
    public static bool IsUniform(this Vector3 vector) =>
        vector.x.Approximately(vector.y) && vector.y.Approximately(vector.z);

    /// <summary>
    /// Calculate evently distributed point position on sphere using Fibonacci spiral distribution.
    /// </summary>
    /// <param name="index">Index of point (0-based).</param>
    /// <param name="radius">Radius of the sphere.</param>
    /// <param name="count">Total count of points to distribute on the sphere.</param>
    /// <returns>Calculated evently distributed point on the sphere surface.</returns>
    /// <remarks>
    /// This method uses the Fibonacci spiral algorithm to distribute points evenly on a sphere.
    /// The distribution becomes more uniform as the count increases.
    /// </remarks>
    public static Vector3 EventlyDistributedPointOnSphere(int index, float radius, int count)
    {
        var k = index + 0.5f;
        var phi = Mathf.Acos(1f - 2f * k / count);
        var theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * k;

        var x = Mathf.Cos(theta) * Mathf.Sin(phi);
        var y = Mathf.Sin(theta) * Mathf.Sin(phi);
        var z = Mathf.Cos(phi);

        return new Vector3(x, y, z) * radius;
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

    /// <summary>
    /// Get the closest point on a ray.
    /// </summary>
    /// <param name="point">A point in space.</param>
    /// <param name="origin">Start point of ray.</param>
    /// <param name="direction">Ray direction. Must be normalized.</param>
    /// <returns>Tuple which contains closest point on line and distance from <paramref name="origin"/> to calculated point.</returns>
    public static (Vector3 point, float distance) GetClosestPointOnRay(this Vector3 point, Vector3 origin,
        Vector3 direction)
    {
        var distance = Vector3.Dot(point - origin, direction);
        return (origin + direction * distance, distance);
    }

    /// <summary>
    /// Get the closest point on a line segment with improved performance.
    /// </summary>
    /// <param name="point">A point in space.</param>
    /// <param name="start">Start of line segment.</param>
    /// <param name="end">End of line segment.</param>
    /// <returns>Tuple containing the closest point and distance from <paramref name="start"/> to calculated point.</returns>
    /// <remarks>
    /// This method uses optimized algorithms for better performance compared to Unity's built-in methods.
    /// The distance returned is the distance along the line segment from the start point.
    /// </remarks>
    public static (Vector3 point, float distance) GetClosestPointOnSegment(this Vector3 point, Vector3 start,
        Vector3 end)
    {
        Vector3 direction = end - start;
        float lineMagnitude = direction.magnitude;
        direction.Normalize();

        var distance = Mathf.Clamp(Vector3.Dot(point - start, direction), 0f, lineMagnitude);
        return (start + direction * distance, distance);
    }

    /// <summary>
    /// Finds the closest <see cref="Vector3"/> in <paramref name="allTargets"/>.
    /// </summary>
    public static Vector3 FindClosest(this Vector3 origin, [System.Diagnostics.CodeAnalysis.NotNull] IList<Vector3> allTargets)
    {
        if (allTargets == null)
        {
            throw new ArgumentNullException(nameof(allTargets));
        }

        switch (allTargets.Count)
        {
            case 0: return Vector3.zero;
            case 1: return allTargets[0];
        }

        float closestDistance = Mathf.Infinity;
        var closest = Vector3.zero;

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
    /// Finds the closest <see cref="Transform"/> in <paramref name="allTargets"/>.
    /// </summary>
    public static Transform FindClosest(this Vector3 origin, [System.Diagnostics.CodeAnalysis.NotNull] IList<Transform> allTargets)
    {
        if (allTargets == null)
        {
            throw new ArgumentNullException(nameof(allTargets));
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
            float distanceSqr = (iteratingTarget.position - origin).sqrMagnitude;

            if (distanceSqr < closestDistance)
            {
                closestDistance = distanceSqr;
                closest = iteratingTarget;
            }
        }

        return closest;
    }

    /// <summary>
    /// Finds the closest <see cref="GameObject"/> in <paramref name="allTargets"/>.
    /// </summary>
    public static GameObject FindClosest(this Vector3 origin, [System.Diagnostics.CodeAnalysis.NotNull] IList<GameObject> allTargets)
    {
        if (allTargets == null)
        {
            throw new ArgumentNullException(nameof(allTargets));
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
            float distanceSqr = (iteratingTarget.transform.position - origin).sqrMagnitude;

            if (distanceSqr < closestDistance)
            {
                closestDistance = distanceSqr;
                closest = iteratingTarget;
            }
        }

        return closest;
    }

    /// <summary>
    /// Returns the Vecto3 distance between these two points
    /// </summary>
    /// <param name="start"></param>
    /// <param name="dest"></param>
    /// <returns></returns>
    public static float DistanceTo(this Vector3 start, Vector3 dest)
    {
        return Vector3.Distance(start, dest);
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

    // Set Z

    public static Vector3 SetZ(this Vector3 vector, float z)
    {
        return new Vector3(vector.x, vector.y, z);
    }

    // Set XY

    public static Vector3 SetXY(this Vector3 vector, float x, float y)
    {
        return new Vector3(x, y, vector.z);
    }

    // Set XZ

    public static Vector3 SetXZ(this Vector3 vector, float x, float z)
    {
        return new Vector3(x, vector.y, z);
    }

    // Set YZ

    public static Vector3 SetYZ(this Vector3 vector, float y, float z)
    {
        return new Vector3(vector.x, y, z);
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


    public static Vector3 OffsetXY(this Vector3 vector, float x, float y)
    {
        return new Vector3(vector.x + x, vector.y + y, vector.z);
    }


    public static Vector3 OffsetXZ(this Vector3 vector, float x, float z)
    {
        return new Vector3(vector.x + x, vector.y, vector.z + z);
    }


    public static Vector3 OffsetYZ(this Vector3 vector, float y, float z)
    {
        return new Vector3(vector.x, vector.y + y, vector.z + z);
    }

    #endregion

    #region Clamp X/Y

    public static Vector3 ClampX(this Vector3 vector, float min, float max)
    {
        return vector.SetX(Mathf.Clamp(vector.x, min, max));
    }

    public static Vector3 ClampY(this Vector3 vector, float min, float max)
    {
        return vector.SetY(Mathf.Clamp(vector.y, min, max));
    }

    #endregion


    #region Invert

    #endregion


    #region Convert

    public static Vector2 ToVector2(this Vector3 vector)
    {
        return new Vector2(vector.x, vector.y);
    }


    public static Vector3Int ToVector3Int(this Vector3 vector)
    {
        return new Vector3Int(Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y), Mathf.RoundToInt(vector.z));
    }

    #endregion


    #region Snap

    /// <summary>
    /// Snap to grid of snapValue
    /// </summary>
    public static Vector3 SnapValue(this Vector3 val, float snapValue)
    {
        return new Vector3(
            MathX.Snap(val.x, snapValue),
            MathX.Snap(val.y, snapValue),
            MathX.Snap(val.z, snapValue));
    }

    /// <summary>
    /// Snap to one unit grid
    /// </summary>
    public static Vector3 SnapToOne(this Vector3 vector)
    {
        return new Vector3(Mathf.Round(vector.x), Mathf.Round(vector.y), Mathf.Round(vector.z));
    }

    #endregion


    #region Average

    public static Vector3 AverageVector(this Vector3[] vectors)
    {
        if (vectors.IsNullOrEmpty()) return Vector3.zero;

        float x = 0f, y = 0f, z = 0f;
        for (var i = 0; i < vectors.Length; i++)
        {
            x += vectors[i].x;
            y += vectors[i].y;
            z += vectors[i].z;
        }

        return new Vector3(x / vectors.Length, y / vectors.Length, z / vectors.Length);
    }

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

    /// <summary>
    /// Finds the position closest to the given one.
    /// </summary>
    /// <param name="position">World position.</param>
    /// <param name="otherPositions">Other world positions.</param>
    /// <returns>Closest position.</returns>
    public static Vector3 GetClosest(this Vector3 position, IEnumerable<Vector3> otherPositions)
    {
        var closest = Vector3.zero;
        var shortestDistance = Mathf.Infinity;

        foreach (var otherPosition in otherPositions)
        {
            var distance = (position - otherPosition).sqrMagnitude;

            if (distance < shortestDistance)
            {
                closest = otherPosition;
                shortestDistance = distance;
            }
        }

        return closest;
    }

    public static Vector3 GetClosest(this IEnumerable<Vector3> positions, Vector3 position)
    {
        return position.GetClosest(positions);
    }

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

    /// <summary>
    /// Immutably returns the result of the source vector multiplied with
    /// another vector component-wise.
    /// </summary>
    public static Vector3 ScaleBy(this Vector3 source, Vector3 right) =>
        Vector3.Scale(source, right);

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
    /// gets the square distance between two vector3 positions. this is much faster that Vector3.distance.
    /// </summary>
    /// <param name="first">first point</param>
    /// <param name="second">second point</param>
    /// <returns>squared distance</returns>
    public static float SqrDistance(this Vector3 first, Vector3 second)
    {
        return (first.x - second.x) * (first.x - second.x) +
               (first.y - second.y) * (first.y - second.y) +
               (first.z - second.z) * (first.z - second.z);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    public static Vector3 MidPoint(this Vector3 first, Vector3 second)
    {
        return new Vector3((first.x + second.x) * 0.5f, (first.y + second.y) * 0.5f, (first.z + second.z) * 0.5f);
    }

    /// <summary>
    /// get the square distance from a point to a line segment.
    /// </summary>
    /// <param name="point">point to get distance to</param>
    /// <param name="lineP1">line segment start point</param>
    /// <param name="lineP2">line segment end point</param>
    /// <param name="closestPoint">set to either 1, 2, or 4, determining which end the point is closest to (p1, p2, or the middle)</param>
    /// <returns></returns>
    public static float SqrLineDistance(this Vector3 point, Vector3 lineP1, Vector3 lineP2, out int closestPoint)
    {
        Vector3 v = lineP2 - lineP1;
        Vector3 w = point - lineP1;

        float c1 = Vector3.Dot(w, v);

        if (c1 <= 0) //closest point is p1
        {
            closestPoint = 1;
            return SqrDistance(point, lineP1);
        }

        float c2 = Vector3.Dot(v, v);
        if (c2 <= c1) //closest point is p2
        {
            closestPoint = 2;
            return SqrDistance(point, lineP2);
        }

        float b = c1 / c2;

        Vector3 pb = lineP1 + b * v;
        {
            closestPoint = 4;
            return SqrDistance(point, pb);
        }
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
    /// Vector3.Project, onto a plane
    /// </summary>
    /// <param name="v"></param>
    /// <param name="planeNormal"></param>
    /// <returns></returns>
    public static Vector3 ProjectOntoPlane(this Vector3 v, Vector3 planeNormal)
    {
        return v - Vector3.Project(v, planeNormal);
    }

    /// <summary>
    /// Gets the normal of the triangle formed by the 3 vectors
    /// </summary>
    /// <param name="vec1"></param>
    /// <param name="vec2"></param>
    /// <param name="vec3"></param>
    /// <returns></returns>
    public static Vector3 Vector3Normal(this Vector3 vec1, Vector3 vec2, Vector3 vec3)
    {
        return Vector3.Cross((vec3 - vec1), (vec2 - vec1));
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

    /// <summary>
    ///
    /// </summary>
    /// <param name="dir1"></param>
    /// <param name="dir2"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    public static float AngleAroundAxis(this Vector3 dir1, Vector3 dir2, Vector3 axis)
    {
        dir1 = dir1 - Vector3.Project(dir1, axis);
        dir2 = dir2 - Vector3.Project(dir2, axis);

        float angle = Vector3.Angle(dir1, dir2);
        return angle * (Vector3.Dot(axis, Vector3.Cross(dir1, dir2)) < 0 ? -1 : 1);
    }

    /// <summary>
    /// test if a Vector3 is close to another Vector3 (due to floating point inprecision)
    /// compares the square of the distance to the square of the range as this
    /// avoids calculating a square root which is much slower than squaring the range
    /// </summary>
    /// <param name="val"></param>
    /// <param name="about"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    public static bool Approx(this Vector3 val, Vector3 about, float range)
    {
        return ((val - about).sqrMagnitude < range * range);
    }

    /// <summary>
    /// Find a point on the infinite line nearest to point
    /// </summary>
    /// <param name="lineStart"></param>
    /// <param name="lineEnd"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static Vector3 NearestPoint(this Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 lineDirection = Vector3.Normalize(lineEnd - lineStart);
        float closestPoint =
            Vector3.Dot((point - lineStart), lineDirection) / Vector3.Dot(lineDirection, lineDirection);
        return lineStart + (closestPoint * lineDirection);
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

    // Subtract

    #endregion

    public static Vector2 GetAnglesTo(this Vector3 referenceVector, Vector3 compareVector)
        => new Vector2(-Mathf.Asin(Vector3.Cross(compareVector, referenceVector).y) * Mathf.Rad2Deg,
            -Mathf.Asin(Vector3.Cross(compareVector, referenceVector).x) * Mathf.Rad2Deg);

    public static Vector3 RotateAround(this Vector3 point, Vector3 pivot, Quaternion rotation) =>
        rotation * (point - pivot) + pivot;

    /// <summary>
    /// Translates, rotates and scales the <paramref name="vector"/> by the position, rotation and scale of the transform.
    /// </summary>
    /// <param name="vector">Vector to transform.</param>
    /// <param name="transform">Transform to be applied.</param>
    /// <returns>Transformed vector.</returns>
    public static Vector3 ApplyTransform(this Vector3 vector, Transform transform) =>
        vector.Transform(transform.position, transform.rotation, transform.lossyScale);

    public static Vector3 Transform(this Vector3 vector, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        vector = Vector3.Scale(vector, new Vector3(scale.x, scale.y, scale.z));
        vector = vector.RotateAround(Vector3.zero, rotation);
        vector += position;
        return vector;
    }

    public static Vector3 InverseApplyTransform(this Vector3 vector, Transform transform) =>
        vector.InverseTransform(transform.position, transform.rotation, transform.lossyScale);

    public static Vector3 InverseTransform(this Vector3 vector, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        vector -= position;
        vector = vector.RotateAround(Vector3.zero, Quaternion.Inverse(rotation));
        vector = Vector3.Scale(vector, new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z));
        return vector;
    }

    public static bool NearlyEquals(this Vector3 lhs, Vector3 rhs, double inaccuracy) =>
        Vector3.SqrMagnitude(lhs - rhs) < inaccuracy;

    /// <summary>Checks near-equality using a 9.99999943962493E-11 inaccuracy threshold.</summary>
    public static bool NearlyEquals(this Vector3 lhs, Vector3 rhs) => NearlyEquals(lhs, rhs, 9.99999943962493E-11);

    public static Vector3 MidPointTo(this Vector3 origin, Vector3 destination) => new Vector3(
        (origin.x + destination.x) / 2,
        (origin.y + destination.y) / 2,
        (origin.z + destination.z) / 2
    );

    public static bool IsInside(this Vector3 vector, Collider collider) => vector == collider.ClosestPoint(vector);

    /// <summary>
    /// Transforms a <paramref name="worldPoint"/> seen by <paramref name="worldCamera"/> to a screen point within the specified <paramref name="canvas"/>.
    /// Supports only <see cref="RenderMode.ScreenSpaceCamera"/> and <see cref="RenderMode.ScreenSpaceOverlay"/> render modes for the <paramref name="canvas"/>.
    /// Returns <see cref="Vector3.negativeInfinity"/> if the <paramref name="worldPoint"/> is not in front of the <paramref name="worldCamera"/>.
    /// </summary>
    /// <param name="worldPoint">Vector3 to be transformed to a screen point.</param>
    /// <param name="worldCamera">Camera looking at the <paramref name="worldPoint"/>.</param>
    /// <param name="canvas">Target canvas of the screen point.</param>
    /// <returns>A Vector3 within the specified <paramref name="canvas"/> that is in the same screen position as the <paramref name="worldPoint"/>.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="canvas"/> has an unsupported RenderMode.</exception>
    /// <example>
    /// This sample shows how to call the <see cref="WorldToScreenPointInCanvas"/> method to set the position of a UI image.
    /// <code>
    /// var screenPoint = targetObject.transform.position.WorldToScreenPointInCanvas(Camera.main, canvas);
    /// if(screenPoint != Vector3.negativeInfinity) uiImage.RectTransform.position = screenPoint;
    /// </code>
    /// </example>
    public static Vector3 WorldToScreenPointInCanvas(this Vector3 worldPoint, Camera worldCamera, Canvas canvas)
    {
        var direction = worldPoint - worldCamera.transform.position;
        if (!(Vector3.Dot(worldCamera.transform.forward, direction) > 0.0f))
        {
            return Vector3.negativeInfinity;
        }

        var screenPoint = worldCamera.WorldToScreenPoint(worldPoint);
        switch (canvas.renderMode)
        {
            case RenderMode.ScreenSpaceCamera:
                return MathExtensions.ScreenPointToLocalPointInRectangle(canvas, position: screenPoint);
            case RenderMode.ScreenSpaceOverlay:
                return screenPoint;
            default:
                throw new InvalidOperationException(
                    "WorldToScreenPointInCanvas supports only ScreenSpaceCamera and ScreenSpaceOverlay; got " +
                    canvas.renderMode + ".");
        }
    }

    public static string ToStringVerbose(this Vector3 v) => $"({v.x}, {v.y}, {v.z})";

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

    public static float Distance2D(this Vector3 v, Vector3 other)
    {
        return Vector2.Distance(new Vector2(v.x, v.y), new Vector2(other.x, other.y));
    }

    public static float DistanceSqr2D(this Vector3 v, Vector3 other)
    {
        return (new Vector2(v.x, v.y) - new Vector2(other.x, other.y)).sqrMagnitude;
    }

    public static Vector3 MoveToward(this Vector3 v, Vector3 target, ref float speed, float maxSpeed, float accel,
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

    #region ToV3String

    /// <summary>
    /// Converts a Vector3 to a string in X, Y, Z format
    /// </summary>
    /// <param name="v3"></param>
    /// <returns></returns>
    public static string ToV3String(this Vector3 v3)
    {
        return string.Format("{0}, {1}, {2}", v3.x, v3.y, v3.z);
    }

    // ToV3String

    #endregion

    #region RotateAroundY

    /// <summary>
    /// Rotates goV3 around the vector v3, keeping y in the original position
    /// </summary>
    /// <param name="v3"></param>
    /// <param name="goV3">the game object's transform, which will be rotating</param>
    /// <returns></returns>
    public static Vector3 RotateAroundY(this Vector3 v3, Vector3 goV3)
    {
        return new Vector3(v3.x, goV3.y, v3.z);
    }

    // RotateAroundY

    #endregion

    #region StringToBytes

    /// <summary>
    /// Converts a string to bytes, in a Unity friendly way
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static byte[] StringToBytes(this string source)
    {
        // exit if null
        if (string.IsNullOrEmpty(source))
            return null;

        // convert to bytes
        using (MemoryStream compMemStream = new MemoryStream())
        {
            using (StreamWriter writer = new StreamWriter(compMemStream, Encoding.UTF8))
            {
                writer.Write(source);
                writer.Close();

                return compMemStream.ToArray();
            }
        }
    }

    // StringToBytes

    #endregion

    #region BytesToString

    /// <summary>
    /// Converts a byte array to a Unicode string, in a Unity friendly way
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static string BytesToString(this byte[] source)
    {
        // exit if null
        if (source.IsNullOrEmpty())
            return string.Empty;

        // read from bytes
        using (MemoryStream compMemStream = new MemoryStream(source))
        {
            using (StreamReader reader = new StreamReader(compMemStream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }

    // BytesToString

    #endregion

    /// <summary>
    /// Returns a vector from the element calling the method to the parameter target.
    /// </summary>
    /// <returns></returns>
    public static Vector3 RelativePosTo(this Component origin, Component target)
    {
        return (target.transform.position - origin.transform.position);
    }

    /// <summary>
    /// Returns a vector from the element calling the method to the parameter target.
    /// </summary>
    public static Vector3 RelativePosTo(this Component origin, Vector3 target)
    {
        return (target - origin.transform.position);
    }

    /// <summary>
    /// Converts a vector3 to a Vector2 using x and z values instead of x and y.
    /// </summary>
    public static Vector2 ToVector2Z(this Vector3 vector) => new Vector2(vector.x, vector.z);

    /// <summary>
    /// Returns the vector with its x value set to 0.
    /// </summary>
    public static Vector3 ZeroX(this Vector3 vector)
    {
        vector.x = 0;
        return vector;
    }

    /// <summary>
    /// Returns the vector with its y value set to 0.
    /// </summary>
    public static Vector3 ZeroY(this Vector3 vector)
    {
        vector.y = 0;
        return vector;
    }

    /// <summary>
    /// Returns the vector with its z value set to 0.
    /// </summary>
    public static Vector3 ZeroZ(this Vector3 vector)
    {
        vector.z = 0;
        return vector;
    }

    /// <summary>
    /// Returns the vector with its x value set to 1.
    /// </summary>
    public static Vector3 OneX(this Vector3 vector)
    {
        vector.x = 1;
        return vector;
    }

    /// <summary>
    /// Returns the vector with its y value set to 1.
    /// </summary>
    public static Vector3 OneY(this Vector3 vector)
    {
        vector.y = 1;
        return vector;
    }

    /// <summary>
    /// Returns the vector with its z value set to 1.
    /// </summary>
    public static Vector3 OneZ(this Vector3 vector)
    {
        vector.z = 1;
        return vector;
    }

    public static Vector3 WithX(this Vector3 vec, float x)
    {
        return new Vector3(x, vec.y, vec.z);
    }

    public static Vector3 WithY(this Vector3 vec, float y)
    {
        return new Vector3(vec.x, y, vec.z);
    }

    public static Vector3 WithZ(this Vector3 vec, float z)
    {
        return new Vector3(vec.x, vec.y, z);
    }

    public static Vector3 AddX(this Vector3 vec, float x)
    {
        return new Vector3(vec.x + x, vec.y, vec.z);
    }

    public static Vector3 AddY(this Vector3 vec, float y)
    {
        return new Vector3(vec.x, vec.y + y, vec.z);
    }

    public static Vector3 AddZ(this Vector3 vec, float z)
    {
        return new Vector3(vec.x, vec.y, vec.z + z);
    }

    public static Vector3 InvertX(this Vector3 vec)
    {
        return new Vector3(-vec.x, vec.y, vec.z);
    }

    public static Vector3 InvertY(this Vector3 vec)
    {
        return new Vector3(vec.x, -vec.y, vec.z);
    }

    public static Vector3 InvertZ(this Vector3 vec)
    {
        return new Vector3(vec.x, vec.y, -vec.z);
    }

    public static Vector3 Invert(this Vector3 vec)
    {
        return new Vector3(-vec.x, -vec.y, -vec.z);
    }


    public static Vector2Int ToVector2Int(this Vector3 v)
    {
        return new Vector2Int((int)v.x, (int)v.y);
    }

    /// <summary>
    /// Converts Vector2Int to Vector3 with optional Z value.
    /// </summary>
    /// <param name="vector2Int">Vector2Int to convert.</param>
    /// <param name="z">Z value (default: 0).</param>
    /// <returns>Vector3 representation.</returns>
    public static Vector3 ToVector3(this Vector2Int vector2Int, float z) =>
        new Vector3(vector2Int.x, vector2Int.y, z);

    /// <summary>Converts Vector2Int to Vector3 with a 0 Z value.</summary>
    public static Vector3 ToVector3(this Vector2Int vector2Int) => ToVector3(vector2Int, 0f);

    /// <summary>
    /// Converts Vector2Int to Vector3 using X and Z components.
    /// </summary>
    /// <param name="vector2Int">Vector2Int to convert.</param>
    /// <param name="y">Y value (default: 0).</param>
    /// <returns>Vector3 representation.</returns>
    public static Vector3 ToVector3XZ(this Vector2Int vector2Int, float y) =>
        new Vector3(vector2Int.x, y, vector2Int.y);

    /// <summary>Converts Vector2Int to Vector3 using X and Z components, with a 0 Y value.</summary>
    public static Vector3 ToVector3XZ(this Vector2Int vector2Int) => ToVector3XZ(vector2Int, 0f);

#if UNITY_MATHEMATICS
    /// <summary>
    /// Converts Vector3 to Unity.Mathematics.float3.
    /// </summary>
    /// <param name="vector">Vector3 to convert.</param>
    /// <returns>float3 representation.</returns>
    public static Unity.Mathematics.float3 ToFloat3(this Vector3 vector) => 
        new Unity.Mathematics.float3(vector.x, vector.y, vector.z);

    /// <summary>
    /// Converts Unity.Mathematics.float3 to Vector3.
    /// </summary>
    /// <param name="float3">float3 to convert.</param>
    /// <returns>Vector3 representation.</returns>
    public static Vector3 ToVector3(this Unity.Mathematics.float3 float3) => 
        new Vector3(float3.x, float3.y, float3.z);
#endif

    /// <summary>
    /// Creates a Vector3 from individual components with validation.
    /// </summary>
    /// <param name="x">X component.</param>
    /// <param name="y">Y component.</param>
    /// <param name="z">Z component.</param>
    /// <param name="validateNaN">Whether to validate for NaN values.</param>
    /// <returns>Vector3 with validated components.</returns>
    public static Vector3 CreateValidated(float x, float y, float z, bool validateNaN)
    {
        if (validateNaN && (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(z)))
        {
            Debug.LogWarning("NaN values detected in Vector3 creation. Using zero instead.");
            return Vector3.zero;
        }
        return new Vector3(x, y, z);
    }

    /// <summary>Creates a Vector3, validating for NaN values.</summary>
    public static Vector3 CreateValidated(float x, float y, float z) => CreateValidated(x, y, z, true);
}}
