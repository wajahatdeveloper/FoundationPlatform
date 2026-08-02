using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class IListExtensions
{
    public static T Random<T>(this IList<T> list)
    {
        if (list.Count == 0) throw new IndexOutOfRangeException("List needs at least one entry to call Random()");
        if (list.Count == 1) return list[0];
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    public static T RandomOrDefault<T>(this IList<T> list)
    {
        if (list.Count == 0) return default(T);
        return list.Random();
    }

    public static T PopLast<T>(this IList<T> list)
    {
        if (list.Count == 0) throw new InvalidOperationException("List is empty");
        var t = list[list.Count - 1];
        list.RemoveAt(list.Count - 1);
        return t;
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
    /// Removes null entries from a list.
    /// </summary>
    /// <typeparam name="T">The type of list.</typeparam>
    /// <param name="list">The list to remove null entries from.</param>
    public static void RemoveNullEntries<T>(this IList<T> list) where T : class
    {
        for (int i = list.Count - 1; i >= 0; i--)
            if (Equals(list[i], null))
                list.RemoveAt(i);
    }

    /// <summary>
    /// Removes default values from a list.
    /// </summary>
    /// <typeparam name="T">The type of list.</typeparam>
    /// <param name="list">The list to remove default values from.</param>
    public static void RemoveDefaultValues<T>(this IList<T> list)
    {
        for (int i = list.Count - 1; i >= 0; i--)
            if (Equals(default(T), list[i]))
                list.RemoveAt(i);
    }

    /// <summary>
    /// Returns whether an index is inside the bounds of the list.
    /// </summary>
    /// <typeparam name="T">The type of list to check the bounds of.</typeparam>
    /// <param name="list">The list to check the bounds of.</param>
    /// <param name="index">The index to check.</param>
    /// <returns>Whether the index is inside the bounds.</returns>
    public static bool HasIndex<T>(this IList<T> list, int index) => index.InRange(0, list.Count - 1);


    /// <summary>
    /// Inserts a value in the list, assuming it is already sorted, preserving the order of elements.
    /// </summary>
    /// <param name="list">The list in which to insert the element.</param>
    /// <param name="value">Value to insert.</param>
    /// <typeparam name="T">The element type of the list.</typeparam>
    public static void SortedInsert<T>(this IList<T> list, T value)
        where T : IComparable<T>
    {
        SortedInsert(list, value, (a, b) => a.CompareTo(b));
    }

    /// <summary>
    /// Inserts a collection of values in the list, assuming it is already sorted, preserving the order of elements.
    /// </summary>
    /// <param name="list">The list in which to insert the elements.</param>
    /// <param name="values">The elements to be added to the list.</param>
    /// <typeparam name="T">The element type of the list.</typeparam>
    public static void SortedInsert<T>(this IList<T> list, IEnumerable<T> values)
        where T : IComparable<T>
    {
        values.ThrowIfNull(nameof(values));
        foreach (T value in values)
        {
            list.SortedInsert(value);
        }
    }

    /// <summary>
    /// Inserts a value in the list, assuming it is already sorted, preserving the order of elements.
    /// </summary>
    /// <param name="list">The list in which to insert the value.</param>
    /// <param name="value">Value to insert.</param>
    /// <param name="comparison">Comparison operator to determine the order of elements.</param>
    /// <typeparam name="T">The element type of the list.</typeparam>
    public static void SortedInsert<T>(this IList<T> list, T value, Comparison<T> comparison)
    {
        list.ThrowIfNull(nameof(list));
        comparison.ThrowIfNull(nameof(comparison));

        // If no elements exist in the list, add it.
        if (list.Count == 0)
        {
            list.Add(value);
            return;
        }

        // Search for the insertion index using binary search.
        int startIndex = 0;
        int endIndex = list.Count;
        while (endIndex > startIndex)
        {
            int windowSize = endIndex - startIndex;
            int middleIndex = startIndex + (windowSize / 2);
            T middleValue = list[middleIndex];
            int compareToResult = comparison(middleValue, value);
            if (compareToResult == 0)
            {
                list.Insert(middleIndex, value);
                return;
            }
            else if (compareToResult < 0)
            {
                startIndex = middleIndex + 1;
            }
            else
            {
                endIndex = middleIndex;
            }
        }

        list.Insert(startIndex, value);
    }

    /// <summary>
    /// Inserts a set of values in the list, assuming it is already sorted, preserving the order of elements.
    /// </summary>
    /// <param name="list">The list in which to insert the values.</param>
    /// <param name="values">Values to insert.</param>
    /// <param name="comparison">Comparison operator to determine the order of elements.</param>
    /// <typeparam name="T">The element type of the list.</typeparam>
    public static void SortedInsert<T>(this IList<T> list, IEnumerable<T> values, Comparison<T> comparison)
    {
        values.ThrowIfNull(nameof(values));
        comparison.ThrowIfNull(nameof(comparison));

        foreach (T value in values)
        {
            list.SortedInsert(value, comparison);
        }
    }

    /// <summary>
    /// Inserts a value in the list, assuming it is already sorted, preserving the order of elements.
    /// </summary>
    /// <param name="list">The list in which to insert the value.</param>
    /// <param name="value">Value to insert.</param>
    public static void SortedInsert(this IList list, IComparable value)
    {
        SortedInsert(list, value, (a, b) => a.CompareTo(b));
    }

    /// <summary>
    /// Inserts a set of values in the list, assuming it is already sorted, preserving the order of elements.
    /// </summary>
    /// <param name="list">The list in which to insert the values.</param>
    /// <param name="values">Values to insert.</param>
    public static void SortedInsert(this IList list, IEnumerable<IComparable> values)
    {
        values.ThrowIfNull(nameof(values));
        foreach (IComparable value in values)
        {
            list.SortedInsert(value);
        }
    }

    /// <summary>
    /// Inserts a value in the list, assuming it is already sorted, preserving the order of elements.
    /// </summary>
    /// <param name="list">The list in which to insert the values.</param>
    /// <param name="value">Value to insert.</param>
    /// <param name="comparison">Comparison operator to determine the order of elements.</param>
    public static void SortedInsert(this IList list, IComparable value, Comparison<IComparable> comparison)
    {
        comparison.ThrowIfNull(nameof(comparison));

        // If no elements exist in the list, add it.
        if (list.Count == 0)
        {
            list.Add(value);
            return;
        }

        int startIndex = 0;
        int endIndex = list.Count;
        while (endIndex > startIndex)
        {
            int windowSize = endIndex - startIndex;
            int middleIndex = startIndex + (windowSize / 2);
            IComparable middleValue = (IComparable)list[middleIndex];
            int compareToResult = comparison(middleValue, value);
            if (compareToResult == 0)
            {
                list.Insert(middleIndex, value);
                return;
            }
            else if (compareToResult < 0)
            {
                startIndex = middleIndex + 1;
            }
            else
            {
                endIndex = middleIndex;
            }
        }

        list.Insert(startIndex, value);
    }

    /// <summary>
    /// Inserts a set of values in the list, assuming it is already sorted, preserving the order of elements.
    /// </summary>
    /// <param name="list">The list in which to insert the values.</param>
    /// <param name="values">Values to insert.</param>
    /// <param name="comparison">Comparison operator to determine the order of elements.</param>
    public static void SortedInsert(this IList list, IEnumerable<IComparable> values,
        Comparison<IComparable> comparison)
    {
        values.ThrowIfNull(nameof(values));
        comparison.ThrowIfNull(nameof(comparison));

        foreach (IComparable value in values)
        {
            list.SortedInsert(value, comparison);
        }
    }

    /// <summary>
    /// Tries to find the index of the element that matches.
    /// </summary>
    /// <returns>True if an element is present that matches, false otherwise.</returns>
    public static bool TryFindIndex<TElement>(this TElement[] arr, Predicate<TElement> match, out int index)
    {
        arr.ThrowIfNull(nameof(arr));
        index = Array.FindIndex(arr, match);
        return index >= 0;
    }

    /// <summary>
    /// Tries to find the index of the element that matches.
    /// </summary>
    /// <returns>True if an element is present that matches, false otherwise.</returns>
    public static bool TryFindIndex<TElement>(this TElement[] arr, TElement match, out int index)
    {
        arr.ThrowIfNull(nameof(arr));
        for (int i = 0; i < arr.Length; ++i)
        {
            if (arr[i].Equals(match))
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    /// <summary>
    /// Tries to find the index of the element that matches.
    /// </summary>
    /// <returns>True if an element is present that matches, false otherwise.</returns>
    public static bool TryFindIndex<TElement>(this IReadOnlyList<TElement> l, Predicate<TElement> match, out int index)
    {
        l.ThrowIfNull(nameof(l));
        for (int i = 0; i < l.Count; ++i)
        {
            if (match(l[i]))
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    /// <summary>
    /// Tries to find the index of the element that matches.
    /// </summary>
    /// <returns>True if an element is present that matches, false otherwise.</returns>
    public static bool TryFindIndex<TElement>(this IReadOnlyList<TElement> l, TElement match, out int index)
    {
        l.ThrowIfNull(nameof(l));
        for (int i = 0; i < l.Count; ++i)
        {
            if (l[i].Equals(match))
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

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
    /// Pops random element from <paramref name="list"/>.
    /// </summary>
    /// <typeparam name="T">Source type.</typeparam>
    /// <param name="list">List with elements.</param>
    /// <returns>Tuple with popped element and it's index.</returns>
    public static (T element, int index) PopRandom<T>(this IList<T> list)
    {
        var index = UnityEngine.Random.Range(0, list.Count);
        return (list.Pop(index), index);
    }

    /// <summary>
    /// Pops random elements from list.
    /// </summary>
    /// <typeparam name="T">Source type.</typeparam>
    /// <param name="list">List with elements.</param>
    /// <param name="count">Count of elements to be popped.</param>
    /// <returns>List of tuples with popped elements and it's indexes.</returns>
    public static List<(T element, int index)> PopRandoms<T>(this IList<T> list, int count)
    {
        var popped = new List<(T element, int index)>();

        for (int i = 0; i < count; i++)
            popped.Add(list.PopRandom());

        return popped;
    }

    /// <summary>
    /// Pops random elements from list with specified probability.
    /// </summary>
    /// <typeparam name="T">Source type.</typeparam>
    /// <param name="list">List with elements.</param>
    /// <param name="probabilities">Probabilities, must match in count with enumerable.</param>
    /// <returns>Popped element.</returns>
    public static (T element, int index) PopRandomElementWithProbability<T>(this IList<T> list,
        params float[] probabilities)
    {
        return PopRandomElementWithProbability(list, (IEnumerable<float>)probabilities);
    }

    /// <summary>
    /// Pops random elements from list with specified probability.
    /// </summary>
    /// <typeparam name="T">Source type.</typeparam>
    /// <param name="list">List with elements.</param>
    /// <param name="probabilities">Probabilities, must match in count with enumerable.</param>
    /// <returns>Popped elements.</returns>
    public static (T element, int index) PopRandomElementWithProbability<T>(this IList<T> list,
        IEnumerable<float> probabilities)
    {
        var random = list.GetRandomElementWithProbability(probabilities);
        Pop(list, random.index);

        return random;
    }

    /// <summary>
    /// Pops random elements from list with specified probability selector.
    /// </summary>
    /// <typeparam name="T">Source type.</typeparam>
    /// <param name="list">List with elements.</param>
    /// <param name="probabilitiesSelector">Probabilities selector.</param>
    /// <returns>Popped elements.</returns>
    public static (T element, int index) PopRandomElementWithProbability<T>(this IList<T> list,
        Func<T, float> probabilitiesSelector)
    {
        var random = list.GetRandomElementWithProbability(probabilitiesSelector);
        Pop(list, random.index);

        return random;
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

    public static TValue RemoveFirst<TValue>(this List<TValue> @this)
    {
        var result = @this[0];
        @this.RemoveAt(0);
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

    public static T AtWrapped<T>(this IList<T> @this, int index)
    {
        return @this[WrapIndex(index, @this.Count)];
    }

    public static T AtWrappedOrDefault<T>(this IList<T> @this, int index, T defaultValue)
    {
        return @this.Count > 0 ? @this[WrapIndex(index, @this.Count)] : defaultValue;
    }

    /// <summary>Gets the wrapped-index item, or default(T) if the list is empty.</summary>
    public static T AtWrappedOrDefault<T>(this IList<T> @this, int index) => AtWrappedOrDefault(@this, index, default(T));

    public static void SetAtWrapped<T>(this IList<T> @this, int index, T value)
    {
        @this[WrapIndex(index, @this.Count)] = value;
    }

    public static int IndexOfOrDefault<T>(this IList<T> @this, T value, int defaultIndex)
    {
        int index = @this.IndexOf(value);
        return index != -1 ? index : defaultIndex;
    }

    public static void ClearAndDispose<T>(this IList<T> @this) where T : IDisposable
    {
        foreach (T item in @this)
            item.Dispose();
        @this.Clear();
    }

    public static void AddRangeUntyped(this IList @this, IEnumerable items)
    {
        foreach (object item in items)
            @this.Add(item);
    }

    public static void RemoveRangeUntyped(this IList @this, IEnumerable items)
    {
        foreach (object item in items)
            @this.Remove(item);
    }

    public static void ReplaceUntyped(this IList @this, IEnumerable items)
    {
        @this.Clear();
        @this.AddRangeUntyped(items);
    }

    /// <summary>
    /// 改变元素的索引位置
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="this">集合</param>
    /// <param name="item">元素</param>
    /// <param name="index">索引值</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static IList<T> ChangeIndex<T>(this IList<T> @this, T item, int index)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        ChangeIndexInternal(@this, item, index);
        return @this;
    }

    /// <summary>
    /// 改变元素的索引位置
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="this">集合</param>
    /// <param name="condition">元素定位条件</param>
    /// <param name="index">索引值</param>
    public static IList<T> ChangeIndex<T>(this IList<T> @this, Func<T, bool> condition, int index)
    {
        var item = @this.FirstOrDefault(condition);
        if (item != null)
        {
            ChangeIndexInternal(@this, item, index);
        }

        return @this;
    }

    private static void ChangeIndexInternal<T>(IList<T> list, T item, int index)
    {
        index = Math.Max(0, index);
        index = Math.Min(list.Count - 1, index);
        list.Remove(item);
        list.Insert(index, item);
    }

    private static int WrapIndex(int index, int count)
    {
        if (count == 0)
            throw new IndexOutOfRangeException();
        else if (index >= count)
            index = index % count;
        else if (index < 0)
        {
            index = index % count;
            if (index != 0)
                index += count;
        }

        return index;
    }

    /// <summary>
    /// Gets a random element from a List.
    /// </summary>
    /// <typeparam name="T">Type of the elements in the list.</typeparam>
    /// <param name="list">The list to get a random element from.</param>
    /// <returns>A random element from the list, or default(T) if the list is empty.</returns>
    public static T GetRandom<T>(this List<T> list)
    {
        return list.Count > 0 ? list[UnityEngine.Random.Range(0, list.Count)] : default(T);
    }
}}
