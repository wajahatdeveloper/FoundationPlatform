using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using State = CoroutineX.State;

/// <summary>
/// Allows you to work with multiple Coroutines.
/// </summary>
public class CoroutineXGroup
{
    /// <summary>
    /// Coroutines added to the group.
    /// </summary>
    public List<CoroutineX> Coroutines { get; } = new();

    /// <summary>
    /// Coroutines owner. If Coroutines have different owners, <see langword="null"/> will be returned.
    /// </summary>
    public CoroutineXOwner Owner
    {
        get
        {
            if (Coroutines.Count == 0)
                return null;

            var owner = Coroutines[0].Owner;

            for (int i = 1; i < Coroutines.Count; i++)
            {
                if (Coroutines[i].Owner != owner)
                    return null;
            }

            return owner;
        }
    }

    #region State
    /// <summary>
    /// Are all Coroutines in the reset state?
    /// </summary>
    public bool IsReseted => Coroutines.All(m => m.IsReseted);

    /// <summary>
    /// Are all Coroutines in the running state?
    /// </summary>
    public bool IsRunning => Coroutines.All(m => m.IsRunning);

    /// <summary>
    /// Are all Coroutines in the stopped state?
    /// </summary>
    public bool IsStopped => Coroutines.All(m => m.IsStopped);

    /// <summary>
    /// Are all Coroutines in the completed state?
    /// </summary>
    public bool IsCompleted => Coroutines.All(m => m.IsCompleted);

    /// <summary>
    /// Are all Coroutines in the destroyed state?
    /// </summary>
    public bool IsDestroyed => Coroutines.All(m => m.IsDestroyed);

    /// <summary>
    /// Are all Coroutines have owner?
    /// </summary>
    public bool IsOwned => Coroutines.All(m => m.IsOwned);
    #endregion

    /// <summary>
    /// Sets auto destroy option to all Coroutines.
    /// </summary>
    public bool AutoDestroy
    {
        get => Coroutines.All(m => m.AutoDestroy);
        set => Coroutines.ForEach(m => m.AutoDestroy = value);
    }

    #region Events
    /// <summary>
    /// The event that is redeemed when the group resets all Coroutines to its initial state.
    /// Triggered after <see cref="Reset"/> method called.
    /// </summary>
    public event Action<CoroutineXGroup> Reseted;

    /// <summary>
    /// The event that is redeemed when the group starts performing.
    /// Triggered after <see cref="Run"/> method called.
    /// </summary>
    public event Action<CoroutineXGroup> Running;

    /// <summary>
    /// The event that is redeemed when the group stops executing.
    /// Triggered after <see cref="Stop"/> method called.
    /// </summary>
    public event Action<CoroutineXGroup> Stopped;

    /// <summary>
    /// The event that is emit when the Coroutines destroyed.
    /// Triggered after <see cref="Destroy"/> method called.
    /// </summary>
    public event Action<CoroutineXGroup> Destroyed;
    #endregion

    #region Constructors
    /// <summary>
    /// Create empty group.
    /// </summary>
    public CoroutineXGroup() { }

    /// <summary>
    /// Create group with passed <paramref name="coroutines"/>.
    /// </summary>
    /// <param name="coroutines">Coroutines to add in group.</param>
    public CoroutineXGroup(params CoroutineX[] coroutines) : this((IEnumerable<CoroutineX>)coroutines) { }

    /// <summary>
    /// Create group with passed <paramref name="coroutines"/>.
    /// </summary>
    /// <param name="coroutines">Coroutines to add in group.</param>
    public CoroutineXGroup(IEnumerable<CoroutineX> coroutines) => Coroutines.AddRange(coroutines);
    #endregion

    #region Owning
    /// <summary>
    /// Find all unowned Coroutines in the group.
    /// </summary>
    /// <returns>Unowned Coroutines.</returns>
    public List<CoroutineX> GetUnownedCoroutinesX()
    {
        return GetUnownedCoroutinesX(State.Reseted | State.Running | State.Stopped | State.Completed | State.Destroyed);
    }

    /// <summary>
    /// Find all unowned Coroutines in the group by mask.
    /// </summary>
    /// <param name="mask">Mask to filter Coroutines.</param>
    /// <returns>Unowned Coroutines.</returns>
    public List<CoroutineX> GetUnownedCoroutinesX(State mask)
    {
        return CoroutineX.GetUnownedCoroutinesX(mask).Where(m => Coroutines.Contains(m)).ToList();
    }

    /// <summary>
    /// Sets owner to all Coroutines.
    /// </summary>
    /// <param name="component">Any owner's component.</param>
    public CoroutineXGroup SetOwner(Component component) => SetOwner(component.gameObject);

    /// <summary>
    /// Sets owner to all Coroutines.
    /// </summary>
    /// <param name="gameObject">Owner game object.</param>
    public CoroutineXGroup SetOwner(GameObject gameObject)
    {
        Coroutines.ForEach(mor => mor.SetOwner(gameObject));
        return this;
    }

    /// <summary>
    /// Makes all Coroutines unowned.
    /// </summary>
    public CoroutineXGroup MakeUnowned() => SetOwner((GameObject)null);
    #endregion

