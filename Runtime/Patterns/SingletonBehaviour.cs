using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.Logging;
using UnityEngine;

namespace AetherNexus.FoundationPlatform
{
// Non-generic host so a single RuntimeInitializeOnLoadMethod can reset every
// closed SingletonBehaviourCore<T> / Singleton<T> instantiation.
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

/// <summary>
///     Shared slot registry for <see cref="SingletonBehaviour{T}" /> and <see cref="PersistentSingletonBehaviour{T}" />.
///     An instance is resolvable only after its own <c>Awake</c> has registered it; there is no scene search.
/// </summary>
public abstract class SingletonBehaviourCore<T> : MonoBehaviour where T : MonoBehaviour
{
    // Keyed by concrete runtime type so subclasses that share the same closed
    // generic base (e.g. Dialog / InputDialog : Dialog) each get their own slot
    // instead of fighting over one shared static and destroying each other.
    private static readonly Dictionary<Type, T> instances = new();
    private static bool isQuitting;

    static SingletonBehaviourCore()
    {
        SingletonResetRegistry.Register(() =>
        {
            isQuitting = false;
            instances.Clear();
        });
    }

    /// <summary>
    ///     The registered instance. Throws when none has registered (none placed, or read before its
    ///     <c>Awake</c>); returns null only while the application is quitting.
    /// </summary>
    public static T Instance => GetInstance(typeof(T));

    public static bool HasInstance =>
        !isQuitting && instances.TryGetValue(typeof(T), out var instance) && instance != null;

    /// <summary>Quiet resolve for optional callers; false when nothing has registered.</summary>
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

        throw new InvalidOperationException(
            $"{type.Name}: no registered instance. It registers in its own Awake, so place one in the scene " +
            "and do not read Instance before that Awake has run. Use HasInstance / TryGetInstance for optional access.");
    }

    protected static bool TryGetInstance(Type type, out T instance)
    {
        instance = null;
        if (isQuitting) return false;
        return instances.TryGetValue(type, out instance) && instance != null;
    }

    /// <summary>Claims this concrete type's slot; false when another live instance already holds it.</summary>
    protected bool TryClaimSlot()
    {
        var type = GetType();
        if (instances.TryGetValue(type, out var existing) && existing != null && !ReferenceEquals(existing, this))
            return false;

        instances[type] = this as T;
        return true;
    }

    protected void ReleaseSlot()
    {
        var type = GetType();
        if (instances.TryGetValue(type, out var existing) && ReferenceEquals(existing, this))
            instances.Remove(type);
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }
}

public class SingletonBehaviour<T> : SingletonBehaviourCore<T> where T : MonoBehaviour
{
    protected virtual void Awake()
    {
        if (TryClaimSlot()) return;

        DebugX.Logger(LogChannels.DevTools).Info(
            "SingletonBehaviour<{TypeName}>: Newly loaded scene had a second copy; keeping the session survivor and destroying the duplicate.",
            GetType().Name);
        Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        ReleaseSlot();
    }
}

public class PersistentSingletonBehaviour<T> : SingletonBehaviourCore<T> where T : MonoBehaviour
{
    protected virtual void Awake()
    {
        // Persistence, the duplicate decision and the scene-boundary broadcast all come from the one
        // registry, so a persistent singleton behaves exactly like any other persistent object.
        if (!PersistentObjects.Register(gameObject, Scope, GetType().FullName)) { return; }

        TryClaimSlot();
    }

    /// <summary>
    ///     Lifetime this singleton claims. Defaults to <see cref="PersistenceScope.Application" /> — the
    ///     scope that carries music and other cross-scene services. Override for a session-lifetime one.
    /// </summary>
    protected virtual PersistenceScope Scope => PersistenceScope.Application;

    protected virtual void OnDestroy()
    {
        ReleaseSlot();
        PersistentObjects.Unregister(GetType().FullName, gameObject);
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
