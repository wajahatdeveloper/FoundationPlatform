using System;
using System.Collections.Generic;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
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
}}
