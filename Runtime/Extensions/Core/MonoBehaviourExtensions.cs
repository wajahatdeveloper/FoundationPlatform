using System;
using System.Runtime.CompilerServices;
using AetherNexus.FoundationPlatform.SupportTypes;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class MonoBehaviourExtensions
{
    public static void RunOnce(this MonoBehaviour behaviour, Action action)
    {
        var key = GetActionKey(action);
        var customState = behaviour.gameObject.AddOrGetComponent<CustomState>();
        customState.keyValuePairs.TryGetValue(key, out var value);
        if (value != null)
        {
            return;
        }
        else
        {
            customState.keyValuePairs[key] = "1";
            action();
        }
    }

    public static void RunOncePersistent(this MonoBehaviour behaviour, Action action)
    {
        var key = GetPersistentActionKey(action);
        if (PlayerPrefs.GetString(key, "") != "")
        {
            return;
        }
        else
        {
            PlayerPrefs.SetString(key, "1");
            action();
        }
    }

    /// <summary>
    /// Build a stable, collision-resistant dedup key from a delegate's method and target
    /// identity. Avoids keying on Action.ToString(), which returns only the declaring
    /// type + method name and therefore collides across instances and across distinct
    /// closures over the same method.
    /// </summary>
    private static string GetActionKey(Action action)
    {
        var method = action.Method;
        var declaringType = method.DeclaringType != null ? method.DeclaringType.FullName : "<global>";
        var target = action.Target;
        var targetId = target != null ? RuntimeHelpers.GetHashCode(target).ToString() : "static";
        return declaringType + "." + method.Name + "#" + method.MetadataToken + "@" + targetId;
    }

    /// <summary>
    /// Build a dedup key for the PERSISTENT (PlayerPrefs) variant. Keys on method identity
    /// only (declaring type + name + metadata token) and deliberately omits the target's
    /// runtime hash: RuntimeHelpers.GetHashCode is a per-process object identity that is not
    /// stable across app restarts, so including it would make the persisted key differ every
    /// launch — the action would re-run on each start and leak an unbounded number of
    /// PlayerPrefs keys. Persistent "run once" is therefore method-scoped (once ever), not
    /// instance-scoped, which is the correct granularity for state that survives restarts.
    /// </summary>
    private static string GetPersistentActionKey(Action action)
    {
        var method = action.Method;
        var declaringType = method.DeclaringType != null ? method.DeclaringType.FullName : "<global>";
        return "persist:" + declaringType + "." + method.Name + "#" + method.MetadataToken;
    }

    /// <summary>
    /// disable the specified behaviour if the assert value is false, and throw a warning
    /// </summary>
    /// <param name="behaviour"></param>
    /// <param name="assertValue"></param>
    /// <param name="message"></param>
    public static void Assert(this MonoBehaviour behaviour, bool assertValue, string message = "")
    {
        if (!assertValue)
        {
            Debug.LogWarning("Assert failed. " + message);
            behaviour.enabled = false;
        }
    }
}}
