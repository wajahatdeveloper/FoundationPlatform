using System;
using AetherNexus.FoundationPlatform.SupportTypes;
using UnityEngine;


namespace AetherNexus.FoundationPlatform.Extensions
{
public static class ColorExtensions
{
    /// <summary>
    /// Converts the given <see cref="Color"/> to a hex <see cref="uint"/> value.
    /// Would turn <see cref="Color.red"/> to <c>0xffff0000</c> (the first two values being the alpha channel).
    /// </summary>
    /// <param name="color">The color to convert.</param>
    /// <returns>The hexadecimal representation of a <see cref="Color"/> object.</returns>
    public static uint ToHex(this Color color)
    {
        uint r = (uint)(color.r * 255) << 24;
        uint g = (uint)(color.g * 255) << 16;
        uint b = (uint)(color.b * 255) << 8;
        uint a = (uint)(color.a * 255);
        return r + g + b + a;
    }

    /// <summary>
    /// Compress a color object to an int.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static int Encode(Color self)
    {
        Color32 color32 = self;

        int c = 0;

        c |= color32.a << 24;
        c |= color32.r << 16;
        c |= color32.g << 8;
        c |= color32.b;

        return c;
    }

    /// <summary>
    /// Decompress a int into a color object.
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public static Color Decode(int color)
    {
        byte colorAlpha = (byte)((color >> 24) & 0xFF);
        byte colorRed = (byte)((color >> 16) & 0xFF);
        byte colorGreen = (byte)((color >> 8) & 0xFF);
        byte colorBlue = (byte)(color & 0xFF);

        return new Color32(colorRed, colorGreen, colorBlue, colorAlpha);
    }

    /// <summary>
    /// Set value to color's channel.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="channel">Channel index of the color.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color With(this Color color, int channel, float value)
    {
        color[channel] = value;
        return color;
    }

    /// <summary>
    /// Set color's red channel value.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="r">Value to set.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color WithR(this Color color, float r) => With(color, 0, r);

    /// <summary>
    /// Set color's green channel value.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="g">Value to set.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color WithG(this Color color, float g) => With(color, 1, g);

    /// <summary>
    /// Set color's blue channel value.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="b">Value to set.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color WithB(this Color color, float b) => With(color, 2, b);

    /// <summary>
    /// Set color's alpha channel value.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="a">Value to set.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color WithA(this Color color, float a) => With(color, 3, a);

    /// <summary>
    /// Set values to color's channels.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="channel1">First channel index of the color.</param>
    /// <param name="value1">First channel value.</param>
    /// <param name="channel2">Second channel index of the color.</param>
    /// <param name="value2">Second channel value.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color With(this Color color, int channel1, float value1, int channel2, float value2)
    {
        color[channel1] = value1;
        color[channel2] = value2;

        return color;
    }

    /// <summary>
    /// Set color's red and green channels value.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="r">Value to set in red channel.</param>
    /// <param name="g">Value to set in green channel.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color WithRG(this Color color, float r, float g) => With(color, 0, r, 1, g);

    /// <summary>
    /// Set color's red and blue channels value.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="r">Value to set in red channel.</param>
    /// <param name="b">Value to set in blue channel.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color WithRB(this Color color, float r, float b) => With(color, 0, r, 2, b);

    /// <summary>
    /// Set color's red and alpha channels value.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="r">Value to set in red channel.</param>
    /// <param name="a">Value to set in alpha channel.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color WithRA(this Color color, float r, float a) => With(color, 0, r, 3, a);

    /// <summary>
    /// Set color's green and blue channels value.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="g">Value to set in green channel.</param>
    /// <param name="b">Value to set in blue channel.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color WithGB(this Color color, float g, float b) => With(color, 1, g, 2, b);

    /// <summary>
    /// Set color's green and alpha channels value.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="g">Value to set in green channel.</param>
    /// <param name="a">Value to set in alpha channel.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color WithGA(this Color color, float g, float a) => With(color, 1, g, 3, a);

    /// <summary>
    /// Set color's blue and alpha channels value.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="b">Value to set in blue channel.</param>
    /// <param name="a">Value to set in alpha channel.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color WithBA(this Color color, float b, float a) => With(color, 2, b, 3, a);

    /// <summary>
    /// Set values to color's channels.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="channel1">First channel index of the color.</param>
    /// <param name="value1">First channel value.</param>
    /// <param name="channel2">Second channel index of the color.</param>
    /// <param name="value2">Second channel value.</param>
    /// <param name="channel3">Third channel index of the color.</param>
    /// <param name="value3">Third channel value.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color With(this Color color, int channel1, float value1, int channel2, float value2, int channel3,
        float value3)
    {
        color[channel1] = value1;
        color[channel2] = value2;
        color[channel3] = value3;

        return color;
    }

    /// <summary>
    /// Set color's red, green and blue channels value.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="r">Value to set in red channel.</param>
    /// <param name="g">Value to set in green channel.</param>
    /// <param name="b">Value to set in blue channel.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color WithRGB(this Color color, float r, float g, float b) => With(color, 0, r, 1, g, 2, b);

