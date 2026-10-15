using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class UnityEventExtensions
{
    /// <summary>
    /// Adds a listener that executes only once to the UnityEvent.
    /// </summary>
    public static UnityEvent Once(this UnityEvent source, UnityAction action)
    {
        UnityAction wrapperAction = null;
        wrapperAction = () =>
        {
            source.RemoveListener(wrapperAction);
            action();
        };
        source.AddListener(wrapperAction);
        return source;
    }

    /// <summary>
    /// Adds a listener that executes only once to the UnityEvent.
    /// </summary>
    public static UnityEvent<T> Once<T>(this UnityEvent<T> source,
        UnityAction<T> action)
    {
        UnityAction<T> wrapperAction = null;
        wrapperAction = p =>
        {
            source.RemoveListener(wrapperAction);
            action(p);
        };
        source.AddListener(wrapperAction);
        return source;
    }

    /// <summary>
    /// Adds a listener that executes only once to the UnityEvent.
    /// </summary>
    public static UnityEvent<T0, T1> Once<T0, T1>(
        this UnityEvent<T0, T1> source,
        UnityAction<T0, T1> action)
    {
        UnityAction<T0, T1> wrapperAction = null;
        wrapperAction = (p0, p1) =>
        {
            source.RemoveListener(wrapperAction);
            action(p0, p1);
        };
        source.AddListener(wrapperAction);
        return source;
    }

    /// <summary>
    /// Adds a listener that executes only once to the UnityEvent.
    /// </summary>
    public static UnityEvent<T0, T1, T2> Once<T0, T1, T2>(
        this UnityEvent<T0, T1, T2> source,
        UnityAction<T0, T1, T2> action)
    {
        UnityAction<T0, T1, T2> wrapperAction = null;
        wrapperAction = (p0, p1, p2) =>
        {
            source.RemoveListener(wrapperAction);
            action(p0, p1, p2);
        };
        source.AddListener(wrapperAction);
        return source;
    }

    /// <summary>
    /// Adds a listener that executes only once to the UnityEvent.
    /// </summary>
    public static UnityEvent<T0, T1, T2, T3> Once<T0, T1, T2, T3>(
        this UnityEvent<T0, T1, T2, T3> source,
        UnityAction<T0, T1, T2, T3> action)
    {
        UnityAction<T0, T1, T2, T3> wrapperAction = null;
        wrapperAction = (p0, p1, p2, p3) =>
        {
            source.RemoveListener(wrapperAction);
            action(p0, p1, p2, p3);
        };
        source.AddListener(wrapperAction);
        return source;
    }
}}
