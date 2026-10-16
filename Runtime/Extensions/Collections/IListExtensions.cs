using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AetherNexus.FoundationPlatform.Attributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class IListExtensions
{
    [PresentationRandom]
    public static T Random<T>(this IList<T> list)
    {
        if (list.Count == 0) throw new IndexOutOfRangeException("List needs at least one entry to call Random()");
        if (list.Count == 1) return list[0];
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    /// <summary>
    /// Swaps values at 'first' index with value at 'second' index.
    /// </summary>
    /// <param name="list">The list to swap values of.</param>
    /// <param name="firstIndex">The first index.</param>
    /// <param name="secondIndex">The second index.</param>
    /// <typeparam name="T">The type of list.</typeparam>
    public static void Swap<T>(this IList<T> list, int firstIndex, int secondIndex)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));

        if (list.Count < 2)
            throw new ArgumentException("List count should be at least 2 for a swap.");

        T firstValue = list[firstIndex];

        list[firstIndex] = list[secondIndex];
        list[secondIndex] = firstValue;
    }

    /// <summary>
    /// Shuffles the list using the Fisher-Yates algorithm.
    /// PRESENTATION-ONLY: uses UnityEngine.Random. Use the <see cref="Shuffle{T}(IList{T}, int)"/> seeded
    /// overload (or an IRandomProvider-based path) in simulation code.
    /// </summary>
    /// <param name="list">The list to shuffle.</param>
    /// <typeparam name="T">The type of list.</typeparam>
    [PresentationRandom]
    public static void Shuffle<T>(this IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            Swap(list, randomIndex, i);
        }
    }

    /// <summary>
    /// Shuffles the list using the Fisher-Yates algorithm.
    /// </summary>
    /// <param name="list">The list to shuffle.</param>
    /// <param name="seed">The seed to use for the random shuffle.</param>
    /// <typeparam name="T">The type of list.</typeparam>
    public static void Shuffle<T>(this IList<T> list, int seed)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));

        if (list.Count < 2)
            return;

        // Use a local seeded RNG for a self-contained, reproducible shuffle. Does NOT mutate
        // UnityEngine.Random global state (which would perturb any other system reading it this
        // frame) and does not depend on Unity's RNG implementation. Sim code needing the project
        // stream should route through DeterministicRandom in the gameplay assembly; FoundationPlatform
        // cannot reference it across the assembly boundary, so a local System.Random is the seam here.
        var rng = new System.Random(seed);
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = rng.Next(i, list.Count);
            Swap(list, randomIndex, i);
        }
    }

    /// <summary>
    /// Moves all items of a list to the left.
    /// </summary>
    /// <param name="list">The list to rotate.</param>
    /// <param name="count">The amount of times to move to the left.</param>
    /// <typeparam name="T">The type of list.</typeparam>
    public static void RotateLeft<T>(this IList<T> list, int count)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));

        if (list.Count < 2)
            return;

        for (int current = 0; current < count; current++)
        {
            T first = list[0];
            list.RemoveAt(0);
            list.Add(first);
        }
    }

    /// <summary>Moves all items of a list one position to the left.</summary>
    public static void RotateLeft<T>(this IList<T> list) => RotateLeft(list, 1);

    /// <summary>
    /// Moves all items of a list to the right.
    /// </summary>
    /// <param name="list">The list to rotate.</param>
    /// <param name="count">The amount of times to move to the right.</param>
    /// <typeparam name="T">The type of list.</typeparam>
    public static void RotateRight<T>(this IList<T> list, int count)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));

        if (list.Count < 2)
            return;

        int lastIndex = list.Count - 1;
        for (int current = 0; current < count; current++)
        {
            T last = list[lastIndex];
            list.RemoveAt(lastIndex);
            list.Insert(0, last);
        }
    }

    /// <summary>Moves all items of a list one position to the right.</summary>
    public static void RotateRight<T>(this IList<T> list) => RotateRight(list, 1);

    /// <summary>
    /// Checks whether the collection is null or empty.
    /// </summary>
    /// <param name="c">The collection to test.</param>
    /// <returns>True if the collection is either null, or has 0 elements in it. False otherwise.</returns>
    public static bool IsNullOrEmpty(this ICollection c)
    {
        return (c == null) || (c.Count == 0);
    }

    /// <summary>
    /// Checks whether the collection is null or empty.
    /// </summary>
    /// <param name="c">The collection to test.</param>
    /// <returns>True if the collection is either null, or has 0 elements in it. False otherwise.</returns>
    public static bool IsNullOrEmpty<TElement>(this ICollection<TElement> c)
    {
        return (c == null) || (c.Count == 0);
    }

    /// <summary>
    /// Checks whether the collection is null or empty.
    /// </summary>
    /// <param name="c">The collection to test.</param>
    /// <returns>True if the collection is either null, or has 0 elements in it. False otherwise.</returns>
    public static bool IsNullOrEmpty<TElement>(this IReadOnlyCollection<TElement> c)
    {
        return (c == null) || (c.Count == 0);
    }

    public static int IndexOf<T>(this IReadOnlyCollection<T> collection, T elementToFind)
    {
        int i = 0;

        foreach (T element in collection)
        {
            if (Equals(element, elementToFind))
                return i;

            i++;
        }

        return -1;
    }

    /// <summary>
    /// Checks whether the array is null or empty.
    /// </summary>
    /// <param name="c">The array to test.</param>
    /// <returns>True if the array is either null, or has 0 elements in it. False otherwise.</returns>
    public static bool IsNullOrEmpty<TElement>(this TElement[] c)
    {
        return (c == null) || (c.Length == 0);
    }

    /// <summary>
    /// Checks whether the list is null or empty.
    /// </summary>
    /// <param name="c">The list to test.</param>
    /// <returns>True if the list is either null, or has 0 elements in it. False otherwise.</returns>
    public static bool IsNullOrEmpty<TElement>(this List<TElement> c)
    {
        return (c == null) || (c.Count == 0);
    }

    /// <summary>
    /// Checks whether the dictionary is null or empty.
    /// </summary>
    /// <param name="d">The dictionary to test.</param>
    /// <returns>True if the dictionary is either null, or has 0 elements in it. False otherwise.</returns>
    public static bool IsNullOrEmpty<TKey, TValue>(this Dictionary<TKey, TValue> d)
    {
        return (d == null) || (d.Count == 0);
    }

    /// <summary>
    /// Checks whether the queue is null or empty.
    /// </summary>
    /// <param name="q">The queue to test.</param>
    /// <returns>True if the queue is either null, or has 0 elements in it. False otherwise.</returns>
    public static bool IsNullOrEmpty<TElement>(this Queue<TElement> q)
    {
        return (q == null) || (q.Count == 0);
    }

    /// <summary>
    /// Checks whether the collection is null or empty.
    /// </summary>
    /// <param name="a">The collection to test.</param>
    /// <returns>True if the collection is either null, or has 0 elements in it. False otherwise.</returns>
    public static bool IsNullOrEmpty(this ArrayList a)
    {
        return (a == null) || (a.Count == 0);
    }

    /// <summary>
    /// Pops element by <paramref name="index"/>.
    /// </summary>
    /// <typeparam name="T">Source type.</typeparam>
    /// <param name="list">List with elements.</param>
    /// <param name="index">Index of element to pop.</param>
    /// <returns>The popped element.</returns>
    public static T Pop<T>(this IList<T> list, int index)
    {
        var element = list[index];
        list.RemoveAt(index);

        return element;
    }

    /// <summary>
    /// Pops elements by <paramref name="indexes"/>.
    /// </summary>
    /// <typeparam name="T">Source type.</typeparam>
    /// <param name="list">List with elements.</param>
    /// <param name="indexes">Indexes of elements to be popped.</param>
    /// <returns>The popped element.</returns>
    public static List<T> Pop<T>(this IList<T> list, params int[] indexes)
    {
        var popped = new List<T>();

        foreach (var index in indexes)
            popped.Add(list.Pop(index));

        return popped;
    }

    /// <summary>
    /// An ICollection&lt;T&gt; extension method that swaps item only when it exists in a collection.
    /// </summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// <param name="this">The @this to act on.</param>
    /// <param name="oldValue">The old value.</param>
    /// <param name="newValue">The new value.</param>
    /// <returns>
    /// true if it succeeds, false if it fails.
    /// </returns>
    public static void Swap<T>(this IList<T> @this, T oldValue, T newValue)
    {
        var oldIndex = @this.IndexOf(oldValue);
        if (oldIndex >= 0)
        {
            @this[oldIndex] = newValue;
        }
    }

    /// <summary>
    /// Removes all elements starts from <paramref name="index"/>.
    /// </summary>
    /// <typeparam name="T">Elements type.</typeparam>
    /// <param name="list">The list.</param>
    /// <param name="index">From what index need starts removing?</param>
    public static void RemoveRange<T>(this IList<T> list, int index)
    {
        for (int i = list.Count - 1; i >= index; i--)
            list.RemoveAt(i);
    }

    public static T Next<T>(this IList<T> list, T item)
    {
        return list[NextPosition(list, item)];
    }

    public static T Previous<T>(this IList<T> list, T item)
    {
        return list[PreviousPosition(list, item)];
    }

    public static int NextPosition<T>(this IList<T> list, T item)
    {
        return (list.IndexOf(item) + 1) == list.Count ? 0 : (list.IndexOf(item) + 1);
    }

    public static int PreviousPosition<T>(this IList<T> list, T item)
    {
        return (list.IndexOf(item) - 1) < 0 ? list.Count - 1 : (list.IndexOf(item) - 1);
    }

    public static TValue RemoveLast<TValue>(this List<TValue> @this)
    {
        var index = @this.Count - 1;
        var result = @this[index];
        @this.RemoveAt(index);
        return result;
    }

    public static TValue First<TValue>(this List<TValue> @this)
    {
        var result = @this[0];
        return result;
    }

    public static TValue Last<TValue>(this List<TValue> @this)
    {
        var result = @this[@this.Count - 1];
        return result;
    }

    public static void InsertRange<T>(this IList<T> @this, int index, IEnumerable<T> items)
    {
        foreach (T item in items)
            @this.Insert(index++, item);
    }
}}
