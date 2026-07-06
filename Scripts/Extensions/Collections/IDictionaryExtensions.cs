using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;

public static class IDictionaryExtensions
{
    // <summary>
    /// Finds a key (if there's one) that matches the value set in parameters
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="W"></typeparam>
    /// <param name="dictionary"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static T KeyByValue<T, W>(this Dictionary<T, W> dictionary, W value)
    {
        T key = default;
        foreach (KeyValuePair<T, W> pair in dictionary)
        {
            if (EqualityComparer<W>.Default.Equals(pair.Value, value))
            {
                key = pair.Key;
                break;
            }
        }

        return key;
    }

    /// <summary>
    /// Gets the value associated with the specified <paramref name="key"/> and casts it to type <typeparamref name="TActual"/>.
    /// </summary>
    /// <param name="data">The dictionary to search for the value in.</param>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">
    /// When this method returns, contains the value associated with the specified key, if the key is found;
    /// otherwise, the default value for the type of the <paramref name="value" /> parameter.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value in the dictionary.</typeparam>
    /// <typeparam name="TActual">Type to cast the value to when returning it.</typeparam>
    /// <returns>
    /// <see langword="true" /> if the <see cref="T:System.Collections.Generic.Dictionary`2" /> contains an element
    /// with the specified key; otherwise, <see langword="false" />.
    /// </returns>
    public static bool TryGetTypedValue<TKey, TValue, TActual>(this Dictionary<TKey, TValue> data, TKey key,
        out TActual value)
        where TActual : TValue
    {
        // Only cast on a hit, and guard the cast so a type mismatch returns false instead of throwing.
        if (data.TryGetValue(key, out TValue tmp) && tmp is TActual actual)
        {
            value = actual;
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    ///     An IDictionary extension method that converts the @this to a hashtable.
    /// </summary>
    /// <param name="this">The @this to act on.</param>
    /// <returns>@this as a Hashtable.</returns>
    public static Hashtable ToHashtable(this IDictionary @this)
    {
        return new Hashtable(@this);
    }

    /// <summary>
    ///     Uses the specified functions to add a key/value pair to the IDictionary&lt;TKey, TValue&gt; if the key does
    ///     not already exist, or to update a key/value pair in the IDictionary&lt;TKey, TValue&gt;> if the key already
    ///     exists.
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    /// <param name="this">The @this to act on.</param>
    /// <param name="key">The key to be added or whose value should be updated.</param>
    /// <param name="value">The value to be added or updated.</param>
    /// <returns>The new value for the key.</returns>
    public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, TValue value)
    {
        if (!@this.ContainsKey(key))
        {
            @this.Add(new KeyValuePair<TKey, TValue>(key, value));
        }
        else
        {
            @this[key] = value;
        }

        return @this[key];
    }

    /// <summary>
    ///     Uses the specified functions to add a key/value pair to the IDictionary&lt;TKey, TValue&gt; if the key does
    ///     not already exist, or to update a key/value pair in the IDictionary&lt;TKey, TValue&gt;> if the key already
    ///     exists.
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    /// <param name="this">The @this to act on.</param>
    /// <param name="key">The key to be added or whose value should be updated.</param>
    /// <param name="addValue">The value to be added for an absent key.</param>
    /// <param name="updateValueFactory">
    ///     The function used to generate a new value for an existing key based on the key's
    ///     existing value.
    /// </param>
    /// <returns>
    ///     The new value for the key. This will be either be addValue (if the key was absent) or the result of
    ///     updateValueFactory (if the key was present).
    /// </returns>
    public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, TValue addValue,
        Func<TKey, TValue, TValue> updateValueFactory)
    {
        if (!@this.ContainsKey(key))
        {
            @this.Add(new KeyValuePair<TKey, TValue>(key, addValue));
        }
        else
        {
            @this[key] = updateValueFactory(key, @this[key]);
        }

        return @this[key];
    }

    /// <summary>
    ///     Uses the specified functions to add a key/value pair to the IDictionary&lt;TKey, TValue&gt; if the key does
    ///     not already exist, or to update a key/value pair in the IDictionary&lt;TKey, TValue&gt;> if the key already
    ///     exists.
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    /// <param name="this">The @this to act on.</param>
    /// <param name="key">The key to be added or whose value should be updated.</param>
    /// <param name="addValueFactory">The function used to generate a value for an absent key.</param>
    /// <param name="updateValueFactory">
    ///     The function used to generate a new value for an existing key based on the key's
    ///     existing value.
    /// </param>
    /// <returns>
    ///     The new value for the key. This will be either be the result of addValueFactory (if the key was absent) or
    ///     the result of updateValueFactory (if the key was present).
    /// </returns>
    public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key,
        Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
    {
        if (!@this.ContainsKey(key))
        {
            @this.Add(new KeyValuePair<TKey, TValue>(key, addValueFactory(key)));
        }
        else
        {
            @this[key] = updateValueFactory(key, @this[key]);
        }

        return @this[key];
    }


