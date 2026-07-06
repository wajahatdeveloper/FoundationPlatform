using System;
using System.Numerics;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

public static class MathExtensions
{
    public static bool ApproxZero(this float f) => Mathf.Approximately(0, f);
    public static bool Approx(this float f, float f2) => Mathf.Approximately(f, f2);
    public static float Sqr(this float f) => f * f;

    private static readonly string[] abbreviations =
    {
        "", "k", "M", "B", "T", "aa", "ab", "ac", "ad", "ae", "af", "ag", "ah", "ai", "aj", "ak", "al", "am", "an",
        "ao", "ap", "aq", "ar", "as", "at", "au", "av", "aw", "ax", "ay", "az", "ba", "bb", "bc", "bd", "be", "bf",
        "bg", "bh", "bi", "bj", "bk", "bl", "bm", "bn", "bo", "bp", "bq", "br", "bs", "bt", "bu", "bv", "bw", "bx",
        "by", "bz"
    };

    public static string ToStringAbbreviated(this BigInteger value, string format = "0.00")
    {
        // Pick the suffix using integer division on a scratch copy so the original
        // value is preserved for the fractional scaling below.
        int suffixIndex = 0;
        BigInteger threshold = BigInteger.Pow(10, 3);
        BigInteger scratch = value;
        while (scratch >= threshold && suffixIndex < abbreviations.Length - 1)
        {
            scratch /= threshold;
            suffixIndex++;
        }

        // Scale the original value in double so the fractional part survives for the
        // decimal-place format (e.g. 1,250 -> "1.25k" instead of "1.00k").
        double scaled = suffixIndex == 0 ? (double)value : (double)value / Math.Pow(1000, suffixIndex);

        // Format value with two decimal places
        string formattedValue = string.Format($"{{0:{format}}}", scaled) + abbreviations[suffixIndex];
        return formattedValue;
    }

    public static Quaternion ExtractRotation(this Matrix4x4 matrix)
    {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;

        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;

        return Quaternion.LookRotation(forward, upwards);
    }

    public static Vector3 ExtractPosition(this Matrix4x4 matrix)
    {
        Vector3 position;
        position.x = matrix.m03;
        position.y = matrix.m13;
        position.z = matrix.m23;
        return position;
    }

    public static Vector3 ExtractScale(this Matrix4x4 matrix)
    {
        Vector3 scale;
        scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
        scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
        scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
        return scale;
    }

    public static Quaternion Clamp(this Quaternion q, Vector3 min, Vector3 max)
        => Quaternion.Euler(Mathf.Clamp(MathExtensions.NormalizeAngle(q.eulerAngles.x), min.x, max.x),
            Mathf.Clamp(MathExtensions.NormalizeAngle(q.eulerAngles.y), min.y, max.y),
            Mathf.Clamp(MathExtensions.NormalizeAngle(q.eulerAngles.z), min.z, max.z));

    /// <summary>
    /// Truncates the decimal places of <paramref name="value"/> to fit <paramref name="decimalCount"/>.
    /// </summary>
    /// <example> 1.14d.LimitDecimals(1) --> 1.1d </example>
    public static double LimitDecimals(this double value, int decimalCount)
    {
        var rate = Math.Pow(10, decimalCount);
        return Math.Truncate(value * rate) / rate;

        // This below uses the rounding option, which is not precise:
        //double rounded = Math.Round(value, decimalCount, MidpointRounding.AwayFromZero);
        //return rounded;
    }

    /// <summary>
    /// Truncates the decimal places of <paramref name="value"/> to fit <paramref name="decimalCount"/>.
    /// </summary>
    /// <example> 1.14f.LimitDecimals(1) --> 1.1f </example>
    public static float LimitDecimals(this float value, int decimalCount)
    {
        var rate = Math.Pow(10, decimalCount);
        return (float)(Math.Truncate(value * rate) / rate);

        // This below uses the rounding option, which is not precise:
        //float rounded = (float)Math.Round(value, decimalCount, MidpointRounding.AwayFromZero);
        //return rounded;
    }

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

