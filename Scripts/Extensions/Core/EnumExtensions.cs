using System;


/// <summary>
/// Provides extensions methods for working with enumerations.
/// </summary>
public static class EnumExtensions
{
    // Per-T cached values array: Enum.GetValues reflects + allocates a fresh array on every call,
    // which is wasteful for per-frame enum cycling. The cached array is only read (never mutated).
    private static class EnumCache<T> where T : Enum
    {
        public static readonly T[] Values = (T[])Enum.GetValues(typeof(T));
    }

    /// <summary>
    /// Returns the next value in the enum value sequence.
    /// Will loop back to the first value if the value is 
    /// the last.
    /// </summary>
    /// <typeparam name="T">The type of enum.</typeparam>
    /// <param name="enumValue">The enum value.</param>
    /// <returns>The next value in the enum value sequence.</returns>
    public static T Next<T>(this T enumValue) where T : Enum
    {
        T[] array = EnumCache<T>.Values;
        int i = Array.IndexOf(array, enumValue) + 1;
        return (i >= array.Length) ? array[0] : array[i];
    }

    /// <summary>
    /// Returns the previous value in the enum value sequence. 
    /// Will loop to the last value if the value is the first.
    /// </summary>
    /// <typeparam name="T">The type of enum.</typeparam>
    /// <param name="enumValue">The enum value.</param>
    /// <returns>The previous value in the enum value sequence.</returns>
    public static T Previous<T>(this T enumValue) where T : Enum
    {
        T[] array = EnumCache<T>.Values;
        int i = Array.IndexOf(array, enumValue) - 1;
        return (i < 0) ? array[array.Length - 1] : array[i];
    }

    /// <summary>
    /// Returns the underlying character value.
    /// </summary>
    /// <param name="enumValue">The enum value to get the underlying character value of.</param>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <returns>The underlying character value.</returns>
    public static char ToChar<T>(this T enumValue) where T : Enum
    {
        if (enumValue == null)
            throw new ArgumentNullException(nameof(enumValue));

        if (!typeof(char).IsAssignableFrom(Enum.GetUnderlyingType(typeof(T))))
            throw new ArgumentException("Underlying type of enum value isn't char.");

        return (char)(object)enumValue;
    }

    /// <summary>
    /// Returns the underlying byte value.
    /// </summary>
    /// <param name="enumValue">The enum value to get the underlying byte value of.</param>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <returns>The underlying byte value.</returns>
    public static byte ToByte<T>(this T enumValue) where T : Enum
    {
        if (enumValue == null)
            throw new ArgumentNullException(nameof(enumValue));

        if (!typeof(byte).IsAssignableFrom(Enum.GetUnderlyingType(typeof(T))))
            throw new ArgumentException("Underlying type of enum value isn't byte.");

        return (byte)(object)enumValue;
    }

    /// <summary>
    /// Returns the underlying integer value.
    /// </summary>
    /// <param name="enumValue">The enum value to get the underlying integer value of.</param>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <returns>The underlying integer value.</returns>
    public static int ToInt<T>(this T enumValue) where T : Enum
    {
        if (enumValue == null)
            throw new ArgumentNullException(nameof(enumValue));

        if (!typeof(int).IsAssignableFrom(Enum.GetUnderlyingType(typeof(T))))
            throw new ArgumentException("Underlying type of enum value isn't int.");

        return (int)(object)enumValue;
    }
}