using System;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class IComparableExtensions
{
    /// <summary>
    /// Checks if the object is on the specified interval. 
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="value">Target value.</param>
    /// <param name="a">Interval's start value.</param>
    /// <param name="b">Interval's end value.</param>
    /// <param name="aInclusive">Is the beginning of the interval included?</param>
    /// <param name="bInclusive">Is the end of the interval included?</param>
    /// <returns><see langword="true"/> if the <paramref name="value"/> is between <paramref name="a"/> and <paramref name="b"/>.</returns>
    public static bool IsBetween<T>(this T value, T a, T b, bool aInclusive, bool bInclusive)
        where T : IComparable
    {
        // IComparable.CompareTo only guarantees the SIGN of the result (<0, 0, >0), not exactly -1/0/1.
        if (a.CompareTo(b) > 0)
        {
            (a, b) = (b, a);
            (aInclusive, bInclusive) = (bInclusive, aInclusive);
        }

        return (aInclusive ? value.CompareTo(a) >= 0 : value.CompareTo(a) > 0) &&
               (bInclusive ? value.CompareTo(b) <= 0 : value.CompareTo(b) < 0);
    }

    /// <summary>Checks if the value is within the interval, with both ends inclusive.</summary>
    public static bool IsBetween<T>(this T value, T a, T b) where T : IComparable => IsBetween(value, a, b, true, true);
}}
