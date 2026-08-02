using System;
using UnityEngine;

/// <summary>
/// Time helpers
/// </summary>
namespace AetherNexus.FoundationPlatform.Extensions
{
public class TimeX
{
    /// <summary>
    /// Turns a float (expressed in seconds) into a string displaying hours, minutes, seconds and hundredths optionnally
    /// </summary>
    /// <param name="t"></param>
    /// <param name="displayHours"></param>
    /// <param name="displayMinutes"></param>
    /// <param name="displaySeconds"></param>
    /// <param name="displayHundredths"></param>
    /// <returns></returns>
    public static string FloatToTimeString(float t, bool displayHours, bool displayMinutes,
        bool displaySeconds, bool displayMilliseconds)
    {
        int intTime = (int)t;
        int hours = intTime / 3600;
        int minutes = intTime / 60;                  // total minutes (for formats without an hours field)
        int minutesWithinHour = (intTime % 3600) / 60; // minutes after subtracting whole hours
        int seconds = intTime % 60;
        int milliseconds = Mathf.FloorToInt((t * 1000) % 1000);

        if (displayHours && displayMinutes && displaySeconds && displayMilliseconds)
        {
            return string.Format("{0:00}:{1:00}:{2:00}.{3:D3}", hours, minutesWithinHour, seconds, milliseconds);
        }

        if (!displayHours && displayMinutes && displaySeconds && displayMilliseconds)
        {
            return string.Format("{0:00}:{1:00}.{2:D3}", minutes, seconds, milliseconds);
        }

        if (!displayHours && !displayMinutes && displaySeconds && displayMilliseconds)
        {
            return string.Format("{0:D2}.{1:D3}", seconds, milliseconds);
        }

        if (!displayHours && !displayMinutes && displaySeconds && !displayMilliseconds)
        {
            return string.Format("{0:00}", seconds);
        }

        if (displayHours && displayMinutes && displaySeconds && !displayMilliseconds)
        {
            return string.Format("{0:00}:{1:00}:{2:00}", hours, minutesWithinHour, seconds);
        }

        if (!displayHours && displayMinutes && displaySeconds && !displayMilliseconds)
        {
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        throw new InvalidOperationException(
            "FloatToTimeString: unsupported combination of display flags (displayHours=" + displayHours +
            ", displayMinutes=" + displayMinutes + ", displaySeconds=" + displaySeconds +
            ", displayMilliseconds=" + displayMilliseconds + ").");
    }

    /// <summary>Formats as minutes:seconds.</summary>
    public static string FloatToTimeString(float t) => FloatToTimeString(t, false, true, true, false);

    /// <summary>
    /// Takes a hh:mm:ss:SSS string and turns it into a float value expressed in seconds
    /// </summary>
    /// <returns>a number of seconds.</returns>
    /// <param name="timeInStringNotation">Time in string notation to decode.</param>
    public static float TimeStringToFloat(string timeInStringNotation)
    {
        if (timeInStringNotation == null)
        {
            throw new ArgumentNullException(nameof(timeInStringNotation));
        }

        if (timeInStringNotation.Length != 12)
        {
            throw new ArgumentException(
                "TimeStringToFloat expects exactly 12 characters (hh:mm:ss.SSS), got length " +
                timeInStringNotation.Length + ": '" + timeInStringNotation + "'.",
                nameof(timeInStringNotation));
        }

        string[] timeStringArray = timeInStringNotation.Split(new string[] { ":", "." }, StringSplitOptions.None);
        if (timeStringArray.Length != 4)
        {
            throw new ArgumentException(
                "TimeStringToFloat expected 4 segments after split (hh:mm:ss.SSS), got " + timeStringArray.Length +
                " for '" + timeInStringNotation + "'.",
                nameof(timeInStringNotation));
        }

        float startTime = 0f;
        float result;
        if (!float.TryParse(timeStringArray[0], out result))
        {
            throw new FormatException(
                "TimeStringToFloat: invalid hours segment '" + timeStringArray[0] + "' in '" + timeInStringNotation + "'.");
        }

        startTime += result * 3600f;

        if (!float.TryParse(timeStringArray[1], out result))
        {
            throw new FormatException(
                "TimeStringToFloat: invalid minutes segment '" + timeStringArray[1] + "' in '" + timeInStringNotation + "'.");
        }

        startTime += result * 60f;

        if (!float.TryParse(timeStringArray[2], out result))
        {
            throw new FormatException(
                "TimeStringToFloat: invalid seconds segment '" + timeStringArray[2] + "' in '" + timeInStringNotation + "'.");
        }

        startTime += result;

        if (!float.TryParse(timeStringArray[3], out result))
        {
            throw new FormatException(
                "TimeStringToFloat: invalid fractional seconds segment '" + timeStringArray[3] + "' in '" +
                timeInStringNotation + "'.");
        }

        startTime += result / 1000f;

        return startTime;
    }

    /// <summary>
    /// Formats a TimeSpan duration using the specified format string.
    /// </summary>
    /// <param name="duration">The duration to format.</param>
    /// <param name="format">Format string (default: "hh\\:mm\\:ss").</param>
    /// <returns>Formatted duration string.</returns>
    public static string FormatDuration(System.TimeSpan duration, string format)
    {
        return duration.ToString(format);
    }

    /// <summary>Formats using "hh\:mm\:ss".</summary>
    public static string FormatDuration(System.TimeSpan duration) => FormatDuration(duration, "hh\\:mm\\:ss");


    /// <summary>
    /// Gets the current time as a formatted string.
    /// </summary>
    /// <param name="format">Format string (default: "yyyy-MM-dd HH:mm:ss").</param>
    /// <returns>Current time as formatted string.</returns>
    public static string GetCurrentTimeString(string format)
    {
        return System.DateTime.Now.ToString(format);
    }

    /// <summary>Formats using "yyyy-MM-dd HH:mm:ss".</summary>
    public static string GetCurrentTimeString() => GetCurrentTimeString("yyyy-MM-dd HH:mm:ss");

    /// <summary>
    /// Gets the current time as a formatted string with milliseconds.
    /// </summary>
    /// <returns>Current time with milliseconds.</returns>
    public static string GetCurrentTimeStringWithMilliseconds()
    {
        return System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }
}}
