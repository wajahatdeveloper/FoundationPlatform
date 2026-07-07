using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CoroutineXExtensions
{
    /// <summary>
    /// Gets all Coroutines which associated with the game object.<br/>
    /// Destroyed Coroutines are not taken into account.
    /// </summary>
    /// <param name="gameObject">The game object.</param>
    /// <returns>Associated Coroutines.</returns>
    public static List<CoroutineX> GetCoroutinesX(this GameObject gameObject)
    {
        return GetCoroutinesX(gameObject, CoroutineX.State.Reseted | CoroutineX.State.Running | CoroutineX.State.Stopped | CoroutineX.State.Completed);
    }

    /// <summary>
    /// Gets all Coroutines by <paramref name="mask"/> which associated with the game object.<br/>
    /// Destroyed Coroutines are not taken into account.
    /// </summary>
    /// <param name="gameObject">The game object.</param>
    /// <param name="mask">State mask.</param>
    /// <returns>Associated Coroutines.</returns>
    public static List<CoroutineX> GetCoroutinesX(this GameObject gameObject, CoroutineX.State mask)
    {
        if (!gameObject.TryGetComponent<CoroutineXOwner>(out var owner))
            return new();

        return new(owner.Coroutines.Where(m => (m.CurrentState & mask) != 0));
    }

    /// <summary>
    /// Create Coroutines group based on Coroutines enumerable.
    /// </summary>
    /// <param name="Coroutines">Coroutines which need add to created group.</param>
    /// <returns>Coroutines group.</returns>
    public static CoroutineXGroup ToCoroutinesXGroup(this IEnumerable<CoroutineX> coroutines) => new(coroutines);

    public static CoroutineX AsCoroutineX(this YieldInstruction instruction) => CoroutineX.Run(Routines.Wait(instruction));

    public static CustomYieldInstruction AsCustomYieldInstruction(this YieldInstruction instruction)
    {
        var coroutine = instruction.AsCoroutineX();
        return new WaitUntil(() => coroutine.IsCompleted);
    }

    /// <summary>
    /// Returns the Coroutine that represents the CustomYieldInstruction object.
    /// </summary>
    /// <param name="instruction">CustomYieldInstruction object.</param>
    /// <returns>The Coroutine.</returns>
    public static CoroutineX AsCoroutineX(this CustomYieldInstruction instruction) => CoroutineX.Run(instruction);

    /// <summary>
    /// Returns the YieldInstruction object that represents the CustomYieldInstruction object.
    /// </summary>
    /// <param name="instruction">CustomYieldInstruction object.</param>
    /// <returns>The YieldInstruction object.</returns>
    public static YieldInstruction AsYieldInstruction(this CustomYieldInstruction instruction) => instruction.AsCoroutineX().WaitForComplete();
}