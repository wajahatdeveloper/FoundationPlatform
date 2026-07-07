using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Allows you to wait for completing at least one of multiple objects (<see cref="CoroutineX"/>, <see cref="CustomYieldInstruction"/>, <see cref="IEnumerable"/>, <see cref="IEnumerator"/> and other..).<br/>
/// Don't use with <see cref="WaitForSecondsRealtime"/> object.
/// </summary>
public class WaitForAny : CustomYieldInstruction
{
    private readonly IEnumerable<IEnumerator> _instructions;

    /// <summary>
    /// Is it need to keep waiting for the object?
    /// </summary>
    public override bool keepWaiting => _instructions.Select(m => m.MoveNext()).ToList().All(s => s);

    /// <summary>
    /// <inheritdoc cref="WaitFor(CoroutineX[])"/>
    /// </summary>
    /// <param name="Coroutines"><inheritdoc cref="WaitFor(CoroutineX[])"/></param>
    public WaitForAny(params CoroutineX[] coroutines) : this(coroutines.Select(m => m.WaitForComplete()).ToList()) { }

    /// <summary>
    /// Create object which will waiting Coroutines.
    /// </summary>
    /// <param name="Coroutines">Target Coroutines.</param>
    public WaitForAny(IList<CoroutineX> coroutines) : this(coroutines.Select(m => m.WaitForComplete()).ToList()) { }

    /// <summary>
    /// <inheritdoc cref="WaitFor(IEnumerator[])"/>
    /// </summary>
    /// <param name="instructions"><inheritdoc cref="WaitFor(IEnumerator[])"/></param>
    public WaitForAny(params IEnumerator[] instructions) : this((IEnumerable<IEnumerator>)instructions) { }

    /// <summary>
    /// <inheritdoc cref="WaitFor(IEnumerable{IEnumerator})"/>
    /// </summary>
    /// <param name="instructions"><inheritdoc cref="WaitFor(IEnumerable{IEnumerator})"/></param>
    public WaitForAny(IEnumerable<IEnumerator> instructions) => _instructions = instructions;
}