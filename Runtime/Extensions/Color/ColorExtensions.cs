using System;
using AetherNexus.FoundationPlatform.SupportTypes;
using UnityEngine;


namespace AetherNexus.FoundationPlatform.Extensions
{
public static class ColorExtensions
{

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

    public static float Saturation(this Color color)
    {
        if (color == Color.black)
            return 0;

        float max = Mathf.Max(color.r, color.g, color.b);
        float delta = max - Mathf.Min(color.r, color.g, color.b);

        return 255 * delta / max;
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