    /// <summary>
    /// <para>Calculates the shortest rotation direction from this angle to the one given in paranthesis.</para>
    /// <para>If return value is positive, shortest angle is positive way (counter-clockwise); if return value is negative shortest angle is negative way (clockwise).</para>
    /// <para>If return value is 0, this means angles are equal.</para>
    /// </summary>
    /// <param name="from">The angle to start from.</param>
    /// <param name="to"> The destination angle of rotation.</param>
    public static float ShortestRotationTo(this float from, float to)
    {
        //
        // Orginal Source: http://answers.unity.com/answers/556633/view.html
        // Modified and reformatted.
        //

        // If from or to is a negative, we have to recalculate them.
        // For an example, if from = -45 then from(-45) + 360 = 315.
        if (from < 0)
        {
            from += 360;
        }

        if (to < 0)
        {
            to += 360;
        }

        // Do not rotate if from == to.
        if (from == to ||
            from == 0 && to == 360 ||
            from == 360 && to == 0)
        {
            return 0;
        }

        // Pre-calculate left and right.
        float left = (360 - from) + to;
        float right = from - to;

        // If from < to, re-calculate left and right.
        if (from < to)
        {
            if (to > 0)
            {
                left = to - from;
                right = (360 - to) + from;
            }
            else
            {
                left = (360 - to) + from;
                right = to - from;
            }
        }

        // Determine the shortest direction.
        return (left <= right) ? left : (right * -1);
    }

    /// <summary>
    /// Mirrors the <paramref name="value"/> by <paramref name="origin"/>.
    /// </summary>
    public static int MirrorBy(this int value, int origin)
    {
        return origin + (origin - value);
    }

    /// <summary>
    /// Mirrors the <paramref name="value"/> by <paramref name="origin"/>.
    /// </summary>
    public static double MirrorBy(this double value, double origin)
    {
        return origin + (origin - value);
    }

    /// <summary>
    /// Mirrors the <paramref name="value"/> by <paramref name="origin"/>.
    /// </summary>
    public static float MirrorBy(this float value, float origin)
    {
        return origin + (origin - value);
    }

