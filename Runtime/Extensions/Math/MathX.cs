using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.Behaviours;
using UnityEngine;

/// <summary>
/// Most of these are speedups of builtin Lerps, as well as a bunch of extra lerp options
/// </summary>
namespace AetherNexus.FoundationPlatform.Extensions
{
public static class MathX
{
    #region Ease

    public static float Linear(float t) => t;
    public static float EaseInQuad(float t) => t * t;
    public static float EaseOutQuad(float t) => t * (2 - t);
    public static float EaseInOutQuad(float t) => t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
    public static float EaseInCubic(float t) => t * t * t;
    public static float EaseOutCubic(float t) => --t * t * t + 1;
    public static float EaseInOutCubic(float t) => t < 0.5f ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
    public static float EaseInQuart(float t) => t * t * t * t;
    public static float EaseOutQuart(float t) => 1 - (--t * t * t * t);
    public static float EaseInOutQuart(float t) => t < 0.5f ? 8 * t * t * t * t : 1 - 8 * (--t) * t * t * t;
    public static float EaseInQuint(float t) => t * t * t * t * t;
    public static float EaseOutQuint(float t) => 1 + (--t * t * t * t * t);
    public static float EaseInOutQuint(float t) => t < 0.5f ? 16 * t * t * t * t * t : 1 + 16 * (--t) * t * t * t * t;
    public static float EaseInSine(float t) => 1 - Mathf.Cos(t * Mathf.PI / 2);
    public static float EaseOutSine(float t) => Mathf.Sin(t * Mathf.PI / 2);
    public static float EaseInOutSine(float t) => 0.5f * (1 - Mathf.Cos(t * Mathf.PI));
    public static float EaseInExpo(float t) => t == 0 ? 0 : Mathf.Pow(2, 10 * (t - 1));
    public static float EaseOutExpo(float t) => t == 1 ? 1 : 1 - Mathf.Pow(2, -10 * t);

    public static float EaseInOutExpo(float t) => t == 0 ? 0 :
        t == 1 ? 1 :
        t < 0.5f ? 0.5f * Mathf.Pow(2, (20 * t) - 10) : 1 - 0.5f * Mathf.Pow(2, -20 * t + 10);

    public static float EaseInCirc(float t) => 1 - Mathf.Sqrt(1 - t * t);
    public static float EaseOutCirc(float t) => Mathf.Sqrt(1 - (--t * t));

    public static float EaseInOutCirc(float t) => t < 0.5f
        ? 0.5f * (1 - Mathf.Sqrt(1 - (2 * t) * (2 * t)))
        : 0.5f * (Mathf.Sqrt(1 - (-2 * t + 2) * (-2 * t + 2)) + 1);

    // Back — slight overshoot. c1/c3 are the standard easings.net constants.
    private const float BackC1 = 1.70158f;
    private const float BackC2 = BackC1 * 1.525f;
    private const float BackC3 = BackC1 + 1f;

    public static float EaseInBack(float t) => BackC3 * t * t * t - BackC1 * t * t;
    public static float EaseOutBack(float t) => 1 + BackC3 * (--t) * t * t + BackC1 * t * t;

    public static float EaseInOutBack(float t) => t < 0.5f
        ? (2 * t) * (2 * t) * ((BackC2 + 1) * 2 * t - BackC2) / 2
        : ((2 * t - 2) * (2 * t - 2) * ((BackC2 + 1) * (2 * t - 2) + BackC2) + 2) / 2;

    // Elastic — damped spring overshoot.
    public static float EaseInElastic(float t)
    {
        if (t == 0) return 0;
        if (t == 1) return 1;
        const float c4 = (2 * Mathf.PI) / 3f;
        return -Mathf.Pow(2, 10 * t - 10) * Mathf.Sin((t * 10 - 10.75f) * c4);
    }

    public static float EaseOutElastic(float t)
    {
        if (t == 0) return 0;
        if (t == 1) return 1;
        const float c4 = (2 * Mathf.PI) / 3f;
        return Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * c4) + 1;
    }

    public static float EaseInOutElastic(float t)
    {
        if (t == 0) return 0;
        if (t == 1) return 1;
        const float c5 = (2 * Mathf.PI) / 4.5f;
        return t < 0.5f
            ? -(Mathf.Pow(2, 20 * t - 10) * Mathf.Sin((20 * t - 11.125f) * c5)) / 2
            : (Mathf.Pow(2, -20 * t + 10) * Mathf.Sin((20 * t - 11.125f) * c5)) / 2 + 1;
    }

    // Bounce — decaying rebound. (Distinct from the Berp-style Bounce(x) oscillator below.)
    public static float EaseOutBounce(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;
        if (t < 1f / d1) return n1 * t * t;
        if (t < 2f / d1) return n1 * (t -= 1.5f / d1) * t + 0.75f;
        if (t < 2.5f / d1) return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        return n1 * (t -= 2.625f / d1) * t + 0.984375f;
    }

