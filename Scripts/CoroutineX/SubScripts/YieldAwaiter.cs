using System.Collections;
using UnityEngine;

/// <summary>
/// Base class for awaiting some events on Coroutines.
/// </summary>
public abstract class YieldAwaiter : YieldInstruction, IEnumerator
{
    object IEnumerator.Current => null;

    bool IEnumerator.MoveNext() => KeepWaiting;

    void IEnumerator.Reset() { }

    /// <summary>
    /// Should we continue to wait for the event?
    /// </summary>
    public abstract bool KeepWaiting { get; }
}