    /// <summary>
    /// Set color's red, green and alpha channels value.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="r">Value to set in red channel.</param>
    /// <param name="g">Value to set in green channel.</param>
    /// <param name="a">Value to set in alpha channel.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color WithRGA(this Color color, float r, float g, float a) => With(color, 0, r, 1, g, 3, a);

    /// <summary>
    /// Set color's red, blue and alpha channels value.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="r">Value to set in red channel.</param>
    /// <param name="b">Value to set in blue channel.</param>
    /// <param name="a">Value to set in alpha channel.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color WithRBA(this Color color, float r, float b, float a) => With(color, 0, r, 2, b, 3, a);

    /// <summary>
    /// Set color's green, blue and alpha channels value.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="g">Value to set in green channel.</param>
    /// <param name="b">Value to set in blue channel.</param>
    /// <param name="a">Value to set in alpha channel.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color WithGBA(this Color color, float g, float b, float a) => With(color, 1, g, 2, b, 3, a);

    public static float Hue(this Color color)
    {
        float min = Mathf.Min(color.r, color.g, color.b);
        float max = Mathf.Max(color.r, color.g, color.b);

        if (max == 0 || max == min)
            return 0;
        else
        {
            float hue;
            float delta = max - min;

            if (color.r == max)
                hue = 0 + (color.g - color.b) / delta;
            else if (color.g == max)
                hue = 2 + (color.b - color.r) / delta;
            else
                hue = 4 + (color.r - color.g) / delta;

            hue *= 60;

            if (hue < 0)
                hue += 360;

            return hue;
        }
    }

    public static float Brightness(this Color color)
    {
        return Mathf.Max(color.r, color.g, color.b) * 100 / 255;
    }

    public static float Saturation(this Color color)
    {
        if (color == Color.black)
            return 0;

        float max = Mathf.Max(color.r, color.g, color.b);
        float delta = max - Mathf.Min(color.r, color.g, color.b);

        return 255 * delta / max;
    }

    public static Color Closest(this Color color, Color[] colors)
    {
        return colors.MinBy(x => GetDiff(x, color));
    }

    private static float GetDiff(Color color, Color otherColor)
    {
        float a = color.a - otherColor.a,
            r = color.r - otherColor.r,
            g = color.g - otherColor.g,
            b = color.b - otherColor.b;

        return a * a + r * r + g * g + b * b;
    }

    public static Color SetA(this Color color, float a)
    {
        return new Color(color.r, color.g, color.b, a);
    }

    public static string ColorToHex(this Color color, bool includeAlpha)
    {
        Color32 color32 = color;
        var result = color32.r.ToString("X2") + color32.g.ToString("X2") + color32.b.ToString("X2");
        if (includeAlpha) result += color32.a.ToString("X2");
        return result;
    }

    /// <summary>Converts to hex without the alpha channel.</summary>
    public static string ColorToHex(this Color color) => ColorToHex(color, false);

    public static Color HexToColor(this string inputHexString)
    {
        if (string.IsNullOrEmpty(inputHexString)) throw new ArgumentNullException(nameof(inputHexString));
        if (inputHexString.Length != 6 && inputHexString.Length != 8)
            throw new ArgumentException("Input string must have exactly 6 or 8 characters (without or with alpha).",
                nameof(inputHexString));

        var r = byte.Parse(inputHexString.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        var g = byte.Parse(inputHexString.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        var b = byte.Parse(inputHexString.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        var a = inputHexString.Length == 8
            ? byte.Parse(inputHexString.Substring(6, 2), System.Globalization.NumberStyles.HexNumber)
            : (byte)255;
        return new Color32(r, g, b, a);
    }

    public static Color GetColorFromRGB255(int r, int g, int b) => new Color(r / 255.0f, g / 255.0f, b / 255.0f);

    public static HSV GetHSV(this Color color)
    {
        return HSV.FromColor(color);
    }

    public static HSL ToHsl(this Color color)
    {
        return HSL.FromColor(color);
    }

    public static Color MakeRandomColor(this Color color, float minClamp)
    {
        var randCol = UnityEngine.Random.onUnitSphere * 3;
        randCol.x = Mathf.Clamp(randCol.x, minClamp, 1f);
        randCol.y = Mathf.Clamp(randCol.y, minClamp, 1f);
        randCol.z = Mathf.Clamp(randCol.z, minClamp, 1f);

        return new Color(randCol.x, randCol.y, randCol.z, 1f);
    }

    /// <summary>Makes a random color, clamped to a 0.5 minimum per channel.</summary>
    public static Color MakeRandomColor(this Color color) => MakeRandomColor(color, 0.5f);

    /// <summary>
    /// Direct speedup of <seealso cref="Color.Lerp"/>
    /// </summary>
    /// <param name="c1"></param>
    /// <param name="c2"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Color Lerp(Color c1, Color c2, float value)
    {
        if (value > 1.0f)
            return c2;
        if (value < 0.0f)
            return c1;
        return new Color(c1.r + (c2.r - c1.r) * value,
            c1.g + (c2.g - c1.g) * value,
            c1.b + (c2.b - c1.b) * value,
            c1.a + (c2.a - c1.a) * value);
    }
}}
