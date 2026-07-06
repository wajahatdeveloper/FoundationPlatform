using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public static class StackQueueExtensions
{
    /// <summary>
    /// Pushes a range of items onto the stack
    /// </summary>
    public static void PushRange<T>(this Stack<T> stack, IEnumerable<T> items)
    {
        foreach (T item in items)
            stack.Push(item);
    }

    /// <summary>
    /// Enqueues a range of items into the queue
    /// </summary>
    public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> items)
    {
        foreach (T item in items)
            queue.Enqueue(item);
    }

    /// <summary>
    /// A NameValueCollection extension method that converts the @this to a dictionary.
    /// </summary>
    /// <param name="this">The @this to act on.</param>
    /// <returns>@this as an IDictionary&lt;string,object&gt;</returns>
    public static IDictionary<string, object> ToDictionary(this NameValueCollection @this)
    {
        var dict = new Dictionary<string, object>();

        if (@this != null)
        {
            foreach (string key in @this.AllKeys)
            {
                dict.Add(key, @this[key]);
            }
        }

        return dict;
    }

    /// <summary>
    /// Get a slice of an array as a new array.
    /// </summary>
    /// <param name="source">Source array from which the slice will be made.</param>
    /// <param name="start">Index from the original array from which to begin the slice.</param>
    /// <param name="count">Count of elements to copy from the array.</param>
    /// <returns></returns>
    public static T[] Slice<T>(this T[] source, int start, int count)
    {
        var array = new T[count];
        float limit = count + start;
        int c = 0;
        for (int i = start; i < limit; i++)
        {
            array[c] = source[i];
            c++;
        }

        return array;
    }
}

