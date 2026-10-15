using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class Color32Extensions
{
    /// <summary>
    /// Set value to color's channel.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="channel">Channel index of the color.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color32 With(this Color32 color, int channel, byte value)
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
    public static Color32 WithA(this Color32 color, byte a) => With(color, 3, a);

    /// <summary>
    /// Set values to color's channels.
    /// </summary>
    /// <param name="color">Target color.</param>
    /// <param name="channel1">First channel index of the color.</param>
    /// <param name="value1">First channel value.</param>
    /// <param name="channel2">Second channel index of the color.</param>
    /// <param name="value2">Second channel value.</param>
    /// <returns>Changed copy of the color.</returns>
    public static Color32 With(this Color32 color, int channel1, byte value1, int channel2, byte value2)
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
    public static Color32 With(this Color32 color, int channel1, byte value1, int channel2, byte value2, int channel3,
        byte value3)
    {
        color[channel1] = value1;
        color[channel2] = value2;
        color[channel3] = value3;

        return color;
    }
}}
