using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public static class RegexPattern
{
    /// <summary>
    /// The regex used for stripping html tags from strings.
    /// </summary>
    public static readonly Regex htmlRegex = new Regex(@"<.*?>", RegexOptions.Compiled);

    /// <summary>
    /// The regular expression used for determining whether a string is a valid hexadecimal.
    /// </summary>
    public static readonly Regex hexadecimalRegex =
        new Regex(@"^#([a-fA-F0-9]{6}|[a-fA-F0-9]{3})$", RegexOptions.Compiled);

    /// <summary>
    /// The regular expression used for determining whether a string corresponds with a variable name
    /// usable in the CSharp programming language.
    /// </summary>
    public static readonly Regex variableRegex = new Regex(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    public const string EmptyOrWhiteSpace = @"^[A-Z\s]*$";
    public const string URL = @"^(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?$";
    public const string EmailAddress = @"^([a-z0-9_\.-]+)@([\da-z\.-]+)\.([a-z\.]{2,6})$";
    public const string HexCode = @"^#?([a-f0-9]{6}|[a-f0-9]{3})$";

    public const string IPAddress =
        @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";

    public const string HTMLTag = @"^<([a-z]+)([^<]+)*(?:>(.*)<\/\1>|\s+\/>)$";

    public const string ExtractFromBrackets = @"\(([^)]*)\)";
    public const string ExtractFromSquareBrackets = @"\[([^\]]*)\]";
    public const string ExtractFromCurlyBrackets = @"\{([^\}]*)\}";

    public const string WholeNumber = @"^-?\d+$";
    public const string FloatingNumber = @"^-?\d*(\.\d+)?$";

    public const string AlphanumericWithoutSpace = @"^[a-zA-Z0-9]*$";
    public const string AlphanumericWithSpace = @"^[a-zA-Z0-9 ]*$";

    public const string Email = @"^([a-zA-Z0-9._%-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,6})*$";

    public const string URL_STRICT =
        @"(https?:\/\/)?(www\.)?[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)";
}

public static class StringExtensions
{
    public static string Fmt(this string formatString, params object[] args)
    {
        return string.Format(formatString, args);
    }

    public static bool IsWhitespace(this char character)
    {
        switch (character)
        {
            case '\u0020':
            case '\u00A0':
            case '\u1680':
            case '\u2000':
            case '\u2001':
            case '\u2002':
            case '\u2003':
            case '\u2004':
            case '\u2005':
            case '\u2006':
            case '\u2007':
            case '\u2008':
            case '\u2009':
            case '\u200A':
            case '\u202F':
            case '\u205F':
            case '\u3000':
            case '\u2028':
            case '\u2029':
            case '\u0009':
            case '\u000A':
            case '\u000B':
            case '\u000C':
            case '\u000D':
            case '\u0085':
            {
                return true;
            }

            default:
            {
                return false;
            }
        }
    }

    // While unnecessary for this project, I've used the method seen here: https://stackoverflow.com/a/37368176
    // Benchmarks: https://stackoverflow.com/a/37347881
    public static string RemoveWhitespaces(this string text)
    {
        int textLength = text.Length;

        char[] textCharacters = text.ToCharArray();

        int currentWhitespacelessTextLength = 0;

        for (int currentCharacterIndex = 0; currentCharacterIndex < textLength; ++currentCharacterIndex)
        {
            char currentTextCharacter = textCharacters[currentCharacterIndex];

            if (currentTextCharacter.IsWhitespace())
            {
                continue;
            }

            textCharacters[currentWhitespacelessTextLength++] = currentTextCharacter;
        }

        return new string(textCharacters, 0, currentWhitespacelessTextLength);
    }

    // See here for alternatives: https://stackoverflow.com/questions/3210393/how-do-i-remove-all-non-alphanumeric-characters-from-a-string-except-dash
    public static string RemoveSpecialCharacters(this string text)
    {
        int textLength = text.Length;

        char[] textCharacters = text.ToCharArray();

        int currentWhitespacelessTextLength = 0;

        for (int currentCharacterIndex = 0; currentCharacterIndex < textLength; ++currentCharacterIndex)
        {
            char currentTextCharacter = textCharacters[currentCharacterIndex];

            if (!char.IsLetterOrDigit(currentTextCharacter) && !currentTextCharacter.IsWhitespace())
            {
                continue;
            }

            textCharacters[currentWhitespacelessTextLength++] = currentTextCharacter;
        }

        return new string(textCharacters, 0, currentWhitespacelessTextLength);
    }

    /// <summary>
    /// Replace all but matching parts of the input string
    /// </summary>
    public static string KeepMatching(this Regex regex, string input) => regex.Matches(input).Cast<Match>()
        .Aggregate(string.Empty, (a, m) => a + m.Value);

    /// <summary>
    /// "Camel case string" => "CamelCaseString" 
    /// </summary>
    public static string ToCamelCase(this string message)
    {
        message = message.Replace("-", " ").Replace("_", " ");
        message = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(message);
        message = message.Replace(" ", "");
        return message;
    }

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
    /// Convert a string value to an Enum value.
    /// </summary>
    public static T AsEnum<T>(this string source, bool ignoreCase = true) where T : Enum =>
        (T)Enum.Parse(typeof(T), source, ignoreCase);

    /// <summary>
    /// Converts a hex code into corresponding color. Supports RGB and RGBA
    /// </summary>
    /// <param name="hex">Color hex code, without prefixes.</param>
    /// <returns></returns>
    public static Color ParseColor(this string hex)
    {
        int length = hex.Length;
        if (!(length == 6 || length == 8))
            throw new ArgumentException($"Color Hex code {hex} is not a valid hex code.");

        var color = new Color32();
        if (
            byte.TryParse(hex.Substring(0, 2), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo,
                out byte r) &&
            byte.TryParse(hex.Substring(2, 2), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo,
                out byte g) &&
            byte.TryParse(hex.Substring(4, 2), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo,
                out byte b))
        {
            color.r = r;
            color.b = b;
            color.g = g;
        }
        else
            throw new ArgumentException($"Color Hex code {hex} is not a valid hex code.");

        if (length == 8)
            if (byte.TryParse(hex.Substring(6, 2), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo,
                    out byte a))
                color.a = a;
            else
                throw new ArgumentException($"Color Hex code {hex} is not a valid hex code.");
        else
            color.a = 0xFF;

        return color;
    }

    /// <summary>
    /// Number presented in Roman numerals
    /// </summary>
    public static string ToRoman(this int i)
    {
        if (i > 999) return "M" + ToRoman(i - 1000);
        if (i > 899) return "CM" + ToRoman(i - 900);
        if (i > 499) return "D" + ToRoman(i - 500);
        if (i > 399) return "CD" + ToRoman(i - 400);
        if (i > 99) return "C" + ToRoman(i - 100);
        if (i > 89) return "XC" + ToRoman(i - 90);
        if (i > 49) return "L" + ToRoman(i - 50);
        if (i > 39) return "XL" + ToRoman(i - 40);
        if (i > 9) return "X" + ToRoman(i - 10);
        if (i > 8) return "IX" + ToRoman(i - 9);
        if (i > 4) return "V" + ToRoman(i - 5);
        if (i > 3) return "IV" + ToRoman(i - 4);
        if (i > 0) return "I" + ToRoman(i - 1);
        return "";
    }

    /// <summary>
    /// Get the "message" string with the "surround" string at the both sides 
    /// </summary>
    public static string SurroundedWith(this string message, string surround) => surround + message + surround;

    /// <summary>
    /// Get the "message" string with the "start" at the beginning and "end" at the end of the string
    /// </summary>
    public static string SurroundedWith(this string message, string start, string end) => start + message + end;

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
    /// Surround string with "size" tag
    /// </summary>
    public static string Sized(this string message, int size) => $"<size={size}>{message}</size>";

    /// <summary>
    /// Surround string with "u" tag
    /// </summary>
    public static string Underlined(this string message) => $"<u>{message}</u>";

    /// <summary>
    /// Surround string with "b" tag
    /// </summary>
    public static string Bold(this string message) => $"<b>{message}</b>";

    /// <summary>
    /// Surround string with "i" tag
    /// </summary>
    public static string Italics(this string message) => $"<i>{message}</i>";

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

    public static bool IsNumber(this string @string) =>
        double.TryParse(@string, NumberStyles.Any, CultureInfo.InvariantCulture, out _);

    public static bool Contains(this string @string, char @char) =>
        @string.IndexOf(@char) != -1;

    public static bool ContainsAnyOf(this string @string, params char[] chars) =>
        chars.Any(@string.Contains);

    public static string WithoutPrefix(this string @string, string prefix) =>
        @string.StartsWith(prefix) ? @string.Substring(prefix.Length) : @string;

    public static string WithoutSuffix(this string @string, string suffix) =>
        @string.EndsWith(suffix) ? @string.Remove(@string.Length - suffix.Length) : @string;

    public static string Without(this string @string, string part) =>
        @string.Replace(part, string.Empty);

    public static string AllAfter(this string @string, string part)
    {
        int index = @string.IndexOf(part, StringComparison.Ordinal);

        return index == -1 ? @string : @string.Substring(index + part.Length);
    }

    public static string FromLeft(this string @string, int length) =>
        @string.Length > length ? @string.Substring(0, length) : @string;

    public static string FromRight(this string @string, int length) =>
        @string.Length > length ? @string.Substring(@string.Length - length) : @string;

    public static float ToFloat(this string @string, NumberStyles style = NumberStyles.Any) =>
        float.Parse(@string, style, CultureInfo.InvariantCulture);

    public static float ToFloat(this string @string, float @default, NumberStyles style = NumberStyles.Any) =>
        float.TryParse(@string, style, CultureInfo.InvariantCulture, out float result) ? result : @default;

    public static int ToInt(this string @string, NumberStyles style = NumberStyles.Any) =>
        int.Parse(@string, style, CultureInfo.InvariantCulture);

    public static int ToInt(this string @string, int @default, NumberStyles style = NumberStyles.Any) =>
        int.TryParse(@string, style, CultureInfo.InvariantCulture, out int result) ? result : @default;

    public static T ToEnum<T>(this string @string) where T : struct, Enum =>
        (T)Enum.Parse(typeof(T), @string, true);

    /// <summary>
    /// Truncates a string to the specified maximum length with optional suffix.
    /// </summary>
    /// <param name="string">The string to truncate.</param>
    /// <param name="maxLength">Maximum length of the result string.</param>
    /// <param name="suffix">Suffix to append if truncation occurs. Default is "...".</param>
    /// <returns>Truncated string with suffix if needed.</returns>
    public static string Truncate(this string @string, int maxLength, string suffix = "...")
    {
        if (@string.Length <= maxLength) return @string;
        suffix ??= string.Empty;
        // Clamp so maxLength < suffix.Length (or tiny maxLength) can't produce a negative/oversized substring.
        int keep = System.Math.Max(0, maxLength - suffix.Length);
        keep = System.Math.Min(keep, @string.Length);
        return @string.Substring(0, keep) + suffix;
    }

    /// <summary>
    /// Gets a ReadOnlySpan<char> representation of the string for better performance.
    /// </summary>
    /// <param name="string">The string to convert.</param>
    /// <returns>ReadOnlySpan<char> representation.</returns>
    public static ReadOnlySpan<char> AsSpan(this string @string) => @string.AsSpan();

    /// <summary>
    /// Checks if the string starts with any of the specified prefixes.
    /// </summary>
    /// <param name="string">The string to check.</param>
    /// <param name="prefixes">Prefixes to check against.</param>
    /// <returns>True if the string starts with any of the prefixes.</returns>
    public static bool StartsWithAny(this string @string, params string[] prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (@string.StartsWith(prefix)) return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if the string ends with any of the specified suffixes.
    /// </summary>
    /// <param name="string">The string to check.</param>
    /// <param name="suffixes">Suffixes to check against.</param>
    /// <returns>True if the string ends with any of the suffixes.</returns>
    public static bool EndsWithAny(this string @string, params string[] suffixes)
    {
        foreach (var suffix in suffixes)
        {
            if (@string.EndsWith(suffix)) return true;
        }
        return false;
    }

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
}