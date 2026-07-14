using System;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class BitwiseExtensions
{
    /// <summary>
    /// Get individual bit from a byte.
    /// </summary>
    /// <param name="input">Byte to extract boolean bit from.</param>
    /// <param name="index">Index from 0 to 7 of the desired bit within the byte.</param>
    /// <returns></returns>
    public static bool GetBit(this byte input, int index)
    {
        if (index < 0 || index > 7)
            throw new IndexOutOfRangeException();
        return (input & (1 << index)) != 0;
    }

    /// <summary>
    /// Set individual bit to a byte. This does not modifies the referenced byte.
    /// </summary>
    /// <param name="thisByte">Byte to insert bit into.</param>
    /// <param name="index">Index from 0 to 7 of the desired bit within the byte.</param>
    /// <param name="value">Boolean value to set bit as (0 or 1).</param>
    /// <returns></returns>
    public static byte SetBit(this byte thisByte, int index, bool value)
    {
        if (index < 0 || index > 7)
            throw new IndexOutOfRangeException();
        if (value)
            thisByte = (byte)(thisByte | 1 << index);
        else
            thisByte = (byte)(thisByte & ~(1 << index));
        return thisByte;
    }

    /// <summary>
    /// Same as SetBit, but instead actually modifies the referenced byte.
    /// Set individual bit to a byte.
    /// </summary>
    /// <param name="thisByte">Byte to insert bit into.</param>
    /// <param name="index">Index from 0 to 7 of the desired bit within the byte.</param>
    /// <param name="value">Boolean value to set bit as (0 or 1).</param>
    public static void RefSetBit(this ref byte thisByte, int index, bool value) =>
        thisByte = thisByte.SetBit(index, value);

    /// <summary>
    /// Get individual bit from an int.
    /// </summary>
    /// <param name="input">Int to extract boolean bit from.</param>
    /// <param name="index">Index from 0 to 31 of the desired bit within the int.</param>
    /// <returns></returns>
    public static bool GetBit(this int input, int index)
    {
        if (index < 0 || index > 31)
            throw new IndexOutOfRangeException();
        return (input & (1 << index)) != 0;
    }

    /// <summary>
    /// Set individual bit to an in. This does not modifies the referenced int.
    /// </summary>
    /// <param name="input">Int to insert bit into.</param>
    /// <param name="value">Boolean value to set bit as (0 or 1).</param>
    /// <param name="index">Index from 0 to 31 of the desired bit within the byte.</param>
    /// <returns></returns>
    public static int SetBit(this int input, int index, bool value)
    {
        if (index < 0 || index > 31)
            throw new IndexOutOfRangeException();
        if (value)
            input = input | (1 << index);
        else
            input = input & ~(1 << index);
        return input;
    }
}
}

