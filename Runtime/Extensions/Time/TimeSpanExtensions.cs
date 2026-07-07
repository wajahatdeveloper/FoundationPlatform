using System;

/// <summary>
/// Extension methods for TimeSpan and float time-related operations.
/// </summary>
public static class TimeSpanExtensions
{
    /// <summary>
    /// Converts a float representing seconds to a TimeSpan.
    /// </summary>
    /// <param name="seconds">Seconds as float.</param>
    /// <returns>TimeSpan representation.</returns>
    public static TimeSpan ToTimeSpan(this float seconds) =>
        TimeSpan.FromSeconds(seconds);

    /// <summary>
    /// Converts a TimeSpan to seconds as float.
    /// </summary>
    /// <param name="timeSpan">TimeSpan to convert.</param>
    /// <returns>Total seconds as float.</returns>
    public static float ToSeconds(this TimeSpan timeSpan) =>
        (float)timeSpan.TotalSeconds;

    /// <summary>
    /// Formats a TimeSpan duration using the specified format string.
    /// </summary>
    /// <param name="timeSpan">The TimeSpan to format.</param>
    /// <param name="format">Format string (default: "hh\\:mm\\:ss").</param>
    /// <returns>Formatted duration string.</returns>
    public static string ToFormattedString(this TimeSpan timeSpan, string format = "hh\\:mm\\:ss")
    {
        return timeSpan.ToString(format);
    }

    /// <summary>
    /// Gets a human-readable representation of the TimeSpan.
    /// </summary>
    /// <param name="timeSpan">The TimeSpan to format.</param>
    /// <returns>Human-readable string (e.g., "2 hours, 30 minutes").</returns>
    public static string ToHumanReadableString(this TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
            return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays != 1 ? "s" : "")}, {timeSpan.Hours} hour{(timeSpan.Hours != 1 ? "s" : "")}";
        
        if (timeSpan.TotalHours >= 1)
            return $"{timeSpan.Hours} hour{(timeSpan.Hours != 1 ? "s" : "")}, {timeSpan.Minutes} minute{(timeSpan.Minutes != 1 ? "s" : "")}";
        
        if (timeSpan.TotalMinutes >= 1)
            return $"{timeSpan.Minutes} minute{(timeSpan.Minutes != 1 ? "s" : "")}, {timeSpan.Seconds} second{(timeSpan.Seconds != 1 ? "s" : "")}";
        
        return $"{timeSpan.TotalSeconds:F1} second{(timeSpan.TotalSeconds != 1 ? "s" : "")}";
    }
}