    public static void TwoDimenionalLookAt(Transform transf, Vector3 targetPos)
    {
        var toTarget = targetPos - transf.position;
        transf.rotation = Quaternion.LookRotation(Vector3.forward, -toTarget);
        transf.Rotate(Vector3.forward, -90f);
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

    public static Vector3 GetDirectionFromSpread(Quaternion rotation, float spreadAngle)
    {
        float angleOff = spreadAngle * Mathf.Deg2Rad;
        var multiplier = new Vector3(UnityEngine.Random.Range(-Mathf.Sin(angleOff), Mathf.Sin(angleOff)),
            UnityEngine.Random.Range(-Mathf.Sin(angleOff), Mathf.Sin(angleOff)),
            1.0f);
        return rotation * multiplier;
    }

    public static double DegreeToRadian(this double angle) => Math.PI * angle / 180.0;
    public static double DegreeToRadian(this float angle) => Math.PI * angle / 180.0;

    public static double RadianToDegree(this double angle) => angle * (180.0 / Math.PI);
    public static double RadianToDegree(this float angle) => angle * (180.0 / Math.PI);

    public static int WrapValue(int value, int min, int max)
        => (((value - min) % (max - min)) + (max - min)) % (max - min) + min;

    public static int RandomNegPos() => UnityEngine.Random.Range(0, 2) * 2 - 1;

    public static void SetGlobalScale(Transform transform, Vector3 globalScale)
    {
        transform.localScale = Vector3.one;
        transform.localScale = new Vector3(globalScale.x / transform.lossyScale.x,
            globalScale.y / transform.lossyScale.y,
            globalScale.z / transform.lossyScale.z);
    }

    public static byte ConvertBoolArrayToByte(bool[] source)
    {
        byte result = 0;

        // This assumes the array never contains more than 8 elements!
        int index = 8 - source.Length;
        int length = source.Length;

        // Loop through the array
        for (int i = 0; i < length; i++)
        {
            // if the element is 'true' set the bit at that position
            if (source[i])
            {
                result |= (byte)(1 << (7 - index));
            }

            index++;
        }

        return result;
    }

    public static bool[] ConvertByteToBoolArray(byte b)
    {
        // prepare the return result
        bool[] result = new bool[8];

        // check each bit in the byte. if 1 set to true, if 0 set to false
        for (int i = 0; i < 8; i++)
            result[i] = (b & (1 << i)) == 0 ? false : true;

        // reverse the array
        //System.Array.Reverse(result);

        for (int i = 0; i < result.Length / 2; i++)
        {
            bool tmp = result[i];
            result[i] = result[result.Length - i - 1];
            result[result.Length - i - 1] = tmp;
        }

        return result;
    }

    public static bool LineIntersectsRect(Vector2 p1, Vector2 p2, Rect r)
    {
        return LineIntersectsLine(p1, p2, new Vector2(r.x, r.y), new Vector2(r.x + r.width, r.y)) ||
               LineIntersectsLine(p1,
                   p2,
                   new Vector2(r.x + r.width, r.y),
                   new Vector2(r.x + r.width, r.y + r.height)) ||
               LineIntersectsLine(p1,
                   p2,
                   new Vector2(r.x + r.width, r.y + r.height),
                   new Vector2(r.x, r.y + r.height)) ||
               LineIntersectsLine(p1, p2, new Vector2(r.x, r.y + r.height), new Vector2(r.x, r.y)) ||
               (r.Contains(p1) && r.Contains(p2));
    }

    private static bool LineIntersectsLine(Vector2 l1p1, Vector2 l1p2, Vector2 l2p1, Vector2 l2p2)
    {
        float q = (l1p1.y - l2p1.y) * (l2p2.x - l2p1.x) - (l1p1.x - l2p1.x) * (l2p2.y - l2p1.y);
        float d = (l1p2.x - l1p1.x) * (l2p2.y - l2p1.y) - (l1p2.y - l1p1.y) * (l2p2.x - l2p1.x);

        if (d == 0)
        {
            return false;
        }

        float r = q / d;

        q = (l1p1.y - l2p1.y) * (l1p2.x - l1p1.x) - (l1p1.x - l2p1.x) * (l1p2.y - l1p1.y);
        float s = q / d;

        if (r < 0 || r > 1 || s < 0 || s > 1)
        {
            return false;
        }

        return true;
    }

    public static Vector3 LineIntersection3D(Vector3 p1, Vector3 v1, Vector3 p2, Vector3 v2)
    {
        //assumes the lines actually intersect

        Vector3 cross = Vector3.Cross(v1, v2);
        float magCross = cross.magnitude;

        if (magCross != 0)
        {
            Vector3 pointDiff = p2 - p1;
            Vector3 pointDiffCrossV2 = Vector3.Cross(pointDiff, v2);

            float a = pointDiffCrossV2.magnitude / magCross;

            return p1 + v1 * a;
        }
        else
        {
            Debug.LogError("error! lines are coplanar!");
            return Vector3.zero;
        }
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
    /// Returns the complement of the value so (1 - 'value').
    /// </summary>
    /// <param name="value">The value to get the complement of.</param>
    /// <returns>The complement.</returns>
    public static float Complement(this float value)
    {
        if (value < 0.0f || value > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(value), "Expects value between in range 0 to 1.");

        return 1.0f - value;
    }

    /// <summary>
    /// Returns the complement of the value so (1 - 'value').
    /// </summary>
    /// <param name="value">The value to get the complement of.</param>
    /// <returns>The complement.</returns>
    public static double Complement(this double value)
    {
        if (value < 0.0d || value > 1.0d)
            throw new ArgumentOutOfRangeException(nameof(value), "Expects value between in range 0 to 1.");

        return 1.0d - value;
    }

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

    /// <summary>
    /// Gets a percentage of the total value
    /// </summary>
    /// <param name="totalValue">The total value</param>
    /// <param name="percent">The percentage (0-100)</param>
    /// <returns>The percentage of the total value</returns>
    public static float GetPercent(this float totalValue, float percent)
    {
        return (totalValue * (percent / 100f));
    }

    /// <summary>
    /// Gets a percentage of the total value
    /// </summary>
    /// <param name="totalValue">The total value</param>
    /// <param name="percent">The percentage (0-100)</param>
    /// <returns>The percentage of the total value</returns>
    public static float GetPercent(this int totalValue, float percent)
    {
        return ((float)totalValue * (percent / 100f));
    }

    /// <summary>
    /// Returns value squared.
    /// </summary>
    /// <returns></returns>
    public static float Squared(this float num) => num * num;

    /// <summary>
    /// XNOR logical operation (exclusive NOR)
    /// </summary>
    public static bool XNOR(this bool a, bool b) => !(a ^ b);

    /// <summary>
    /// Given an int value, returns a layer mask for that layer alone.
    /// </summary>
    /// <param name="layer">Layer to create a layer mask from (0-31)</param>
    /// <returns></returns>
    public static LayerMask IntToLayerMask(this int layer)
    {
        return (1 << layer);
    }
}