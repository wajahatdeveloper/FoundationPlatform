using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AetherNexus.FoundationPlatform.Attributes;
using AetherNexus.FoundationPlatform.Behaviours;
using UnityRandom = UnityEngine.Random;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class IEnumerableExtensions
{
    public static IEnumerable<T> Concat<T>(params IEnumerable<T>[] enumerables)
    {
        return enumerables.SelectMany(e => e);
    }

    public static IEnumerable<T> Concat<T>(this IEnumerable<T> @this, params T[] values)
    {
        foreach (T item in @this)
            yield return item;
        foreach (T value in values)
            yield return value;
    }

    public static IEnumerable<T> Concat<T>(this IEnumerable<T> @this, T value)
    {
        foreach (T item in @this)
            yield return item;
        yield return value;
    }

    public static int IndexOf<T>(this IEnumerable<T> @this, T value, IEqualityComparer<T> comparer)
    {
        if (comparer == null)
            comparer = EqualityComparer<T>.Default;
        int i = 0;
        foreach (T item in @this)
        {
            if (comparer.Equals(item, value))
                return i;
            i++;
        }

        return -1;
    }

    public static int IndexOf<T>(this IEnumerable<T> @this, Func<T, bool> condition)
    {
        int i = 0;
        foreach (T item in @this)
        {
            if (condition(item))
                return i;
            i++;
        }

        return -1;
    }

    public static int LastIndexOf<T>(this IEnumerable<T> @this, T value, IEqualityComparer<T> comparer)
    {
        if (comparer == null)
            comparer = EqualityComparer<T>.Default;
        int i = 0, index = -1;
        foreach (T item in @this)
        {
            if (comparer.Equals(item, value))
                index = i;
            i++;
        }

        return index;
    }

    public static int LastIndexOf<T>(this IEnumerable<T> @this, Func<T, bool> condition)
    {
        int i = 0, index = -1;
        foreach (T item in @this)
        {
            if (condition(item))
                index = i;
            i++;
        }

        return index;
    }

    public static IEnumerable<T> Inverse<T>(this IEnumerable<T> @this)
    {
        var list = @this as IList<T>;
        if (list != null)
        {
            for (int i = list.Count - 1; i >= 0; i--)
                yield return list[i];
        }
        else
        {
            foreach (T item in @this.Reverse())
                yield return item;
        }
    }

    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> @this, int size)
    {
        T[] bucket = null;
        int count = 0;
        foreach (T item in @this)
        {
            if (bucket == null)
                bucket = new T[size];
            bucket[count++] = item;
            if (count != size)
                continue;
            yield return bucket;
            bucket = null;
            count = 0;
        }

        if (bucket != null && count > 0)
            yield return bucket.Take(count);
    }

    // Requires an explicit randomness source (no shared static System.Random): simulation callers pass
    // DeterministicRandomProvider.Instance, presentation callers pass UnityRandomProvider.Create().
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> @this, IRandomProvider rnd)
    {
        if (rnd == null) throw new ArgumentNullException(nameof(rnd));
        return @this.OrderBy(_ => rnd.Range(0f, 1f));
    }

    public static Dictionary<TKey, T> ToDictionary<TKey, T>(this IEnumerable<KeyValuePair<TKey, T>> @this)
    {
        return @this.ToDictionary(p => p.Key, p => p.Value);
    }

    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> @this, IEqualityComparer<T> comparer)
    {
        return new HashSet<T>(@this, comparer);
    }

    public static HashSet<TKey> ToHashSet<TKey, T>(this IEnumerable<T> @this, Func<T, TKey> keySelector)
    {
        return new HashSet<TKey>(@this.Select(keySelector));
    }

    public static HashSet<TKey> ToHashSet<TKey, T>(this IEnumerable<T> @this, Func<T, TKey> keySelector,
        IEqualityComparer<TKey> comparer)
    {
        return new HashSet<TKey>(@this.Select(keySelector), comparer);
    }

    /// <summary>
    ///     An IEnumerable&lt;T&gt; extension method that queries if a not is empty.
    /// </summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// <param name="this">The collection to act on.</param>
    /// <returns>true if a not is t>, false if not.</returns>
    public static bool IsNotEmpty<T>(this IEnumerable<T> @this)
    {
        return @this.Any();
    }

    /// <summary>
    ///     Concatenates all the elements of a IEnumerable, using the specified separator between each element.
    /// </summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// <param name="this">An IEnumerable that contains the elements to concatenate.</param>
    /// <param name="separator">
    ///     The string to use as a separator. separator is included in the returned string only if
    ///     value has more than one element.
    /// </param>
    /// <returns>
    ///     A string that consists of the elements in value delimited by the separator string. If value is an empty array,
    ///     the method returns String.Empty.
    /// </returns>
    public static string StringJoin<T>(this IEnumerable<T> @this, string separator)
    {
        return string.Join(separator, @this);
    }

    /// <summary>
    ///     Concatenates all the elements of a IEnumerable, using the specified separator between
    ///     each element.
    /// </summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// <param name="this">The @this to act on.</param>
    /// <param name="separator">
    ///     The string to use as a separator. separator is included in the
    ///     returned string only if value has more than one element.
    /// </param>
    /// <returns>
    ///     A string that consists of the elements in value delimited by the separator string. If
    ///     value is an empty array, the method returns String.Empty.
    /// </returns>
    public static string StringJoin<T>(this IEnumerable<T> @this, char separator)
    {
        return string.Join(separator.ToString(), @this);
    }

    /// <summary>
    /// Excepts passed elements from <paramref name="enumerable"/>.
    /// </summary>
    /// <typeparam name="T">Source type.</typeparam>
    /// <param name="enumerable">The enumerable.</param>
    /// <param name="elements">Elements to exclude.</param>
    /// <returns>Enumerable without passed elements.</returns>
    public static IEnumerable<T> Except<T>(this IEnumerable<T> enumerable, params T[] elements) =>
        enumerable.Except((IEnumerable<T>)elements);

    /// <summary>
    /// Shuffles <paramref name="enumerable"/>.
    /// PRESENTATION-ONLY: uses UnityEngine.Random and is NOT deterministic. Simulation/validation
    /// code must use the <see cref="IRandomProvider"/> overload to honor the determinism contract.
    /// </summary>
    /// <typeparam name="T">Source type.</typeparam>
    /// <param name="enumerable">The enumerable.</param>
    /// <returns>Shuffled <paramref name="enumerable"/>.</returns>
    [PresentationRandom]
    public static IEnumerable<T> Shuffled<T>(this IEnumerable<T> enumerable) =>
        enumerable.OrderBy(v => UnityRandom.value);

    /// <summary>
    /// Shuffles <paramref name="enumerable"/> using an explicit randomness source.
    /// Pass a deterministic provider from simulation code to stay in sync.
    /// </summary>
    public static IEnumerable<T> Shuffled<T>(this IEnumerable<T> enumerable, IRandomProvider rnd)
    {
        if (rnd == null) throw new ArgumentNullException(nameof(rnd));
        return enumerable.OrderBy(_ => rnd.Range(0f, 1f));
    }

    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enumerable, Action<T, int> action)
    {
        int index = 0;
        foreach (T item in enumerable)
        {
            action(item, index++);

            yield return item;
        }
    }

    public static IEnumerable<T> Except<T>(this IEnumerable<T> enumerable, T item)
    {
        return enumerable.Except(Enumerable.Repeat(item, 1));
    }

    public static IEnumerable<T> Reverse<T>(this IEnumerable<T> enumerable)
    {
        T[] array = enumerable.ToArray();

        for (int i = array.Length - 1; i >= 0; i--)
            yield return array[i];
    }

    /// <summary>
    /// PRESENTATION-ONLY: uses UnityEngine.Random. Use the <see cref="IRandomProvider"/> overload in simulation code.
    /// </summary>
    [PresentationRandom]
    public static T Random<T>(this IEnumerable<T> enumerable)
    {
        T[] array = enumerable.ToArray();

        return array[UnityEngine.Random.Range(0, array.Length)];
    }

    /// <summary>
    /// Returns a uniformly random element using an explicit randomness source.
    /// </summary>
    public static T Random<T>(this IEnumerable<T> enumerable, IRandomProvider rnd)
    {
        if (rnd == null) throw new ArgumentNullException(nameof(rnd));
        T[] array = enumerable.ToArray();

        return array[rnd.Range(0, array.Length)];
    }

    /// <summary>
    /// PRESENTATION-ONLY: uses UnityEngine.Random. Use the <see cref="IRandomProvider"/> overload in simulation code.
    /// </summary>
    [PresentationRandom]
    public static T Random<T>(this IEnumerable<T> enumerable, Func<T, float> chanceSelector)
    {
        T[] orderedArray = enumerable.OrderByDescending(chanceSelector).ToArray();

        float[] chances = orderedArray.Select(chanceSelector).ToArray();
        float totalChance = chances.Sum();
        float chance = UnityEngine.Random.Range(0, totalChance);

        int index = 0;
        for (int i = 0; i < chances.Length; i++)
        {
            if (chance <= chances[i])
            {
                index = i;

                break;
            }
            else chance -= chances[i];
        }

        return orderedArray[index];
    }

    /// <summary>
    /// Weighted-random element selection using an explicit randomness source.
    /// </summary>
    public static T Random<T>(this IEnumerable<T> enumerable, Func<T, float> chanceSelector, IRandomProvider rnd)
    {
        if (rnd == null) throw new ArgumentNullException(nameof(rnd));
        T[] orderedArray = enumerable.OrderByDescending(chanceSelector).ToArray();

        float[] chances = orderedArray.Select(chanceSelector).ToArray();
        float totalChance = chances.Sum();
        float chance = rnd.Range(0f, totalChance);

        int index = 0;
        for (int i = 0; i < chances.Length; i++)
        {
            if (chance <= chances[i])
            {
                index = i;

                break;
            }
            else chance -= chances[i];
        }

        return orderedArray[index];
    }

    /// <summary>
    /// PRESENTATION-ONLY: uses UnityEngine.Random. Use the <see cref="IRandomProvider"/> overload in simulation code.
    /// </summary>
    public static IEnumerable<T> Random<T>(this IEnumerable<T> enumerable, int amount)
    {
        return Shuffle(enumerable).Take(amount);
    }

    /// <summary>
    /// Returns <paramref name="amount"/> random elements using an explicit randomness source.
    /// </summary>
    public static IEnumerable<T> Random<T>(this IEnumerable<T> enumerable, int amount, IRandomProvider rnd)
    {
        return Shuffled(enumerable, rnd).Take(amount);
    }

    /// <summary>
    /// PRESENTATION-ONLY: uses UnityEngine.Random. Use <see cref="Shuffle{T}(IEnumerable{T}, IRandomProvider)"/> in simulation code.
    /// </summary>
    [PresentationRandom]
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> enumerable)
    {
        return enumerable.OrderBy(item => UnityEngine.Random.value);
    }

    public static bool IsEmpty<T>(this IEnumerable<T> enumerable)
    {
        return !enumerable.Any();
    }

    public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
    {
        return enumerable == null || enumerable.IsEmpty();
    }

