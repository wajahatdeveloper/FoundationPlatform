using System;
using System.Collections.Generic;
using UnityEngine;

public static class LayerMaskExtension
{
    public static LayerMask Create(params string[] layerNames)
    {
        return NamesToMask(layerNames);
    }

    public static LayerMask Create(params int[] layerNumbers)
    {
        return LayerNumbersToMask(layerNumbers);
    }

    public static LayerMask NamesToMask(params string[] layerNames)
    {
        LayerMask ret = (LayerMask)0;
        foreach (var name in layerNames)
        {
            ret |= (1 << LayerMask.NameToLayer(name));
        }

        return ret;
    }

    public static LayerMask LayerNumbersToMask(params int[] layerNumbers)
    {
        LayerMask ret = (LayerMask)0;
        foreach (var layer in layerNumbers)
        {
            ret |= (1 << layer);
        }

        return ret;
    }

    public static LayerMask Inverse(this LayerMask original)
    {
        return ~original;
    }

    public static LayerMask AddToMask(this LayerMask original, params string[] layerNames)
    {
        return original | NamesToMask(layerNames);
    }

    public static LayerMask RemoveFromMask(this LayerMask original, params string[] layerNames)
    {
        LayerMask invertedOriginal = ~original;
        return ~(invertedOriginal | NamesToMask(layerNames));
    }

    public static string[] MaskToNames(this LayerMask original)
    {
        var output = new List<string>();

        for (int i = 0; i < 32; ++i)
        {
            int shifted = 1 << i;
            if ((original & shifted) == shifted)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    output.Add(layerName);
                }
            }
        }

        return output.ToArray();
    }

    public static string MaskToString(this LayerMask original)
    {
        return MaskToString(original, ", ");
    }

    public static string MaskToString(this LayerMask original, string delimiter)
    {
        return string.Join(delimiter, MaskToNames(original));
    }

    public static bool IsInLayerMask(this GameObject obj, LayerMask mask)
    {
        return ((mask.value & (1 << obj.layer)) > 0);
    }

    /// <summary>
    /// Determines if given mask includes given layer. Layer parameter must NOT be bit-shifted, bit-shifting is being done inside this method.
    /// </summary>
    public static bool MaskIncludes(this int mask, int layer)
    {
        int shifted = 1 << layer;
        return (mask & shifted) == shifted;
    }

    /// <summary>
    /// Extension method to check if a layer is in a layermask
    /// </summary>
    /// <param name="mask"></param>
    /// <param name="layer"></param>
    /// <returns></returns>
    public static bool ContainsLayer(this LayerMask mask, int layer)
    {
        return mask == (mask | (1 << layer));
    }

    /// <summary>
    /// Determines if a layer is included in a LayerMask
    /// </summary>
    /// <param name="layer">The layer index to check</param>
    /// <param name="layerMask">The LayerMask to check against</param>
    /// <returns>True if the layer is in the mask, false otherwise</returns>
    public static bool IsLayerInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    public static LayerMask GetMask(params string[] layerNames)
    {
        return LayerMask.GetMask(layerNames);
    }

    public static LayerMask GetMask(params int[] layerNumbers)
    {
        if (layerNumbers == null)
            throw new ArgumentNullException(nameof(layerNumbers));

        LayerMask layerMask = 0;

        foreach (int layer in layerNumbers)
            layerMask |= 1 << layer;

        return layerMask;
    }


    public static LayerMask AddToMask(this LayerMask layerMask, params int[] layerNumbers)
    {
        return layerMask | GetMask(layerNumbers);
    }

    public static LayerMask RemoveFromMask(this LayerMask layerMask, params int[] layerNumbers)
    {
        return ~(~layerMask | GetMask(layerNumbers));
    }

    public static string[] GetLayerNames(this LayerMask layerMask)
    {
        List<string> names = new List<string>();

        for (int i = 0; i < 32; ++i)
        {
            int shiftedLayer = 1 << i;

            if ((layerMask & shiftedLayer) == shiftedLayer)
            {
                string layerName = LayerMask.LayerToName(i);

                if (!string.IsNullOrEmpty(layerName))
                    names.Add(layerName);
            }
        }

        return names.ToArray();
    }

    public static string AsString(this LayerMask layerMask)
    {
        return AsString(layerMask, ", ");
    }

    public static string AsString(this LayerMask layerMask, string separator)
    {
        return string.Join(separator, GetLayerNames(layerMask));
    }
}