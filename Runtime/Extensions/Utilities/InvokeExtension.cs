using System;
using System.Collections;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class InvokeExtension
{
    public static void Invoke(this MonoBehaviour me, Action theDelegate, float time, bool realtime = false)
    {
        me.StartCoroutine(ExecuteAfterTime(theDelegate, time, realtime));
    }

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
