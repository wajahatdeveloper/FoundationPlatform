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

    /// <summary>
    /// Invokes the action if it is not null.
    /// </summary>
    public static void InvokeIfNotNull(this UnityAction del)
    {
        if (del != null)
        {
            del.Invoke();
        }
    }

    /// <summary>
    /// Invokes the action if it is not null.
    /// </summary>
    public static void InvokeIfNotNull<T1>(this UnityAction<T1> del, T1 p1)
    {
        if (del != null)
        {
            del.Invoke(p1);
        }
    }

    /// <summary>
    /// Invokes the action if it is not null.
    /// </summary>
    public static void InvokeIfNotNull<T1, T2>(this UnityAction<T1, T2> del, T1 p1, T2 p2)
    {
        if (del != null)
        {
            del.Invoke(p1, p2);
        }
    }

    /// <summary>
    /// Invokes the action if it is not null.
    /// </summary>
    public static void InvokeIfNotNull<T1, T2, T3>(this UnityAction<T1, T2, T3> del, T1 p1, T2 p2, T3 p3)
    {
        if (del != null)
        {
            del.Invoke(p1, p2, p3);
        }
    }

    /// <summary>
    /// Invokes the action if it is not null.
    /// </summary>
    public static void InvokeIfNotNull<T1, T2, T3, T4>(this UnityAction<T1, T2, T3, T4> del, T1 p1, T2 p2, T3 p3, T4 p4)
    {
        if (del != null)
        {
            del.Invoke(p1, p2, p3, p4);
        }
    }

    /// <summary>
    /// Invokes the event if it is not null.
    /// </summary>
    public static void InvokeIfNotNull(this UnityEvent del)
    {
        if (del != null)
        {
            del.Invoke();
        }
    }

    /// <summary>
    /// Invokes the event if it is not null.
    /// </summary>
    public static void InvokeIfNotNull<T1>(this UnityEvent<T1> del, T1 p1)
    {
        if (del != null)
        {
            del.Invoke(p1);
        }
    }

    /// <summary>
    /// Invokes the event if it is not null.
    /// </summary>
    public static void InvokeIfNotNull<T1, T2>(this UnityEvent<T1, T2> del, T1 p1, T2 p2)
    {
        if (del != null)
        {
            del.Invoke(p1, p2);
        }
    }

    /// <summary>
    /// Invokes the event if it is not null.
    /// </summary>
    public static void InvokeIfNotNull<T1, T2, T3>(this UnityEvent<T1, T2, T3> del, T1 p1, T2 p2, T3 p3)
    {
        if (del != null)
        {
            del.Invoke(p1, p2, p3);
        }
    }

    /// <summary>
    /// Invokes the event if it is not null.
    /// </summary>
    public static void InvokeIfNotNull<T1, T2, T3, T4>(this UnityEvent<T1, T2, T3, T4> del, T1 p1, T2 p2, T3 p3, T4 p4)
    {
        if (del != null)
        {
            del.Invoke(p1, p2, p3, p4);
        }
    }
}}
