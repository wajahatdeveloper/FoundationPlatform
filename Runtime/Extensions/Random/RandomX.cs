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

    public static T NextEnum<T>(IRandomProvider rnd)
    {
        if (rnd == null)
        {
            throw new ArgumentNullException(nameof(rnd));
        }

        var values = Enum.GetValues(typeof(T));
        if (values.Length == 0)
        {
            throw new InvalidOperationException(
                "NextEnum<" + typeof(T).Name + ">: enum has no defined values.");
        }

        return (T)values.GetValue(rnd.Range(0, values.Length));
    }

    public static float NextDiscrete(float min, float max, int count, IRandomProvider rnd)
    {
        if (rnd == null)
        {
            throw new ArgumentNullException(nameof(rnd));
        }
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "count must be non-negative.");
        }

        if (count < 2)
        {
            return min;
        }

        if (max < min)
        {
            throw new ArgumentException(
                "NextDiscrete requires max >= min; got min=" + min + ", max=" + max + ".");
        }

        return min + rnd.Range(0, count) * (max - min) / (count - 1);
    }

    public static int NextWeightedInd(int[] weights, IRandomProvider rnd)
    {
        if (weights == null)
        {
            throw new ArgumentNullException(nameof(weights));
        }

        return NextWeightedInd(weights.Select(i => (float)i).ToArray(), rnd);
    }

    public static int NextWeightedInd(float[] weights, IRandomProvider rnd)
    {
        if (weights == null)
        {
            throw new ArgumentNullException(nameof(weights));
        }
        if (rnd == null)
        {
            throw new ArgumentNullException(nameof(rnd));
        }

        if (weights.Length == 0)
        {
            throw new ArgumentException("weights must not be empty.", nameof(weights));
        }

        float total = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] < 0f)
            {
                throw new ArgumentException(
                    "weights[" + i + "] is negative (" + weights[i] + "); all weights must be non-negative.",
                    nameof(weights));
            }

            total += weights[i];
        }

        if (total <= 0f)
        {
            throw new ArgumentException(
                "Sum of weights must be positive; got total=" + total + ".",
                nameof(weights));
        }

        float random = rnd.Range(0f, total);
        float sum = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            sum += weights[i];
            if (random <= sum)
            {
                return i;
            }
        }

        return weights.Length - 1;
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

    /// <summary>
    /// Return random bool value.
    /// </summary>
    public static bool NextBool(IRandomProvider rnd)
    {
        if (rnd == null)
        {
            throw new ArgumentNullException(nameof(rnd));
        }

        return rnd.Range(0, 2) == 0;
    }
}
}