    /// <summary>
    ///     An IDictionary&lt;TKey,TValue&gt; extension method that adds if not contains key.
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    /// <param name="this">The @this to act on.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>true if it succeeds, false if it fails.</returns>
    public static bool AddIfNotContainsKey<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, TValue value)
    {
        if (!@this.ContainsKey(key))
        {
            @this.Add(key, value);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     An IDictionary&lt;TKey,TValue&gt; extension method that adds if not contains key.
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    /// <param name="this">The @this to act on.</param>
    /// <param name="key">The key.</param>
    /// <param name="valueFactory">The value factory.</param>
    /// <returns>true if it succeeds, false if it fails.</returns>
    public static bool AddIfNotContainsKey<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key,
        Func<TValue> valueFactory)
    {
        if (!@this.ContainsKey(key))
        {
            @this.Add(key, valueFactory());
            return true;
        }

        return false;
    }

    /// <summary>
    ///     An IDictionary&lt;TKey,TValue&gt; extension method that adds if not contains key.
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    /// <param name="this">The @this to act on.</param>
    /// <param name="key">The key.</param>
    /// <param name="valueFactory">The value factory.</param>
    /// <returns>true if it succeeds, false if it fails.</returns>
    public static bool AddIfNotContainsKey<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key,
        Func<TKey, TValue> valueFactory)
    {
        if (!@this.ContainsKey(key))
        {
            @this.Add(key, valueFactory(key));
            return true;
        }

        return false;
    }


    /// <summary>
    ///     An IDictionary&lt;string,string&gt; extension method that converts the @this to a name value collection.
    /// </summary>
    /// <param name="this">The @this to act on.</param>
    /// <returns>@this as a NameValueCollection.</returns>
    public static NameValueCollection ToNameValueCollection(this IDictionary<string, string> @this)
    {
        if (@this == null)
        {
            return null;
        }

        var col = new NameValueCollection();
        foreach (var item in @this)
        {
            col.Add(item.Key, item.Value);
        }

        return col;
    }

    /// <summary>
    ///     An IDictionary&lt;TKey,TValue&gt; extension method that query if '@this' contains all key.
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    /// <param name="this">The @this to act on.</param>
    /// <param name="keys">A variable-length parameters list containing keys.</param>
    /// <returns>true if it succeeds, false if it fails.</returns>
    public static bool ContainsAllKey<TKey, TValue>(this IDictionary<TKey, TValue> @this, params TKey[] keys)
    {
        foreach (TKey value in keys)
        {
            if (!@this.ContainsKey(value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     An IDictionary&lt;TKey,TValue&gt; extension method that query if '@this' contains any key.
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    /// <param name="this">The @this to act on.</param>
    /// <param name="keys">A variable-length parameters list containing keys.</param>
    /// <returns>true if it succeeds, false if it fails.</returns>
    public static bool ContainsAnyKey<TKey, TValue>(this IDictionary<TKey, TValue> @this, params TKey[] keys)
    {
        foreach (TKey value in keys)
        {
            if (@this.ContainsKey(value))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Adds a key/value pair to the IDictionary&lt;TKey, TValue&gt; if the key does not already exist.
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    /// <param name="this">The @this to act on.</param>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value to be added, if the key does not already exist.</param>
    /// <returns>
    ///     The value for the key. This will be either the existing value for the key if the key is already in the
    ///     dictionary, or the new value if the key was not in the dictionary.
    /// </returns>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, TValue value)
    {
        if (!@this.ContainsKey(key))
        {
            @this.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        return @this[key];
    }

    /// <summary>
    ///     Adds a key/value pair to the IDictionary&lt;TKey, TValue&gt; by using the specified function, if the key does
    ///     not already exist.
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    /// <param name="this">The @this to act on.</param>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="valueFactory">TThe function used to generate a value for the key.</param>
    /// <returns>
    ///     The value for the key. This will be either the existing value for the key if the key is already in the
    ///     dictionary, or the new value for the key as returned by valueFactory if the key was not in the dictionary.
    /// </returns>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key,
        Func<TKey, TValue> valueFactory)
    {
        if (!@this.ContainsKey(key))
        {
            @this.Add(new KeyValuePair<TKey, TValue>(key, valueFactory(key)));
        }

        return @this[key];
    }

    /// <summary>
    ///     An IDictionary&lt;TKey,TValue&gt; extension method that removes if contains key.
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    /// <param name="this">The @this to act on.</param>
    /// <param name="key">The key.</param>
    public static void RemoveIfContainsKey<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key)
    {
        if (@this.ContainsKey(key))
        {
            @this.Remove(key);
        }
    }

    /// <summary>
    ///     An IDictionary&lt;TKey,TValue&gt; extension method that converts the @this to a sorted dictionary.
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    /// <param name="this">The @this to act on.</param>
    /// <returns>@this as a SortedDictionary&lt;TKey,TValue&gt;</returns>
    public static SortedDictionary<TKey, TValue> ToSortedDictionary<TKey, TValue>(this IDictionary<TKey, TValue> @this)
    {
        return new SortedDictionary<TKey, TValue>(@this);
    }

    /// <summary>
    ///     An IDictionary&lt;TKey,TValue&gt; extension method that converts the @this to a sorted dictionary.
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    /// <param name="this">The @this to act on.</param>
    /// <param name="comparer">The comparer.</param>
    /// <returns>@this as a SortedDictionary&lt;TKey,TValue&gt;</returns>
    public static SortedDictionary<TKey, TValue> ToSortedDictionary<TKey, TValue>(this IDictionary<TKey, TValue> @this,
        IComparer<TKey> comparer)
    {
        return new SortedDictionary<TKey, TValue>(@this, comparer);
    }

    /// <summary>
    /// Removes all dictionary entries where the value is null.
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    /// <param name="dictionary">The dictionary to process.</param>
    /// <returns>The count of removed entries.</returns>
    public static int RemoveNullValues<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) where TValue : class
    {
        if (dictionary == null) return 0;
        var nullKeys = dictionary.Where(pair => pair.Value == null).Select(pair => pair.Key).ToList();
        foreach (var key in nullKeys)
        {
            dictionary.Remove(key);
        }
        return nullKeys.Count;
    }
}