using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class CollectionExtensions
{
    /// <summary>
    /// Returns new array without element at index
    /// </summary>
    public static T[] RemoveAt<T>(this T[] array, int index)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), "Index is less than zero.");
        if (index >= array.Length) throw new ArgumentOutOfRangeException(nameof(index), "Index exceeds array length.");

        T[] newArray = new T[array.Length - 1];
        int index1 = 0;
        for (int index2 = 0; index2 < array.Length; ++index2)
        {
            if (index2 == index) continue;

            newArray[index1] = array[index2];
            ++index1;
        }

        return newArray;
    }

    // Note: the (TKey, TValue) and (TKey, Func<TKey, TValue>) GetOrAdd overloads live in
    // IDictionaryExtensions.cs — kept there as the single source to avoid an ambiguous-call (CS0121)
    // between two identical-signature overloads in this same namespace.

    /// <summary>
    /// Adds a key/value pair to the IDictionary&lt;TKey,TValue&gt; by using
    /// the specified function and an argument if the key does not already
    /// exist, or returns the existing value if the key exists.
    /// </summary>
    public static TValue GetOrAdd<TKey, TValue, TArg>(
        this IDictionary<TKey, TValue> source,
        TKey key,
        System.Func<TKey, TArg, TValue> valueFactory,
        TArg factoryArgument)
    {
        if (!source.ContainsKey(key))
            source[key] = valueFactory(key, factoryArgument);
        return source[key];
    }

#region One Per Instance
#endregion
}}
