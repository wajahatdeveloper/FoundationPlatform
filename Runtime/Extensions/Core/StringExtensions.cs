using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{

public static class StringExtensions
{

    /// <summary>
    /// "CamelCaseString" => "Camel Case String"
    /// </summary>
    public static string SplitCamelCase(this string camelCaseString)
    {
        if (string.IsNullOrEmpty(camelCaseString)) return camelCaseString;

        string camelCase = Regex.Replace(Regex.Replace(camelCaseString, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"),
            @"(\p{Ll})(\P{Ll})", "$1 $2");
        string firstLetter = camelCase.Substring(0, 1).ToUpper();

        if (camelCaseString.Length > 1)
        {
            string rest = camelCase.Substring(1);

            return firstLetter + rest;
        }

        return firstLetter;
    }

    /// <summary>
    /// Surround string with "color" tag
    /// </summary>
    public static string Colored(this string message, UnityConsoleColors color) => $"<color={color}>{message}</color>";

    /// <summary>
    /// Surround string with "color" tag
    /// </summary>
    public static string Colored(this string message, Color color) => $"<color={color.ColorToHex()}>{message}</color>";

    /// <summary>
    /// Surround string with "color" tag
    /// </summary>
    public static string Colored(this string message, string colorCode) => $"<color={colorCode}>{message}</color>";

    /// <summary>
    /// Surround string with "b" tag
    /// </summary>
    public static string Bold(this string message) => $"<b>{message}</b>";

    /// <summary>
    /// Represents list of supported by Unity Console color names
    /// </summary>
    public enum UnityConsoleColors
    {
        // ReSharper disable InconsistentNaming
        aqua,
        black,
        blue,
        brown,
        cyan,
        darkblue,
        fuchsia,
        green,
        grey,
        lightblue,
        lime,
        magenta,
        maroon,
        navy,
        olive,
        purple,
        red,
        silver,
        teal,
        white,

        yellow
        // ReSharper restore InconsistentNaming
    }

    public static bool IsEmpty(this string @string) =>
        @string == string.Empty;

    public static bool IsNullOrEmpty(this string @string) =>
        string.IsNullOrEmpty(@string);

    public static bool IsNullOrWhiteSpace(this string @string) =>
        string.IsNullOrWhiteSpace(@string);

    public static bool Contains(this string @string, char @char) =>
        @string.IndexOf(@char) != -1;

    public static float ToFloat(this string @string, NumberStyles style) =>
        float.Parse(@string, style, CultureInfo.InvariantCulture);

    /// <summary>Parses using NumberStyles.Any.</summary>
    public static float ToFloat(this string @string) => ToFloat(@string, NumberStyles.Any);

    public static float ToFloat(this string @string, float @default, NumberStyles style) =>
        float.TryParse(@string, style, CultureInfo.InvariantCulture, out float result) ? result : @default;

    /// <summary>Parses using NumberStyles.Any.</summary>
    public static float ToFloat(this string @string, float @default) => ToFloat(@string, @default, NumberStyles.Any);

    public static int ToInt(this string @string, NumberStyles style) =>
        int.Parse(@string, style, CultureInfo.InvariantCulture);

    /// <summary>Parses using NumberStyles.Any.</summary>
    public static int ToInt(this string @string) => ToInt(@string, NumberStyles.Any);

    public static int ToInt(this string @string, int @default, NumberStyles style) =>
        int.TryParse(@string, style, CultureInfo.InvariantCulture, out int result) ? result : @default;

    /// <summary>Parses using NumberStyles.Any.</summary>
    public static int ToInt(this string @string, int @default) => ToInt(@string, @default, NumberStyles.Any);

    /// <summary>
    /// Truncates a string to the specified maximum length with optional suffix.
    /// </summary>
    /// <param name="string">The string to truncate.</param>
    /// <param name="maxLength">Maximum length of the result string.</param>
    /// <param name="suffix">Suffix to append if truncation occurs. Default is "...".</param>
    /// <returns>Truncated string with suffix if needed.</returns>
    public static string Truncate(this string @string, int maxLength, string suffix)
    {
        if (@string.Length <= maxLength) return @string;
        suffix ??= string.Empty;
        // Clamp so maxLength < suffix.Length (or tiny maxLength) can't produce a negative/oversized substring.
        int keep = System.Math.Max(0, maxLength - suffix.Length);
        keep = System.Math.Min(keep, @string.Length);
        return @string.Substring(0, keep) + suffix;
    }

    /// <summary>Truncates a string, appending "..." if truncation occurs.</summary>
    public static string Truncate(this string @string, int maxLength) => Truncate(@string, maxLength, "...");

    /// <summary>
    /// Removes the "(Clone)" suffix from a string if present.
    /// </summary>
    /// <param name="string">The string to process.</param>
    /// <returns>The string without the "(Clone)" suffix, or the original string if the suffix is not present.</returns>
    public static string RemoveCloneSuffix(this string @string)
    {
        if (string.IsNullOrEmpty(@string)) return @string;
        return @string.EndsWith("(Clone)") ? @string.Substring(0, @string.Length - 7) : @string;
    }
}}
