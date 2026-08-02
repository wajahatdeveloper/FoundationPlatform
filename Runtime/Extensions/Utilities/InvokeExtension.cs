using System;
using System.Collections;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class InvokeExtension
{
    public static void Invoke(this MonoBehaviour me, Action theDelegate, float time, bool realtime)
    {
        me.StartCoroutine(ExecuteAfterTime(theDelegate, time, realtime));
    }

    /// <summary>Invokes the delegate after time seconds, using scaled time.</summary>
    public static void Invoke(this MonoBehaviour me, Action theDelegate, float time) => Invoke(me, theDelegate, time, false);

    private static IEnumerator ExecuteAfterTime(Action theDelegate, float delay, bool realtime = false)
    {
        if (realtime)
        {
            yield return new WaitForSecondsRealtime(delay);
        }
        else
        {
            yield return new WaitForSeconds(delay);
        }

        theDelegate();
    }
}}
