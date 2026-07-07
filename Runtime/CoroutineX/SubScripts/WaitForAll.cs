using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Allows you to wait for multiple objects (<see cref="CoroutineX"/>, <see cref="CustomYieldInstruction"/>, <see cref="IEnumerable"/>, <see cref="IEnumerator"/> and other..) at once.<br/>
/// Don't use with <see cref="WaitForSecondsRealtime"/> object.
/// </summary>
public class WaitForAll : CustomYieldInstruction
{
    private readonly IEnumerable<IEnumerator> _instructions;

    /// <summary>
    /// Is it need to keep waiting for the object?
    /// </summary>
    public override bool keepWaiting => _instructions.Select(m => m.MoveNext()).ToList().Any(s => s);

    /// <summary>
    /// Create object which will waiting Coroutines.
    /// </summary>
    /// <param name="Coroutines">Target Coroutines.</param>
    public WaitForAll(params CoroutineX[] coroutines) : this(coroutines.Select(m => m.WaitForComplete()).ToList()) { }

    /// <summary>
    /// Create object which will waiting Coroutines.
    /// </summary>
    /// <param name="Coroutines">Target Coroutines.</param>
    public WaitForAll(IList<CoroutineX> coroutines) : this(coroutines.Select(m => m.WaitForComplete()).ToList()) { }

    /// <summary>
    /// Create object which will waiting <see cref="IEnumerator"/> instructions.
    /// </summary>
    /// <param name="instructions">Target instructions.</param>
    public WaitForAll(params IEnumerator[] instructions) : this((IEnumerable<IEnumerator>)instructions) { }

    /// <summary>
    /// Create object which will waiting <see cref="IEnumerable"/><![CDATA[<]]><see cref="IEnumerator"/><![CDATA[>]]> instructions.
    /// </summary>
    /// <param name="instructions">Target instructions.</param>
    public WaitForAll(IEnumerable<IEnumerator> instructions) => _instructions = instructions;
}