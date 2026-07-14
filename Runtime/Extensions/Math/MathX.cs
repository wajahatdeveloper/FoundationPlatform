using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.Behaviours;
using UnityEngine;

/// <summary>
/// Most of these are speedups of builtin Lerps, as well as a bunch of extra lerp options
/// </summary>
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

    public const double GOLDENRATIO = 1.6180339887498948482;

    public const double PLASTICNUMBER = 1.32471795724474602596;

    public const double TAU = 6.283185307179586;

    public const double SPEEDOFLIGHT = 299792458;

    public const double PLANCKLENGTH = 1.61622938e-35;

    public const double PLANCKTIME = 5.3911613e-44;

    public const double PLANCKTEMPERATURE = 1.41680833e32;

    public const double PLANCKMASS = 4.341e-9;

    public const double GRAVITATIONALCONSTANT = 6.6740831e-11;

    public const double PLANCKSCONSTANT = 6.62e-34;

    /// <summary>
    /// One light year in metres
    /// </summary>
    public const double LIGHTYEAR = 9460730472580800;

    #endregion

    /// <summary>
    /// Computes and returns the direction between two vector3, used to check if a vector is pointing left or right of another one
    /// </summary>
    /// <returns>The <see cref="System.Single"/>.</returns>
    /// <param name="vectorA">Vector a.</param>
    /// <param name="vectorB">Vector b.</param>
    public static float AngleDirection(Vector3 vectorA, Vector3 vectorB, Vector3 up)
    {
        Vector3 cross = Vector3.Cross(vectorA, vectorB);
        float direction = Vector3.Dot(cross, up);

        return direction;
    }

    /// <summary>
    /// Returns the distance between a point and a line.
    /// </summary>
    /// <returns>The between point and line.</returns>
    /// <param name="point">Point.</param>
    /// <param name="lineStart">Line start.</param>
    /// <param name="lineEnd">Line end.</param>
    public static float DistanceBetweenPointAndLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        return Vector3.Magnitude(ProjectPointOnLine(point, lineStart, lineEnd) - point);
    }

    /// <summary>
    /// Projects a point on a line (perpendicularly) and returns the projected point.
    /// </summary>
    /// <returns>The point on line.</returns>
    /// <param name="point">Point.</param>
    /// <param name="lineStart">Line start.</param>
    /// <param name="lineEnd">Line end.</param>
    public static Vector3 ProjectPointOnLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 rhs = point - lineStart;
        Vector3 vector2 = lineEnd - lineStart;
        float magnitude = vector2.magnitude;
        Vector3 lhs = vector2;
        if (magnitude > 1E-06f)
        {
            lhs = (Vector3)(lhs / magnitude);
        }

        float num2 = Mathf.Clamp(Vector3.Dot(lhs, rhs), 0f, magnitude);
        return (lineStart + ((Vector3)(lhs * num2)));
    }

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

    /// <summary>
    /// Remaps a value x in interval [A,B], to the proportional value in interval [C,D]
    /// </summary>
    /// <param name="x">The value to remap.</param>
    /// <param name="A">the minimum bound of interval [A,B] that contains the x value</param>
    /// <param name="B">the maximum bound of interval [A,B] that contains the x value</param>
    /// <param name="C">the minimum bound of target interval [C,D]</param>
    /// <param name="D">the maximum bound of target interval [C,D]</param>
    public static float Remap(float x, float A, float B, float C, float D)
    {
        float remappedValue = C + (x - A) / (B - A) * (D - C);
        return remappedValue;
    }

    /// <summary>
    /// Clamps the angle in parameters between a minimum and maximum angle (all angles expressed in degrees)
    /// </summary>
    /// <param name="angle"></param>
    /// <param name="minimumAngle"></param>
    /// <param name="maximumAngle"></param>
    /// <returns></returns>
    public static float ClampAngle(float angle, float minimumAngle, float maximumAngle)
    {
        if (angle < -360)
        {
            angle += 360;
        }

        if (angle > 360)
        {
            angle -= 360;
        }

        return Mathf.Clamp(angle, minimumAngle, maximumAngle);
    }

    public static float RoundToDecimal(float value, int numberOfDecimals)
    {
        if (numberOfDecimals <= 0)
        {
            return Mathf.Round(value);
        }
        else
        {
            float factor = Mathf.Pow(10f, numberOfDecimals);
            return Mathf.Round(value * factor) / factor;
        }
    }

    /// <summary>
    /// Rounds the value passed in parameters to the closest value in the parameter array
    /// </summary>
    /// <param name="value"></param>
    /// <param name="possibleValues"></param>
    /// <returns></returns>
    public static float RoundToClosest(float value, float[] possibleValues, bool pickSmallestDistance = false)
    {
        if (possibleValues.Length == 0)
        {
            return 0f;
        }

        float closestValue = possibleValues[0];

        foreach (float possibleValue in possibleValues)
        {
            float closestDistance = Mathf.Abs(closestValue - value);
            float possibleDistance = Mathf.Abs(possibleValue - value);

            if (closestDistance > possibleDistance)
            {
                closestValue = possibleValue;
            }
            else if (closestDistance == possibleDistance)
            {
                if ((pickSmallestDistance && closestValue > possibleValue) ||
                    (!pickSmallestDistance && closestValue < possibleValue))
                {
                    closestValue = (value < 0) ? closestValue : possibleValue;
                }
            }
        }

        return closestValue;
    }

    /// <summary>
    /// Returns a vector3 based on the angle in parameters
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static Vector3 DirectionFromAngle(float angle, float additionalAngle)
    {
        angle += additionalAngle;

        Vector3 direction = Vector3.zero;
        direction.x = Mathf.Sin(angle * Mathf.Deg2Rad);
        direction.y = 0f;
        direction.z = Mathf.Cos(angle * Mathf.Deg2Rad);
        return direction;
    }

    /// <summary>
    /// Returns a vector3 based on the angle in parameters
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static Vector3 DirectionFromAngle2D(float angle, float additionalAngle)
    {
        angle += additionalAngle;

        Vector3 direction = Vector3.zero;
        direction.x = Mathf.Cos(angle * Mathf.Deg2Rad);
        direction.y = Mathf.Sin(angle * Mathf.Deg2Rad);
        direction.z = 0f;
        return direction;
    }

    public static T Clamp<T>(T value, T min, T max) where T : System.IComparable<T>
    {
        return value.CompareTo(max) > 0 ? max : value.CompareTo(min) < 0 ? min : value;
    }

    /// <summary>
    /// Maps a value from some arbitrary range to the 0 to 1 range
    /// </summary>
    /// <param name="value">Value.</param>
    /// <param name="min">Lminimum value.</param>
    /// <param name="max">maximum value</param>
    public static float map01(float value, float min, float max)
    {
        return (value - min) * 1f / (max - min);
    }


    /// <summary>
    /// Maps a value from some arbitrary range to the 1 to 0 range. this is just the reverse of map01
    /// </summary>
    /// <param name="value">Value.</param>
    /// <param name="min">minimum value.</param>
    /// <param name="max">maximum value</param>
    public static float map10(float value, float min, float max)
    {
        return 1f - map01(value, min, max);
    }

    #region ReMap

    public static float ReMap(
        float value,
        float inMin, float inMax,
        float outMin, float outMax
    )
    {
        return
            (value - inMin) / (inMax - inMin) * (outMax - outMin) + outMin;
    }

    public static double ReMap(
        double value,
        double inMin, double inMax,
        double outMin, double outMax
    )
    {
        return
            (value - inMin) / (inMax - inMin) * (outMax - outMin) + outMin;
    }

    public static float ReMapClamped(
        float value,
        float inMin, float inMax,
        float outMin, float outMax,
        bool clampIn = true, bool clampOut = true
    )
    {
        // Clamp signature is (value, min, max); operands are kept in their original
        // (max, min) positions to preserve the exact prior numeric behavior.
        return
            Clamp(
                ReMap(
                    Clamp(
                        value,
                        clampIn ? inMax : float.PositiveInfinity,
                        clampIn ? inMin : float.NegativeInfinity
                    ),
                    inMin, inMax,
                    outMin, outMax
                ),
                clampOut ? outMax : float.PositiveInfinity, clampOut ? outMin : float.NegativeInfinity
            );
    }

    public static double ReMapClamped(
        double value,
        double inMin, double inMax,
        double outMin, double outMax,
        bool clampIn = true, bool clampOut = true
    )
    {
        // Clamp signature is (value, min, max); operands are kept in their original
        // (max, min) positions to preserve the exact prior numeric behavior.
        return
            Clamp(
                ReMap(
                    Clamp(
                        value,
                        clampIn ? inMax : double.PositiveInfinity,
                        clampIn ? inMin : double.NegativeInfinity
                    ),
                    inMin, inMax,
                    outMin, outMax
                ),
                clampOut ? outMax : double.PositiveInfinity, clampOut ? outMin : double.NegativeInfinity
            );
    }

    public static float ClampedRemap(float input_min, float input_max, float output_min, float output_max, float value)
    {
        if (value < input_min)
        {
            return output_min;
        }
        else if (value > input_max)
        {
            return output_max;
        }
        else
        {
            return (value - input_min) / (input_max - input_min) * (output_max - output_min) + output_min;
        }
    }

    #endregion

    #region Detectors

    #region 2D Detector

    /// <summary>
    /// Compact implementation of a "Line of Sight"/"Detection" algorithm in 2D.
    /// </summary>
    /// <param name="transform">Transform of the detector entity</param>
    /// <param name="detectionLayerMask">Layer mask of detectable targets</param>
    /// <param name="detectionRange">Detection radius from detector's pivot/centre</param>
    /// <param name="detectionFOV">Detection Fielf of View in degrees</param>
    /// <param name="detectionSightlineLayerMask">"Opaque" objects layer mask. Detector will ignore those excluded layers when determining visibility to target.
    /// Target must be included</param>
    /// <param name="target">First acquired target. Null if none found.</param>
    /// <param name="usesRight">When determining field of view, assign to 'true' if 'transform.right' is considered the front. Otherwise it will use 'transform.forward'.</param>
    /// <returns></returns>
    public static bool Detection2D(Transform transform, LayerMask detectionLayerMask, float detectionRange,
        float detectionFOV, LayerMask detectionSightlineLayerMask, out Transform target, bool usesRight = false)
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, detectionRange, detectionLayerMask);
        if (cols != null && cols.Length > 0)
        {
            foreach (var item in cols)
            {
                Vector2 relativePos = item.transform.position - transform.position;
                if (Vector2.Angle((usesRight ? transform.right : transform.forward), relativePos) < detectionFOV * 0.5f)
                {
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, relativePos, detectionRange,
                        detectionSightlineLayerMask);
                    if (hit.collider != null && detectionLayerMask.ContainsLayer(hit.transform.gameObject.layer))
                    {
                        target = hit.transform;
                        return true;
                    }
                }
            }
        }

        target = null;
        return false;
    }

    #endregion

    #region 3D Detector

    /// <summary>
    /// Compact implementation of a "Line of Sight"/"Detection" algorithm in 3D.
    /// </summary>
    /// <param name="transform">Transform of the detector entity</param>
    /// <param name="detectionLayerMask">Layer mask of detectable targets</param>
    /// <param name="detectionRange">Detection radius from detector's pivot/centre</param>
    /// <param name="detectionFOV">Detection Fielf of View in degrees</param>
    /// <param name="detectionSightlineLayerMask">"Opaque" objects layer mask. Detector will ignore those excluded layers when determining visibility to target.
    /// Target must be included</param>
    /// <param name="target">First acquired target. Null if none found.</param>
    /// <param name="usesRight">When determining field of view, assign to 'true' if 'transform.right' is considered the front. Otherwise it will use 'transform.forward'.</param>
    /// <returns></returns>
    public static bool Detection3D(Transform transform, LayerMask detectionLayerMask, float detectionRange,
        float detectionFOV, LayerMask detectionSightlineLayerMask, out Transform target, bool usesRight = false) =>
        Detection3D(transform, detectionLayerMask, detectionRange, detectionFOV,
            detectionSightlineLayerMask, out target, Vector3.zero, usesRight);

    /// <summary>
    /// Compact implementation of a "Line of Sight"/"Detection" algorithm in 3D.
    /// </summary>
    /// <param name="transform">Transform of the detector entity</param>
    /// <param name="detectionLayerMask">Layer mask of detectable targets</param>
    /// <param name="detectionRange">Detection radius from detector's pivot/centre</param>
    /// <param name="detectionFOV">Detection Fielf of View in degrees</param>
    /// <param name="detectionSightlineLayerMask">"Opaque" objects layer mask. Detector will ignore those excluded layers when determining visibility to target.
    /// Target must be included</param>
    /// <param name="target">First acquired target. Null if none found.</param>
    /// <param name="deltaPos">View offset for when the "eyes" are not located at the object pivot</param>
    /// <param name="usesRight">When determining field of view, assign to 'true' if 'transform.right' is considered the front. Otherwise it will use 'transform.forward'.</param>
    /// <returns></returns>
    public static bool Detection3D(Transform transform, LayerMask detectionLayerMask, float detectionRange,
        float detectionFOV, LayerMask detectionSightlineLayerMask, out Transform target, Vector3 deltaPos,
        bool usesRight = false) =>
        Detection3D(transform, detectionLayerMask, detectionRange, detectionFOV,
            detectionSightlineLayerMask, out target, deltaPos, usesRight ? Vector3.right : Vector3.forward);


    /// <summary>
    /// Compact implementation of a "Line of Sight"/"Detection" algorithm in 3D.
    /// </summary>
    /// <param name="transform">Transform of the detector entity</param>
    /// <param name="detectionLayerMask">Layer mask of detectable targets</param>
    /// <param name="detectionRange">Detection radius from detector's pivot/centre</param>
    /// <param name="detectionFOV">Detection Fielf of View in degrees</param>
    /// <param name="detectionSightlineLayerMask">"Opaque" objects layer mask. Detector will ignore those excluded layers when determining visibility to target.
    /// Target must be included</param>
    /// <param name="target">First acquired target. Null if none found.</param>
    /// <param name="deltaPos">View offset for when the "eyes" are not located at the object pivot</param>
    /// <param name="frontDirection">Determines the direction considered as "front", relative to the transform.
    /// It will be rotated accordingly to the transform's rotation.</param>
    /// <returns></returns>
    public static bool Detection3D(Transform transform, LayerMask detectionLayerMask, float detectionRange,
        float detectionFOV, LayerMask detectionSightlineLayerMask, out Transform target, Vector3 deltaPos,
        Vector3 frontDirection)
    {
        Vector3 refPos = transform.position + (transform.rotation * deltaPos);
        Collider[] cols = Physics.OverlapSphere(refPos, detectionRange, detectionLayerMask);
        if (cols != null && cols.Length > 0)
        {
            foreach (var item in cols)
            {
                Vector3 relativePos = item.transform.position - refPos;
                if (Vector3.Angle((transform.rotation * frontDirection), relativePos) < detectionFOV * 0.5f)
                {
                    RaycastHit hit;
                    bool didHit = Physics.Raycast(refPos, relativePos, out hit, detectionRange,
                        detectionSightlineLayerMask);
                    if (didHit && detectionLayerMask.ContainsLayer(hit.transform.gameObject.layer))
                    {
                        target = hit.transform;
                        return true;
                    }
                }
            }
        }

        target = null;
        return false;
    }

    #endregion

    #region 3D Detector All

    public static bool Detection3DAll(Transform transform, LayerMask detectionLayerMask, float detectionRange,
        float detectionFOV, LayerMask detectionSightlineLayerMask, out List<Transform> targets, Vector3 deltaPos,
        bool usesRight = false)
    {
        Vector3 refPos = transform.position + (transform.rotation * deltaPos);
        Collider[] cols = Physics.OverlapSphere(refPos, detectionRange, detectionLayerMask);
        bool found = false;
        targets = new List<Transform>();

        if (cols != null && cols.Length > 0)
        {
            foreach (var item in cols)
            {
                Vector3 relativePos = item.transform.position - transform.position;
                if (Vector3.Angle((usesRight ? transform.right : transform.forward), relativePos) < detectionFOV * 0.5f)
                {
                    RaycastHit hit;

                    bool didHit = Physics.Raycast(refPos, relativePos, out hit, detectionRange,
                        detectionSightlineLayerMask);
                    if (didHit && detectionLayerMask.ContainsLayer(hit.transform.gameObject.layer))
                    {
                        targets.Add(hit.transform);
                        found = true;
                    }
                }
            }
        }

        return found;
    }

    public static bool Detection3DAll(Transform transform, LayerMask detectionLayerMask, float detectionRange,
        float detectionFOV, LayerMask detectionSightlineLayerMask, out List<Transform> targets, bool usesRight = false)
    {
        return Detection3DAll(transform, detectionLayerMask, detectionRange, detectionFOV, detectionSightlineLayerMask,
            out targets, Vector3.zero, usesRight);
    }

    #endregion

    #endregion

    /// <summary>
    /// Returns the result of rolling a dice of the specified number of sides
    /// </summary>
    /// <returns>The result of the dice roll.</returns>
    /// <param name="numberOfSides">Number of sides of the dice.</param>
    /// <param name="rnd">Randomness source — pass DeterministicRandomProvider.Instance for simulation rolls.</param>
    public static int RollADice(int numberOfSides, IRandomProvider rnd)
    {
        if (rnd == null) throw new ArgumentNullException(nameof(rnd));
        // Range(int) is max-exclusive, so +1 keeps the roll inclusive of numberOfSides (1..numberOfSides).
        return rnd.Range(1, numberOfSides + 1);
    }

    /// <summary>
    /// Returns a random success based on X% of chance.
    /// Example : I have 20% of chance to do X, Chance(20) > true, yay!
    /// </summary>
    /// <param name="percent">Percent of chance.</param>
    /// <param name="rnd">Randomness source — pass DeterministicRandomProvider.Instance for simulation rolls.</param>
    public static bool Chance(int percent, IRandomProvider rnd)
    {
        if (rnd == null) throw new ArgumentNullException(nameof(rnd));
        return rnd.Range(0, 100) <= percent;
    }

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

    public static Quaternion Slerp2D(Vector2 a, Vector2 b, float t, bool usesRight = false)
    {
        float fa;
        float fb;

        if (!usesRight)
        {
            fa = Vector2.SignedAngle(Vector2.up, a);
            fb = Vector2.SignedAngle(Vector2.up, b);
        }
        else
        {
            fa = Vector2.SignedAngle(Vector2.right, a);
            fb = Vector2.SignedAngle(Vector2.right, b);
        }

        Quaternion qa = Quaternion.AngleAxis(fa, Vector3.forward);
        Quaternion qb = Quaternion.AngleAxis(fb, Vector3.forward);

        return Quaternion.Slerp(qa, qb, t);
    }

    public static Quaternion SlerpWithAxises(Vector3 a, Vector3 b, float t, Vector3 reference, Vector3 axis)
    {
        float fa = Vector3.SignedAngle(reference, a, axis);
        float fb = Vector3.SignedAngle(reference, b, axis);

        Quaternion qa = Quaternion.AngleAxis(fa, axis);
        Quaternion qb = Quaternion.AngleAxis(fb, axis);

        return Quaternion.Slerp(qa, qb, t);
    }

    public static void Swap(ref float v1, ref float v2)
    {
        float temp = v1;
        v1 = v2;
        v2 = temp;
    }

    /// <summary>
    /// Returns true if the angle between vec1 and vec2 is smaller than the given angle.
    /// </summary>
    /// <param name="vec1">First vector</param>
    /// <param name="vec2">Second vector</param>
    /// <param name="comparisonAngle">Angle to compare against in degrees.</param>
    /// <returns></returns>
    public static bool AngleBetweenIsSmaller(Vector3 vec1, Vector3 vec2, float comparisonAngle)
        => Vector3.Angle(vec1, vec2) < comparisonAngle;

    /// <summary>
    /// Returns Cosine of angle. Listen, it may be easy, but sometimes you need a reminder
    /// </summary>
    public static float DotValueForNormalizedAngles(float degrees) => Mathf.Cos(degrees);

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
    /// Value is in [closedLeft, closedRight] range, max-inclusive.
    /// </summary>
    public static bool InRangeInclusive<T>(this T value, T closedLeft, T closedRight)
        where T : IComparable =>
        value.CompareTo(closedLeft) >= 0 && value.CompareTo(closedRight) <= 0;

    /// <summary>
    /// Check if value is in range (inclusive)
    /// </summary>
    public static bool InRange(this int value, int min, int max) =>
        value >= min && value <= max;

    /// <summary>
    /// Check if value is in range (inclusive)
    /// </summary>
    public static bool InRange(this float value, float min, float max) =>
        value >= min && value <= max;

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

    /// <summary>
    /// Check if pointA closer to num than pointB
    /// </summary>
    public static bool ClosestPointIsA(this float num, float pointA, float pointB)
    {
        var closestPoint = num.ClosestPoint(pointA, pointB);
        return Mathf.Approximately(closestPoint, pointA);
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

    public static float InverseLerpUnclamped(float from, float to, float value)
    {
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

    public static float SmoothStepUnclamped(float from, float to, float value)
    {
        value = value * value * (3.0f - 2.0f * value);
        return (1.0f - value) * from + value * to;
    }

    public static float SuperLerp(float from, float to, float from2, float to2, float value)
    {
        if (from2 < to2)
        {
            if (value < from2)
                value = from2;
            else if (value > to2)
                value = to2;
        }
        else
        {
            if (value < to2)
                value = to2;
            else if (value > from2)
                value = from2;
        }

        return (to - from) * ((value - from2) / (to2 - from2)) + from;
    }

    public static float SuperLerpUnclamped(float from, float to, float from2, float to2, float value)
    {
        return (to - from) * ((value - from2) / (to2 - from2)) + from;
    }

    public static float Hermite(float start, float end, float value)
    {
        return Mathf.Lerp(start, end, value * value * (3.0f - 2.0f * value));
    }

    public static float Sinerp(float start, float end, float value)
    {
        return Mathf.Lerp(start, end, Mathf.Sin(value * Mathf.PI * 0.5f));
    }

    public static float Coserp(float start, float end, float value)
    {
        return Mathf.Lerp(start, end, 1.0f - Mathf.Cos(value * Mathf.PI * 0.5f));
    }

    public static float Berp(float start, float end, float value)
    {
        value = Mathf.Clamp01(value);
        value = (Mathf.Sin(value * Mathf.PI * (0.2f + 2.5f * value * value * value)) * Mathf.Pow(1f - value, 2.2f) +
                 value) * (1f + (1.2f * (1f - value)));
        return start + (end - start) * value;
    }

    public static float Bounce(float x)
    {
        return Mathf.Abs(Mathf.Sin(6.28f * (x + 1f) * (x + 1f)) * (1f - x));
    }

    // test for value that is near specified float (due to floating point inprecision)
    public static bool Approx(float val, float about, float range)
    {
        return ((Mathf.Abs(val - about) < range));
    }

    /*
      * CLerp - Circular Lerp - is like lerp but handles the wraparound from 0 to 360.
      * This is useful when interpolating eulerAngles and the object
      * crosses the 0/360 boundary.  The standard Lerp function causes the object
      * to rotate in the wrong direction and looks stupid. Clerp fixes that.
      */

    public static float Clerp(float start, float end, float value)
    {
        const float min = 0.0f;
        const float max = 360.0f;
        float half = Mathf.Abs((max - min) / 2.0f); //half the distance between min and max
        float retval;
        float diff;

        if ((end - start) < -half)
        {
            diff = ((max - start) + end) * value;
            retval = start + diff;
        }
        else if ((end - start) > half)
        {
            diff = -((max - end) + start) * value;
            retval = start + diff;
        }
        else retval = start + (end - start) * value;

        // Debug.Log("Start: "  + start + "   End: " + end + "  Value: " + value + "  Half: " + half + "  Diff: " + diff + "  Retval: " + retval);
        return retval;
    }

    public static float GaussFalloff(float distance, float inRadius)
    {
        return Mathf.Clamp01(Mathf.Pow(360f, -Mathf.Pow(distance / inRadius, 2.5f) - 0.01f));
    }

    //public unsafe static float FastInvSqrt(float x)
    //{
    //    float xhalf = 0.5f * x;
    //    int i = *(int*)&x;
    //    i = 0x5f375a86 - (i >> 1); //this constant is slightly more accurate than the common one
    //    x = *(float*)&i;
    //    x = x * (1.5f - xhalf * x * x);
    //    return x;
    //}

    //public static unsafe float FastSqrt(float x)
    //{
    //    float xhalf = 0.5f * x;
    //    int i = *(int*) &x;
    //    i = 0x1fbd1df5 + (i >> 1);  // da magicks
    //    x = *(float*) &i;
    //    x = x * (1.5f - (xhalf * x * x)); //newtons method to improve approximation
    //    return x;
    //}

    public static string ConvertSeconds(int time)
    {
        var _minutes = (int)(time / 60);
        string minutes = "";
        int _seconds = (time % 60);
        string seconds = "";
        if (_minutes > 0) minutes = _minutes + " minute" + (_minutes > 1 ? "s " : " ");
        if (_seconds > 0) seconds = _seconds + " second" + (_seconds > 1 ? "s " : " ");
        var combined = minutes + seconds;
        return combined.Length == 0 ? "0 seconds" : combined.Substring(0, combined.Length - 1);
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
}