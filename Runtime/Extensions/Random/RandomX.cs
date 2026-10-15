using System;
using System.Collections.Generic;
using System.Linq;
using AetherNexus.FoundationPlatform.Behaviours;

namespace AetherNexus.FoundationPlatform.Extensions
{
// The Unity-Random-shaped facade (value, Range, insideUnitSphere, rotation, State, named streams) lives
// in RandomX.Unity.cs. Kept partial so the collection helpers below and that surface share one name —
// gameplay code should never have to pick between two "random" types.
public static partial class RandomX
{
    public static void Shuffle<T>(this List<T> list, IRandomProvider rnd)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list));
        }
        if (rnd == null)
        {
            throw new ArgumentNullException(nameof(rnd));
        }

        T[] array = list.ToArray();
        ShuffleArray(array, rnd);
        list.Clear();
        list.AddRange(array);
    }

    public static void ShuffleArray<T>(T[] array, IRandomProvider rnd)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }
        if (rnd == null)
        {
            throw new ArgumentNullException(nameof(rnd));
        }

        if (array.Length <= 1)
        {
            return;
        }

        T[] source = new T[array.Length];
        Array.Copy(array, source, array.Length);
        for (int i = 1; i < array.Length; i++)
        {
            int indRnd = rnd.Range(0, i + 1);
            array[i] = array[indRnd];
            array[indRnd] = source[i];
        }
    }

    /// <summary>
    /// Return random item from array.
    /// </summary>
    public static T NextItem<T>(T[] array, IRandomProvider rnd)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }
        if (rnd == null)
        {
            throw new ArgumentNullException(nameof(rnd));
        }

        if (array.Length == 0)
        {
            throw new ArgumentException("array must not be empty.", nameof(array));
        }

        return array[rnd.Range(0, array.Length)];
    }

    /// <summary>
    /// Return random item from list.
    /// </summary>
    public static T NextItem<T>(List<T> list, IRandomProvider rnd)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list));
        }
        if (rnd == null)
        {
            throw new ArgumentNullException(nameof(rnd));
        }

        if (list.Count == 0)
        {
            throw new ArgumentException("list must not be empty.", nameof(list));
        }

        return list[rnd.Range(0, list.Count)];
    }

    /// <summary>
    /// Return list of random items from list.
    /// </summary>
    public static List<T> Take<T>(List<T> list, int count, IRandomProvider rnd)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list));
        }
        if (rnd == null)
        {
            throw new ArgumentNullException(nameof(rnd));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "count must be non-negative.");
        }

        if (count > list.Count)
        {
            throw new ArgumentException(
                "count (" + count + ") cannot exceed list.Count (" + list.Count + ").",
                nameof(count));
        }

        List<T> items = new List<T>();
        List<int> remainedIndexes = Enumerable.Range(0, list.Count).ToList();
        for (int i = 0; i < count; i++)
        {
            int selectedIndex = NextItem(remainedIndexes, rnd);
            remainedIndexes.Remove(selectedIndex);
            items.Add(list[selectedIndex]);
        }

        return items;
    }
}
}