#region UnityConvertAll
#endregion


    /// <summary>
    /// Turns a finite sequence into a circular one, or equivalently,
    /// repeats the original sequence indefinitely.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to cycle through.</param>
    /// <returns>An infinite sequence cycling through the given sequence.</returns>
    public static IEnumerable<TSource> Cycle<TSource>(this IEnumerable<TSource> source)
    {
        ThrowIf.Argument.IsNull(source, "source");

        return CycleIterator(source);
    }

    private static IEnumerable<TSource> CycleIterator<TSource>(IEnumerable<TSource> source)
    {
        var collection = source as ICollection<TSource>;

        var elementBuffer = collection == null
            ? new List<TSource>()
            : new List<TSource>(collection.Count);

        foreach (TSource element in source)
        {
            yield return element;

            // We add this element to a local element buffer so that
            // we don't have to enumerate the sequence multiple times
            elementBuffer.Add(element);
        }

        if (elementBuffer.IsEmpty())
        {
            // If the element buffer is empty, so was the source sequence.
            // In this case, we can stop here and simply return an empty sequence.
            yield break;
        }

        int index = 0;
        while (true)
        {
            yield return elementBuffer[index];
            index = (index + 1) % elementBuffer.Count;
        }
    }


    /// <summary>
    /// Splits the given sequence into chunks of the given size.
    /// If the sequence length isn't evenly divisible by the chunk size,
    /// the last chunk will contain all remaining elements.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">The sequence.</param>
    /// <param name="chunkSize">The number of elements per chunk.</param>
    /// <returns>The chunked sequence.</returns>
    public static IEnumerable<TSource[]> Chunk<TSource>(this IEnumerable<TSource> source, int chunkSize)
    {
        ThrowIf.Argument.IsNull(source, "source");
        ThrowIf.Argument.IsZeroOrNegative(chunkSize, "chunkSize");

        return ChunkIterator(source, chunkSize);
    }

    private static IEnumerable<TSource[]> ChunkIterator<TSource>(IEnumerable<TSource> source, int chunkSize)
    {
        TSource[] currentChunk = null;
        int currentIndex = 0;

        foreach (var element in source)
        {
            currentChunk = currentChunk ?? new TSource[chunkSize];
            currentChunk[currentIndex++] = element;

            if (currentIndex == chunkSize)
            {
                yield return currentChunk;
                currentIndex = 0;
                currentChunk = null;
            }
        }

        // Do we have an incomplete chunk of remaining elements?
        if (currentChunk != null)
        {
            // This last chunk is incomplete, otherwise it would've been returned already.
            // Thus, we have to create a new, shorter array to hold the remaining elements.
            var lastChunk = new TSource[currentIndex];
            Array.Copy(currentChunk, lastChunk, currentIndex);

            yield return lastChunk;
        }
    }

    public static T[] RemoveRange<T>(this T[] array, int index, int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException("count", " is out of range");
        if (index < 0 || index > array.Length - 1)
            throw new ArgumentOutOfRangeException("index", " is out of range");

        if (array.Length - count - index < 0)
            throw new ArgumentException("index and count do not denote a valid range of elements in the array", "");

        var newArray = new T[array.Length - count];

        for (int i = 0, ni = 0; i < array.Length; i++)
        {
            if (i < index || i >= index + count)
            {
                newArray[ni] = array[i];
                ni++;
            }
        }

        return newArray;
    }
}

internal static class ThrowIf
{
    public static class Argument
    {
        public static void IsNull(object argument, string argumentName)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        public static void IsNegative(int argument, string argumentName)
        {
            if (argument < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentName + " must not be negative.");
            }
        }

        public static void IsZeroOrNegative(int argument, string argumentName)
        {
            if (argument <= 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentName + " must be positive.");
            }
        }
    }
}}
