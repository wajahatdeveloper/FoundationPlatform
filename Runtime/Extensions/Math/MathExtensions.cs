using System;
using System.Numerics;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class MathExtensions
{

    public static Quaternion Clamp(this Quaternion q, Vector3 min, Vector3 max)
        => Quaternion.Euler(Mathf.Clamp(MathExtensions.NormalizeAngle(q.eulerAngles.x), min.x, max.x),
            Mathf.Clamp(MathExtensions.NormalizeAngle(q.eulerAngles.y), min.y, max.y),
            Mathf.Clamp(MathExtensions.NormalizeAngle(q.eulerAngles.z), min.z, max.z));

    /// <summary>
    /// Square root.
    /// </summary>
    public static float Sqrt(this float number)
    {
        return (float)Math.Sqrt(number);
    }

    /// <summary>
    /// Square root.
    /// </summary>
    public static float Sqrt(this int number)
    {
        return (float)Math.Sqrt(number);
    }

    /// <summary>
    /// Returns the <paramref name="exp"/>'th power of <paramref name="baseN"/>.
    /// </summary>
    /// <param name="baseN">Base.</param>
    /// <param name="exp">Exponent (Power).</param>
    /// <remarks>Float raised to float = float</remarks>
    public static float Pow(this float baseN, float exp)
    {
        return (float)Math.Pow(baseN, exp);
    }

    /// <summary>
    /// Returns the <paramref name="exp"/>'th power of <paramref name="baseN"/>.
    /// </summary>
    /// <param name="baseN">Base.</param>
    /// <param name="exp">Exponent (Power).</param>
    /// <remarks>Integer raised to float = float</remarks>
    public static float Pow(this int baseN, float exp)
    {
        return (float)Math.Pow(baseN, exp);
    }

    /// <summary>
    /// Returns the <paramref name="exp"/>'th power of <paramref name="baseN"/>.
    /// </summary>
    /// <param name="baseN">Base.</param>
    /// <param name="exp">Exponent (Power).</param>
    /// <remarks>Integer raised to integer = integer</remarks>
    public static int Pow(this int baseN, int exp)
    {
        // Negative exponents are not representable as an integer power; preserve the
        // historical behavior of returning 0 for them rather than throwing.
        if (exp < 0)
        {
            return 0;
        }

        // Compute by exact integer multiplication instead of routing through double,
        // which avoids the precision loss / silent rounding of (int)Math.Pow.
        int result = 1;
        for (int i = 0; i < exp; i++)
        {
            result *= baseN;
        }

        return result;
    }

    /// <summary>
    /// Normalizes the angle in degrees to 0-360 range.
    /// </summary>
    public static float NormalizeAngle(this float angle)
    {
        angle = (angle + 360) % 360;

        if (angle < 0)
        {
            angle += 360;
        }

        return angle;
    }

    public static Vector3 ScreenPointToLocalPointInRectangle(Canvas canvas, Vector2 position)
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform,
            position,
            canvas.worldCamera,
            out pos);
        return canvas.transform.TransformPoint(pos);
    }

    public static int Inverse(this int value) => value * -1;

    /// <summary>
    /// Returns the inversed value. This means a positive value
    /// if the given value is negative and negative value if the 
    /// given one is positive.
    /// </summary>
    /// <param name="value">The value to inverse.</param>
    /// <returns>The inversed value.</returns>
    public static double Inverse(this double value) => value *= -1d;

    /// <summary>
    /// Returns the inversed value so a positive value
    /// if this one is negative and negative if this one is positive.
    /// </summary>
    /// <param name="value">The value to inverse.</param>
    /// <returns>The inversed value.</returns>
    public static float Inverse(this float value) => value * -1f;

    /// <summary>
    /// Returns the normalized (between 0 and 1) value.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <param name="min">The minimum value to use.</param>
    /// <param name="max">The maximum value to use.</param>
    /// <returns>The normalized value.</returns>
    public static float Normalize(this float value, float min, float max) => (value - min) / (max - min);

    /// <summary>
    /// Returns the value mapped to a new scale.
    /// </summary>
    /// <param name="value">The value to map.</param>
    /// <param name="min">The minimum range.</param>
    /// <param name="max">The maximum range.</param>
    /// <param name="targetMin">The new minimum range.</param>
    /// <param name="targetMax">The new maximum range.</param>
    /// <returns>The mapped value.</returns>
    public static float Map(this float value, float min, float max, float targetMin, float targetMax)
        => (value - min) * ((targetMax - targetMin) / (max - min)) + targetMin;
}}