    #region Control
    /// <summary>
    /// Resets all Coroutines in group.
    /// </summary>
    /// <returns>The Coroutines group.</returns>
    public CoroutineXGroup Reset()
    {
        Coroutines.ForEach(m => m.Reset());
        Reseted?.Invoke(this);

        return this;
    }

    /// <summary>
    /// Runs all Coroutines in group.
    /// </summary>
    /// <returns>The Coroutines group.</returns>
    public CoroutineXGroup Run()
    {
        Coroutines.ForEach(m => m.Run());
        Running?.Invoke(this);

        return this;
    }

    /// <summary>
    /// Stops all Coroutines in group.
    /// </summary>
    /// <returns>The Coroutines group.</returns>
    public CoroutineXGroup Stop()
    {
        Coroutines.ForEach(m => m.Stop());
        Stopped?.Invoke(this);

        return this;
    }

    /// <summary>
    /// Reruns all Coroutines in group.
    /// </summary>
    /// <returns>The Coroutines group.</returns>
    public CoroutineXGroup Rerun()
    {
        Reset();
        Run();

        return this;
    }
    #endregion

    #region Subscribing
    private CoroutineXGroup OnSubscribe(ref Action<CoroutineXGroup> ev, Action<CoroutineXGroup> action)
    {
        ev += action;
        return this;
    }

    /// <summary>
    /// Subscribe to reset event.
    /// </summary>
    /// <param name="action">Callback to invoke.</param>
    /// <returns>The Coroutines group.</returns>
    public CoroutineXGroup OnReseted(Action<CoroutineXGroup> action) => OnSubscribe(ref Reseted, action);

    /// <summary>
    /// Subscribe to run event.
    /// </summary>
    /// <param name="action"><inheritdoc cref="OnReseted(Action{CoroutineX})"/></param>
    /// <returns>The Coroutines group.</returns>
    public CoroutineXGroup OnRunning(Action<CoroutineXGroup> action) => OnSubscribe(ref Running, action);

    /// <summary>
    /// Subscribe to stop event.
    /// </summary>
    /// <param name="action"><inheritdoc cref="OnReseted(Action{CoroutineX})"/></param>
    /// <returns>The Coroutines group.</returns>
    public CoroutineXGroup OnStopped(Action<CoroutineXGroup> action) => OnSubscribe(ref Stopped, action);
    #endregion

    #region Yielders
    /// <summary>
    /// Create an awaiter object, wich knows how to wait until all Coroutines is complete. <br/>
    /// Changing the list of Coroutines after calling this method will have no effect on the awaiting.
    /// </summary>
    /// <returns>Awaiter object.</returns>
    public WaitForAll WaitForComplete() => new(Coroutines.Select(m => m.WaitForComplete()).ToList());

    /// <summary>
    /// Create an awaiter object, wich knows how to wait until all Coroutines is stopped.
    /// Changing the list of Coroutines after calling this method will have no effect on the awaiting.
    /// </summary>
    /// <returns><inheritdoc cref="WaitForComplete"/></returns>
    public WaitForAll WaitForStop() => new(Coroutines.Select(m => m.WaitForStop()).ToList());

    /// <summary>
    /// Create an awaiter object, wich knows how to wait until all Coroutines is runned.
    /// Changing the list of Coroutines after calling this method will have no effect on the awaiting.
    /// </summary>
    /// <returns><inheritdoc cref="WaitForComplete"/></returns>
    public WaitForAll WaitForRun() => new(Coroutines.Select(m => m.WaitForRun()).ToList());

    /// <summary>
    /// Create an awaiter object, wich knows how to wait until all Coroutines is reseted.
    /// Changing the list of Coroutines after calling this method will have no effect on the awaiting.
    /// </summary>
    /// <returns><inheritdoc cref="WaitForComplete"/></returns>
    public WaitForAll WaitForReset() => new(Coroutines.Select(m => m.WaitForReset()).ToList());

    /// <summary>
    /// Create an awaiter object, wich knows how to wait until all Coroutines is destroyed.
    /// Changing the list of Coroutines after calling this method will have no effect on the awaiting.
    /// </summary>
    /// <returns><inheritdoc cref="WaitForComplete"/></returns>
    public WaitForAll WaitForDestroy() => new(Coroutines.Select(m => m.WaitForDestroy()).ToList());
    #endregion

    #region Destroying
    /// <summary>
    /// Stop and destroy Coroutines immediatly. You can't run Coroutines after it destroying.
    /// </summary>
    public CoroutineXGroup Destroy()
    {
        Coroutines.ForEach(m => m.Destroy());
        Destroyed?.Invoke(this);

        return this;
    }

    /// <summary>
    /// Sets Coroutines auto destroying.
    /// </summary>
    /// <param name="autoDestroy"><see langword="true"/> if you need destroy Coroutines after completion.</param>
    /// <returns>The Coroutine.</returns>
    public CoroutineXGroup SetAutoDestroy(bool autoDestroy)
    {
        AutoDestroy = autoDestroy;
        return this;
    }
    #endregion
}