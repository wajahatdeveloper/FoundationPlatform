using System;
using System.Collections.Generic;
using UnityEngine;

public class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    // Keyed by concrete runtime type so subclasses that share the same closed
    // generic base (e.g. Dialog / InputDialog : Dialog) each get their own slot
    // instead of fighting over one shared static and destroying each other.
    private static readonly Dictionary<Type, T> instances = new Dictionary<Type, T>();
    private static bool isQuitting;

    public static T Instance => GetInstance(typeof(T));

    /// <summary>
    /// Resolves the singleton for a specific concrete type. Subclasses that share
    /// this base should shadow <c>Instance</c> with <c>public static new TSelf Instance</c>
    /// forwarding here with their own type, so the accessor targets their slot
    /// rather than the base <c>typeof(T)</c>.
    /// </summary>
    protected static T GetInstance(Type type)
    {
        if (isQuitting) return null;

        if (!instances.TryGetValue(type, out T instance) || instance == null)
        {
            instance = UnityEngine.Object.FindFirstObjectByType(type) as T;

            if (instance == null)
            {
                DebugX.Logger(LogChannels.DevTools).Error("SingletonBehaviour<{TypeName}>: Instance not found, this is likely due to it being non-existent in the scene.", type.Name);
            }
            else
            {
                instances[type] = instance;
            }
        }

        return instance;
    }

    /// <summary>
    ///  Gets whether this singleton instance has been set.
    ///  Useful for checking initialization state.
    /// </summary>
    public static bool HasInstance => !isQuitting && instances.TryGetValue(typeof(T), out T instance) && instance != null;

    protected virtual void Awake()
    {
        Type type = GetType();

        if (!instances.TryGetValue(type, out T existing) || existing == null)
        {
            instances[type] = this as T;
        }
        else if (!ReferenceEquals(existing, this))
        {
            DebugX.Logger(LogChannels.DevTools).Error("SingletonBehaviour<{TypeName}>: Duplicate instance detected. Destroying duplicate.", type.Name);
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        Type type = GetType();
        if (instances.TryGetValue(type, out T existing) && ReferenceEquals(existing, this))
        {
            instances.Remove(type);
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }
}

public class PersistentSingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    // Keyed by concrete runtime type — see SingletonBehaviour<T> for the rationale.
    private static readonly Dictionary<Type, T> instances = new Dictionary<Type, T>();
    private static bool isQuitting;

    public static T Instance => GetInstance(typeof(T));

    /// <summary>
    /// Resolves the singleton for a specific concrete type. Subclasses that share
    /// this base should shadow <c>Instance</c> with <c>public static new TSelf Instance</c>
    /// forwarding here with their own type.
    /// </summary>
    protected static T GetInstance(Type type)
    {
        if (isQuitting) return null;

        if (!instances.TryGetValue(type, out T instance) || instance == null)
        {
            instance = UnityEngine.Object.FindFirstObjectByType(type) as T;

            if (instance == null)
            {
                DebugX.Logger(LogChannels.DevTools).Error("PersistentSingletonBehaviour<{TypeName}>: Instance not found, this is likely due to it being non-existent in the scene.", type.Name);
            }
            else
            {
                instances[type] = instance;
            }
        }

        return instance;
    }

    /// <summary>
    ///  Gets whether this singleton instance has been set.
    ///  Useful for checking initialization state.
    /// </summary>
    public static bool HasInstance => !isQuitting && instances.TryGetValue(typeof(T), out T instance) && instance != null;

    protected virtual void Awake()
    {
        Type type = GetType();

        if (!instances.TryGetValue(type, out T existing) || existing == null)
        {
            instances[type] = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (!ReferenceEquals(existing, this))
        {
            DebugX.Logger(LogChannels.DevTools).Error("PersistentSingletonBehaviour<{TypeName}>: Duplicate instance detected. Destroying duplicate.", type.Name);
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        Type type = GetType();
        if (instances.TryGetValue(type, out T existing) && ReferenceEquals(existing, this))
        {
            instances.Remove(type);
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }
}

public class Singleton<T> where T : new()
{
    private static T instance;
    private static readonly object sync = new object();

    public static T Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            lock (sync)
            {
                if (instance == null)
                {
                    instance = new T();
                }
            }
            return instance;
        }
    }
}