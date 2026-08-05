using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;

namespace AetherNexus.FoundationPlatform
{
 
using DebugX = DebugX.DebugX;

// Non-generic host so a single RuntimeInitializeOnLoadMethod can reset every
// closed SingletonBehaviour<T>/PersistentSingletonBehaviour<T> instantiation.
// Domain reload being off means static fields of already-used closed generics
// survive Stop->Play; each generic class registers its reset once (in its
// static ctor) and this registry replays all of them every SubsystemRegistration.
internal static class SingletonResetRegistry
{
    private static readonly List<Action> resetCallbacks = new();

    internal static void Register(Action reset)
    {
        resetCallbacks.Add(reset);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAll()
    {
        foreach (var reset in resetCallbacks)
            reset();
    }
}

public class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    // Keyed by concrete runtime type so subclasses that share the same closed
    // generic base (e.g. Dialog / InputDialog : Dialog) each get their own slot
    // instead of fighting over one shared static and destroying each other.
    private static readonly Dictionary<Type, T> instances = new();
    private static bool isQuitting;

    static SingletonBehaviour()
    {
        SingletonResetRegistry.Register(() =>
        {
            isQuitting = false;
            instances.Clear();
        });
    }

    public static T Instance => GetInstance(typeof(T));

    /// <summary>
    ///     Gets whether this singleton instance has been set.
    ///     Useful for checking initialization state.
    /// </summary>
    public static bool HasInstance =>
        !isQuitting && instances.TryGetValue(typeof(T), out var instance) && instance != null;

    protected virtual void Awake()
    {
        var type = GetType();

        if (!instances.TryGetValue(type, out var existing) || existing == null)
        {
            instances[type] = this as T;
        }
        else if (!ReferenceEquals(existing, this))
        {
            FoundationPlatform.DebugX.DebugX.Logger(LogChannels.DevTools).Info(
                "SingletonBehaviour<{TypeName}>: Newly loaded scene had a second copy; keeping the session survivor and destroying the duplicate.",
                type.Name);
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        var type = GetType();
        if (instances.TryGetValue(type, out var existing) && ReferenceEquals(existing, this)) instances.Remove(type);
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    /// <summary>
    ///     Quiet resolve — no error log when absent. Use for optional callers
    ///     (player-loop probes, non-deterministic fallbacks, editor/pre-init).
    ///     Prefer <see cref="HasInstance" /> when you only need a registered slot check
    ///     and must avoid a scene search.
    /// </summary>
    public static bool TryGetInstance(out T instance)
    {
        return TryGetInstance(typeof(T), out instance);
    }

    /// <summary>
    ///     Resolves the singleton for a specific concrete type. Subclasses that share
    ///     this base should shadow <c>Instance</c> with <c>public static new TSelf Instance</c>
    ///     forwarding here with their own type, so the accessor targets their slot
    ///     rather than the base <c>typeof(T)</c>.
    /// </summary>
    protected static T GetInstance(Type type)
    {
        if (isQuitting) return null;

        if (TryGetInstance(type, out var instance))
            return instance;

        FoundationPlatform.DebugX.DebugX.Logger(LogChannels.DevTools)
            .Error(
                "SingletonBehaviour<{TypeName}>: Instance not found, this is likely due to it being non-existent in the scene.",
                type.Name);
        return null;
    }

    protected static bool TryGetInstance(Type type, out T instance)
    {
        instance = null;
        if (isQuitting) return false;

        if (instances.TryGetValue(type, out instance) && instance != null)
            return true;

        instance = FindFirstObjectByType(type) as T;
        if (instance == null)
            return false;

        instances[type] = instance;
        return true;
    }
}

public class PersistentSingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    // Keyed by concrete runtime type — see SingletonBehaviour<T> for the rationale.
    private static readonly Dictionary<Type, T> instances = new();
    private static bool isQuitting;

    static PersistentSingletonBehaviour()
    {
        SingletonResetRegistry.Register(() =>
        {
            isQuitting = false;
            instances.Clear();
        });
    }

    public static T Instance => GetInstance(typeof(T));

    /// <summary>
    ///     Gets whether this singleton instance has been set.
    ///     Useful for checking initialization state.
    /// </summary>
    public static bool HasInstance =>
        !isQuitting && instances.TryGetValue(typeof(T), out var instance) && instance != null;

    protected virtual void Awake()
    {
        var type = GetType();

        if (!instances.TryGetValue(type, out var existing) || existing == null)
        {
            instances[type] = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (!ReferenceEquals(existing, this))
        {
            FoundationPlatform.DebugX.DebugX.Logger(LogChannels.DevTools).Info(
                "PersistentSingletonBehaviour<{TypeName}>: Newly loaded scene had a second copy; keeping the session survivor and destroying the duplicate.",
                type.Name);
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        var type = GetType();
        if (instances.TryGetValue(type, out var existing) && ReferenceEquals(existing, this)) instances.Remove(type);
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    /// <summary>
    ///     Quiet resolve — no error log when absent. Use for optional callers
    ///     (player-loop probes, non-deterministic fallbacks, editor/pre-init).
    ///     Prefer <see cref="HasInstance" /> when you only need a registered slot check
    ///     and must avoid a scene search.
    /// </summary>
    public static bool TryGetInstance(out T instance)
    {
        return TryGetInstance(typeof(T), out instance);
    }

    /// <summary>
    ///     Resolves the singleton for a specific concrete type. Subclasses that share
    ///     this base should shadow <c>Instance</c> with <c>public static new TSelf Instance</c>
    ///     forwarding here with their own type.
    /// </summary>
    protected static T GetInstance(Type type)
    {
        if (isQuitting) return null;

        if (TryGetInstance(type, out var instance))
            return instance;

        FoundationPlatform.DebugX.DebugX.Logger(LogChannels.DevTools).Error(
            "PersistentSingletonBehaviour<{TypeName}>: Instance not found, this is likely due to it being non-existent in the scene.",
            type.Name);
        return null;
    }

    protected static bool TryGetInstance(Type type, out T instance)
    {
        instance = null;
        if (isQuitting) return false;

        if (instances.TryGetValue(type, out instance) && instance != null)
            return true;

        instance = FindFirstObjectByType(type) as T;
        if (instance == null)
            return false;

        instances[type] = instance;
        return true;
    }
}

public class Singleton<T> where T : new()
{
    private static T instance;
    private static readonly object sync = new();

    static Singleton()
    {
        SingletonResetRegistry.Register(() => instance = default);
    }

    public static T Instance
    {
        get
        {
            if (instance != null) return instance;

            lock (sync)
            {
                if (instance == null) instance = new T();
            }

            return instance;
        }
    }
}   
}