    public static float EaseInBounce(float t) => 1 - EaseOutBounce(1 - t);

    public static float EaseInOutBounce(float t) => t < 0.5f
        ? (1 - EaseOutBounce(1 - 2 * t)) / 2
        : (1 + EaseOutBounce(2 * t - 1)) / 2;

#endregion
#region Constants
#endregion

    /// <summary>
    /// Returns the sum of all the int passed in parameters
    /// </summary>
    /// <param name="thingsToAdd">Things to add.</param>
    public static int Sum(params int[] thingsToAdd)
    {
        int result = 0;
        for (int i = 0; i < thingsToAdd.Length; i++)
        {
            result += thingsToAdd[i];
        }

        return result;
    }

    public static T Clamp<T>(T value, T min, T max) where T : System.IComparable<T>
    {
        return value.CompareTo(max) > 0 ? max : value.CompareTo(min) < 0 ? min : value;
    }

#region ReMap
#endregion
#region Detectors
#region 2D Detector
#endregion
#region 3D Detector
#endregion
#region 3D Detector All
#endregion
#endregion

    /// <summary>
    /// Moves from "from" to "to" by the specified amount and returns the corresponding value
    /// </summary>
    /// <param name="from">From.</param>
    /// <param name="to">To.</param>
    /// <param name="amount">Amount.</param>
    public static float Approach(float from, float to, float amount)
    {
        if (from < to)
        {
            from += amount;
            if (from > to)
            {
                return to;
            }
        }
        else
        {
            from -= amount;
            if (from < to)
            {
                return to;
            }
        }

        return from;
    }

    public static void Swap(ref float v1, ref float v2)
    {
        float temp = v1;
        v1 = v2;
        v2 = temp;
    }

    /// <summary>
    /// Swap two reference values
    /// </summary>
    public static void Swap<T>(ref T a, ref T b)
    {
        T x = a;
        a = b;
        b = x;
    }

    /// <summary>
    /// Snap to grid of "round" size
    /// </summary>
    public static float Snap(this float val, float round)
    {
        return round * Mathf.Round(val / round);
    }

    /// <summary>
    /// Returns the sign 1/-1 evaluated at the given value.
    /// </summary>
    public static int Sign(IComparable x) => x.CompareTo(0);

    /// <summary>
    /// Clamp value to less than min or more than max
    /// </summary>
    public static float NotInRange(this float num, float min, float max)
    {
        if (min > max)
        {
            var x = min;
            min = max;
            max = x;
        }

        if (num < min || num > max) return num;

        float mid = (max - min) / 2;

        if (num > min) return num + mid < max ? min : max;
        return num - mid > min ? max : min;
    }

    /// <summary>
    /// Clamp value to less than min or more than max
    /// </summary>
    public static int NotInRange(this int num, int min, int max)
    {
        return (int)((float)num).NotInRange(min, max);
    }

    /// <summary>
    /// Return point A or B, closest to num
    /// </summary>
    public static float ClosestPoint(this float num, float pointA, float pointB)
    {
        if (pointA > pointB)
        {
            var x = pointA;
            pointA = pointB;
            pointB = x;
        }

        float middle = (pointB - pointA) / 2;
        float withOffset = num.NotInRange(pointA, pointB) + middle;
        return (withOffset >= pointB) ? pointB : pointA;
    }

    public static float Lerp(float from, float to, float value)
    {
        if (value < 0.0f)
            return from;
        if (value > 1.0f)
            return to;
        return (to - from) * value + from;
    }

    public static float LerpUnclamped(float from, float to, float value)
    {
        return (1.0f - value) * from + value * to;
    }

    public static float InverseLerp(float from, float to, float value)
    {
        if (from < to)
        {
            if (value < from)
                return 0.0f;
            if (value > to)
                return 1.0f;
        }
        else
        {
            if (value < to)
                return 1.0f;
            if (value > @from)
                return 0.0f;
        }

        return (value - from) / (to - from);
    }

    public static float SmoothStep(float from, float to, float value)
    {
        if (value < 0.0f)
            return from;
        if (value > 1.0f)
            return to;
        value = value * value * (3.0f - 2.0f * value);
        return (1.0f - value) * from + value * to;
    }

    /// <summary>
    /// is this float approximately other
    /// </summary>
    /// <param name="a"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static bool Approximately(this float a, float other)
    {
        return Mathf.Approximately(a, other);
    }

    /// <summary>
    /// is this float within range of other
    /// </summary>
    /// <param name="x"></param>
    /// <param name="other"></param>
    /// <param name="delta"></param>
    /// <returns></returns>
    public static bool Approximately(this float x, float other, float delta)
    {
        return Math.Abs(x - other) < delta;
    }
}}
