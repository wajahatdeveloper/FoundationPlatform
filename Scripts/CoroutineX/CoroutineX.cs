using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Represents a more advanced coroutine. You can control execution, subscribe to events, <br/>
/// get the last result, wait for a specific events, and more.
/// </summary>
[Serializable]
public sealed class CoroutineX
{
    #region Entities
    #region Awaiter classes
    /// <summary>
    /// Represents a base class for expectations.
    /// </summary>
    internal abstract class CoroutineXAwaiter : YieldAwaiter
    {
        protected CoroutineX coroutineX;

        /// <summary>
        /// Create awaiter-object which can await some Coroutine events.
        /// </summary>
        /// <param name="coroutineX">Coroutine, whose event is to be expected.</param>
        public CoroutineXAwaiter(CoroutineX coroutineX) => this.coroutineX = coroutineX;
    }

    /// <summary>
    /// Represents a class capable of waiting for a Coroutine completion event.
    /// </summary>
    internal class CompleteAwaiter : CoroutineXAwaiter
    {
        private IEnumerator _enumerator;

        /// <summary>
        /// Create awaiter-object which can await Coroutine's complete event.
        /// </summary>
        /// <param name="coroutineX"><inheritdoc cref="CoroutineAwaiter(CoroutineX)"/></param>
        public CompleteAwaiter(CoroutineX coroutineX) : base(coroutineX) => _enumerator = coroutineX._enumerator;

        /// <summary>
        /// Should we continue to wait for the Coroutine to be completed, or has it already been completed?
        /// </summary>
        public override bool KeepWaiting => _enumerator == coroutineX._enumerator && !(coroutineX.IsCompleted || coroutineX.IsDestroyed);
    }

    /// <summary>
    /// Represents a class capable of waiting for a Coroutine stop event.
    /// </summary>
    internal class StopAwaiter : CoroutineXAwaiter
    {
        private Coroutine _coroutine;

        /// <summary>
        /// Create awaiter-object which can await Coroutine's stop event.
        /// </summary>
        /// <param name="coroutineX"><inheritdoc cref="CoroutineAwaiter(CoroutineX)"/></param>
        public StopAwaiter(CoroutineX coroutineX) : base(coroutineX) => _coroutine = coroutineX._coroutine;

        /// <summary>
        /// Should we continue to wait for the Coroutine to be stopped, or has it already been stopped?
        /// </summary>
        public override bool KeepWaiting => _coroutine == coroutineX._coroutine && coroutineX.IsRunning;
    }

    /// <summary>
    /// Represents a class capable of waiting for a Coroutine run event.
    /// </summary>
    internal class RunAwaiter : CoroutineXAwaiter
    {
        private Coroutine _coroutine;

        /// <summary>
        /// Create awaiter-object which can await Coroutine's run event.
        /// </summary>
        /// <param name="coroutineX"><inheritdoc cref="CoroutineAwaiter(CoroutineX)"/></param>
        public RunAwaiter(CoroutineX coroutineX) : base(coroutineX) => _coroutine = coroutineX._coroutine;

        /// <summary>
        /// Should we continue to wait for the Coroutine to be run, or has it already been runned?
        /// </summary>
        public override bool KeepWaiting => _coroutine == coroutineX._coroutine && !coroutineX.IsRunning;
    }

    /// <summary>
    /// Represents a class capable of waiting for a Coroutine reset event.
    /// </summary>
    internal class ResetAwaiter : CoroutineXAwaiter
    {
        private IEnumerator _enumerator;

        /// <summary>
        /// Create awaiter-object which can await Coroutine's reset event.
        /// </summary>
        /// <param name="coroutineX"><inheritdoc cref="CoroutineAwaiter(CoroutineX)"/></param>
        public ResetAwaiter(CoroutineX coroutineX) : base(coroutineX) => _enumerator = coroutineX._enumerator;

        /// <summary>
        /// Should we continue to wait for the Coroutine to be reset, or has it already been reseted?
        /// </summary>
        public override bool KeepWaiting => _enumerator == coroutineX._enumerator;
    }

    /// <summary>
    /// Represents a class capable of waiting for a Coroutine destroyed event.
    /// </summary>
    internal class DestroyAwaiter : CoroutineXAwaiter
    {
        /// <summary>
        /// Create awaiter-object which can await Coroutine's destroy event.
        /// </summary>
        /// <param name="coroutineX"><inheritdoc cref="CoroutineAwaiter(CoroutineX)"/></param>
        public DestroyAwaiter(CoroutineX coroutineX) : base(coroutineX) { }

        /// <summary>
        /// Should we continue to wait for the Coroutine to be destroyed, or has it already been destroyed?
        /// </summary>
        public override bool KeepWaiting => !coroutineX.IsDestroyed;
    }
    #endregion

    /// <summary>
    /// Represents the state of Coroutine.
    /// </summary>
    [Flags]
    public enum State
    {
        /// <summary>
        /// CoroutineX is in the initial state.
        /// </summary>
        Reseted = 1,

        /// <summary>
        /// CoroutineX is being executed right now.
        /// </summary>
        Running = 2,

        /// <summary>
        /// CoroutineX is suspended.
        /// </summary>
        Stopped = 4,

        /// <summary>
        /// The execution of the Coroutine is completed.
        /// </summary>
        Completed = 8,

        /// <summary>
        /// Coroutine completely destroyed.
        /// </summary>
        Destroyed = 16,
    }
    #endregion

    #region State
    /// <summary>
    /// Name of the Coroutine.
    /// </summary>
    public string Name { get; set; }

    private State _state = State.Reseted;

    /// <summary>
    /// Current state of the  Coroutine.
    /// </summary>
    public State CurrentState
    {
        get => _state;
        private set
        {
            _state = value;

            Action<CoroutineX> stateEvent = null;

            switch (_state)
            {
                case State.Reseted:
                    stateEvent = Reseted;
                    break;
                case State.Running:
                    stateEvent = Running;
                    break;
                case State.Stopped:
                    stateEvent = Stopped;
                    break;
                case State.Completed:
                    stateEvent = Completed;
                    break;
                case State.Destroyed:
                    stateEvent = Destroyed;
                    break;
                default:
                    DebugX.Logger(LogChannels.DevTools).Error("Wrong CoroutineX state: {State}", _state);
                    break;
            }

            stateEvent?.Invoke(this);
        }
    }

    /// <summary>
    /// Is Coroutine in reset state?
    /// </summary>
    public bool IsReseted => CurrentState == State.Reseted;

    /// <summary>
    /// Is Coroutine being performed right now?
    /// </summary>
    public bool IsRunning => CurrentState == State.Running;

    /// <summary>
    /// Is Coroutine in stopped state?
    /// </summary>
    public bool IsStopped => CurrentState == State.Stopped;

    /// <summary>
    /// Did Coroutine complete the fulfillment?
    /// </summary>
    public bool IsCompleted => CurrentState == State.Completed;

    /// <summary>
    /// Is Coroutine destroyed?
    /// </summary>
    public bool IsDestroyed => CurrentState == State.Destroyed;

    /// <summary>
    /// Does the Coroutine have an owner?
    /// </summary>
    public bool IsOwned { get; private set; }

    /// <summary>
    /// The last result of the Coroutine (the last one that was returned via the yield return instruction inside Coroutine).
    /// </summary>
    public object LastResult => _enumerator?.Current;

    /// <summary>
    /// Is it need to destroy Coroutine after completion? Auto destroy will be ignored if Coroutine was created from <see cref="IEnumerator"/> object.
    /// </summary>
    public bool AutoDestroy { get; set; }
    #endregion

    #region Owner and routines
    public CoroutineXOwner Owner { get; private set; }

    private IEnumerable _enumerable;

    private IEnumerator _enumerator;

    private Coroutine _coroutine;
    #endregion

    #region Events
    /// <summary>
    /// The event that is redeemed when the Coroutine resets to its initial state.
    /// </summary>
    public event Action<CoroutineX> Reseted;

    /// <summary>
    /// The event that is redeemed when the Coroutine starts performing.
    /// </summary>
    public event Action<CoroutineX> Running;

    /// <summary>
    /// The event that is redeemed when the Coroutine stops executing.
    /// </summary>
    public event Action<CoroutineX> Stopped;

    /// <summary>
    /// The event that is redeemed when the Coroutine complete executing.
    /// </summary>
    public event Action<CoroutineX> Completed;

    /// <summary>
    /// The event that is emit when the Coroutine destroyed.
    /// </summary>
    public event Action<CoroutineX> Destroyed;
    #endregion

    private CoroutineX(GameObject owner, IEnumerable enumerable)
    {
        _enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
        _enumerator = _enumerable.GetEnumerator();

        if (owner == null)
        {
            CoroutineXExecutor.Instance.Owner.Add(this);
            return;
        }

        Owner = owner.TryGetComponent(out CoroutineXOwner existingOwner) ? existingOwner : owner.AddComponent<CoroutineXOwner>();
        Owner.Add(this);

        IsOwned = true;
    }

    #region GetUnownedCoroutinesX
    /// <summary>
    /// Gets all unowned Coroutines.<br/>
    /// Destroyed Coroutines are not taken into account.
    /// </summary>
    /// <returns>Unowned Coroutines.</returns>
    public static List<CoroutineX> GetUnownedCoroutinesX() => CoroutineXExecutor.Instance.gameObject.GetCoroutinesX();

    /// <summary>
    /// Gets unowned Coroutines by <paramref name="mask"/>.
    /// Destroyed Coroutines are not taken into account.
    /// </summary>
    /// <param name="mask">State mask.</param>
    /// <returns>Unowned Coroutines.</returns>
    public static List<CoroutineX> GetUnownedCoroutinesX(State mask) => CoroutineXExecutor.Instance.gameObject.GetCoroutinesX(mask);
    #endregion

    #region Creation
    #region Single
    /// <summary>
    /// Create Coroutine.
    /// </summary>
    /// <param name="enumerator">Enumerator which will be perform by Coroutine. <br/>
    /// The <see cref="Reset"/> method call will be ignored.</param>
    /// <returns>The Coroutine.</returns>
    public static CoroutineX Create(IEnumerator enumerator) => Create(new EnumerableEnumerator(enumerator)).SetAutoDestroy(true);

    /// <summary>
    /// <inheritdoc cref="Create(IEnumerator)"/>
    /// </summary>
    /// <param name="enumerable">Enumerable which will be perform by Coroutine. <br/>
    /// Calling the <see cref="Reset"/> method will reset the state of the Coroutine.</param>
    /// <returns><inheritdoc cref="Create(IEnumerator)"/></returns>
    public static CoroutineX Create(IEnumerable enumerable) => Create((GameObject)null, enumerable);

    /// <summary>
    /// Create Coroutine with owner.
    /// </summary>
    /// <param name="ownersComponent">The owner's component of the Coroutine. Coroutine will be owned by the component's gameObject.</param>
    /// <param name="enumerator"><inheritdoc cref="Create(IEnumerator)"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator)"/></returns>
    public static CoroutineX Create(Component ownersComponent, IEnumerator enumerator) => Create(ownersComponent, new EnumerableEnumerator(enumerator)).SetAutoDestroy(true);

    /// <summary>
    /// Create Coroutine with owner.
    /// </summary>
    /// <param name="ownersComponent"><inheritdoc cref="Create(Component, IEnumerator)" path="/param[@name='ownersComponent']"/></param>
    /// <param name="enumerable"><inheritdoc cref="Create(IEnumerable)" path="/param[@name='enumerable']"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator)"/></returns>
    public static CoroutineX Create(Component ownersComponent, IEnumerable enumerable) => Create(ownersComponent.gameObject, enumerable);

    /// <summary>
    /// Create Coroutine with owner.
    /// </summary>
    /// <param name="owner">The owner of the Coroutine.</param>
    /// <param name="enumerator"><inheritdoc cref="Create(IEnumerator)"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator)"/></returns>
    public static CoroutineX Create(GameObject owner, IEnumerator enumerator) => Create(owner, new EnumerableEnumerator(enumerator)).SetAutoDestroy(true);

    /// <summary>
    /// Create Coroutine with owner.
    /// </summary>
    /// <param name="owner"><inheritdoc cref="Create(GameObject, IEnumerator)" path="/param[@name='owner']"/></param>
    /// <param name="enumerable"><inheritdoc cref="Create(IEnumerable)" path="/param[@name='enumerable']"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator)"/></returns>
    public static CoroutineX Create(GameObject owner, IEnumerable enumerable) => new(owner, enumerable);

    /// <summary>
    /// Create and run Coroutine.
    /// </summary>
    /// <param name="enumerator"><inheritdoc cref="Create(IEnumerator)"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator)"/></returns>
    public static CoroutineX Run(IEnumerator enumerator) => Run(new EnumerableEnumerator(enumerator)).SetAutoDestroy(true);

    /// <summary>
    /// Create and run Coroutine.
    /// </summary>
    /// <param name="enumerable"><inheritdoc cref="Create(IEnumerable)"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator)"/></returns>
    public static CoroutineX Run(IEnumerable enumerable) => Run((GameObject)null, enumerable);

    /// <summary>
    /// Create and run Coroutine.
    /// </summary>
    /// <param name="ownersComponent">The owner's component of the Coroutine. Coroutine will be owned by the component's gameObject.</param>
    /// <param name="enumerator"><inheritdoc cref="Run(IEnumerator)"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator)"/></returns>
    public static CoroutineX Run(Component ownersComponent, IEnumerator enumerator) => Run(ownersComponent, new EnumerableEnumerator(enumerator)).SetAutoDestroy(true);

    /// <summary>
    /// Create and run Coroutine.
    /// </summary>
    /// /// <param name="ownersComponent"><inheritdoc cref="Run(Component, IEnumerator)" path="/param[@name='ownersComponent']"/></param>
    /// <param name="enumerable"><inheritdoc cref="Create(IEnumerable)"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator)"/></returns>
    public static CoroutineX Run(Component ownersComponent, IEnumerable enumerable) => Run(ownersComponent.gameObject, enumerable);

    /// <summary>
    /// Create and run Coroutine.
    /// </summary>
    /// <param name="owner"><inheritdoc cref="Create(GameObject, IEnumerator)" path="/param[@name='owner']"/></param>
    /// <param name="enumerator"><inheritdoc cref="Create(IEnumerator)"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator)"/></returns>
    public static CoroutineX Run(GameObject owner, IEnumerator enumerator) => Run(owner, new EnumerableEnumerator(enumerator)).SetAutoDestroy(true);

    /// <summary>
    /// Create and run Coroutine.
    /// </summary>
    /// /// <param name="owner"><inheritdoc cref="Create(GameObject, IEnumerator)" path="/param[@name='owner']"/></param>
    /// <param name="enumerable"><inheritdoc cref="Create(IEnumerable)"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator)"/></returns>
    public static CoroutineX Run(GameObject owner, IEnumerable enumerable) => Create(owner, enumerable).Run();
    #endregion

    #region Multiple
    private static List<CoroutineX> SetAutoDestroy(List<CoroutineX> coroutines)
    {
        coroutines.ForEach(m => m.AutoDestroy = true);
        return coroutines;
    }

    private static List<CoroutineX> Run(List<CoroutineX> coroutines)
    {
        coroutines.ForEach(m => m.Run());
        return coroutines;
    }

    /// <summary>
    /// Create multiple Coroutines.
    /// </summary>
    /// <param name="enumerators">Enumerators which will be perform by Coroutines. <br/>
    /// The <see cref="Reset"/> method call will be ignored.</param>
    /// <returns>The Coroutines.</returns>
    public static List<CoroutineX> Create(params IEnumerator[] enumerators) => SetAutoDestroy(Create(enumerators.Select(e => new EnumerableEnumerator(e)).ToArray()));

    /// <summary>
    /// <inheritdoc cref="Create(IEnumerator[])"/>
    /// </summary>
    /// <param name="enumerables">Enumerables which will be perform by Coroutines. <br/>
    /// Calling the <see cref="Reset"/> method will reset the state of the Coroutine.</param>
    /// <returns><inheritdoc cref="Create(IEnumerator[])"/></returns>
    public static List<CoroutineX> Create(params IEnumerable[] enumerables) => Create((GameObject)null, enumerables);

    /// <summary>
    /// Create Coroutines with owner.
    /// </summary>
    /// <param name="ownersComponent">The owner's component of the Coroutines. Coroutines will be owned by the component's gameObject.</param>
    /// <param name="enumerators"><inheritdoc cref="Create(IEnumerator[])"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator[])"/></returns>
    public static List<CoroutineX> Create(Component ownersComponent, params IEnumerator[] enumerators) => SetAutoDestroy(Create(ownersComponent, enumerators.Select(e => new EnumerableEnumerator(e)).ToArray()));

    /// <summary>
    /// Create Coroutines with owner.
    /// </summary>
    /// <param name="ownersComponent"><inheritdoc cref="Create(Component, IEnumerator[])" path="/param[@name='ownersComponent']"/></param>
    /// <param name="enumerables"><inheritdoc cref="Create(IEnumerable[])" path="/param[@name='enumerables']"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator[])"/></returns>
    public static List<CoroutineX> Create(Component ownersComponent, params IEnumerable[] enumerables) => Create(ownersComponent.gameObject, enumerables);

    /// <summary>
    /// Create Coroutines with owner.
    /// </summary>
    /// <param name="owner">The owner of the Coroutines.</param>
    /// <param name="enumerators"><inheritdoc cref="Create(IEnumerator[])"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator[])"/></returns>
    public static List<CoroutineX> Create(GameObject owner, params IEnumerator[] enumerators) => SetAutoDestroy(Create(owner, enumerators.Select(e => new EnumerableEnumerator(e)).ToArray()));

    /// <summary>
    /// Create Coroutines with owner.
    /// </summary>
    /// <param name="owner"><inheritdoc cref="Create(GameObject, IEnumerator[])" path="/param[@name='owner']"/></param>
    /// <param name="enumerables"><inheritdoc cref="Create(IEnumerable[])" path="/param[@name='enumerables']"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator[])"/></returns>
    public static List<CoroutineX> Create(GameObject owner, params IEnumerable[] enumerables) => enumerables.Select(e => new CoroutineX(owner, e)).ToList();

    /// <summary>
    /// Create and run Coroutines.
    /// </summary>
    /// <param name="enumerators"><inheritdoc cref="Create(IEnumerator[])"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator[])"/></returns>
    public static List<CoroutineX> Run(params IEnumerator[] enumerators) => SetAutoDestroy(Run(enumerators.Select(e => new EnumerableEnumerator(e)).ToArray()));

    /// <summary>
    /// Create and run Coroutines.
    /// </summary>
    /// <param name="enumerables"><inheritdoc cref="Create(IEnumerable[])"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator[])"/></returns>
    public static List<CoroutineX> Run(params IEnumerable[] enumerables) => Run((GameObject)null, enumerables);

    /// <summary>
    /// Create and run Coroutines.
    /// </summary>
    /// <param name="ownersComponent">The owner's component of the Coroutines. Coroutines will be owned by the component's gameObject.</param>
    /// <param name="enumerators"><inheritdoc cref="Run(IEnumerator[])"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator[])"/></returns>
    public static List<CoroutineX> Run(Component ownersComponent, params IEnumerator[] enumerators) => SetAutoDestroy(Run(ownersComponent, enumerators.Select(e => new EnumerableEnumerator(e)).ToArray()));

    /// <summary>
    /// Create and run Coroutines.
    /// </summary>
    /// /// <param name="ownersComponent"><inheritdoc cref="Run(Component, IEnumerator[])" path="/param[@name='ownersComponent']"/></param>
    /// <param name="enumerables"><inheritdoc cref="Create(IEnumerable[])"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator[])"/></returns>
    public static List<CoroutineX> Run(Component ownersComponent, params IEnumerable[] enumerables) => Run(ownersComponent.gameObject, enumerables);

    /// <summary>
    /// Create and run Coroutines.
    /// </summary>
    /// <param name="owner"><inheritdoc cref="Create(GameObject, IEnumerator[])" path="/param[@name='owner']"/></param>
    /// <param name="enumerators"><inheritdoc cref="Create(IEnumerator[])"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator[])"/></returns>
    public static List<CoroutineX> Run(GameObject owner, params IEnumerator[] enumerators) => SetAutoDestroy(Run(owner, enumerators.Select(e => new EnumerableEnumerator(e)).ToArray()));

    /// <summary>
    /// Create and run Coroutines.
    /// </summary>
    /// /// <param name="owner"><inheritdoc cref="Create(GameObject, IEnumerator[])" path="/param[@name='owner']"/></param>
    /// <param name="enumerables"><inheritdoc cref="Create(IEnumerable[])"/></param>
    /// <returns><inheritdoc cref="Create(IEnumerator[])"/></returns>
    public static List<CoroutineX> Run(GameObject owner, params IEnumerable[] enumerables) => Run(Create(owner, enumerables));
    #endregion
    #endregion

    /// <summary>
    /// Sets passed name to Coroutine.
    /// </summary>
    /// <param name="name">Name to set.</param>
    /// <returns>The Coroutine.</returns>
    public CoroutineX SetName(string name)
    {
        Name = name;
        return this;
    }

    #region Control
    /// <summary>
    /// Starts the Coroutine for execution. If the Coroutine has been stopped before, the method will continue.
    /// </summary>
    /// <param name="rerunIfCompleted">Is it necessary to automatically restart the Coroutine if it has been completed?</param>
    /// <returns>The Coroutine.</returns>
    /// <exception cref="PlayControlException"></exception>
    public CoroutineX Run(bool rerunIfCompleted = true)
    {
        if (IsRunning) {return this;}

        if (IsDestroyed)
        {
            DebugX.Logger(LogChannels.DevTools).Error("CoroutineX already destroyed. Name: {Name}", Name ?? "unnamed");
            return this;
        }

        if (IsOwned)
        {
            if (Owner == null)
            {
                DebugX.Logger(LogChannels.DevTools).Error("CoroutineX couldn't be started because the game object's Owner component is missing. Name: {Name}", Name ?? "unnamed");
                return this;
            }

            if (!Owner.gameObject.activeInHierarchy)
            {
                DebugX.Logger(LogChannels.DevTools).Error("CoroutineX couldn't be started because the game object '{OwnerName}' is deactivated. Coroutine name: {Name}", Owner.name, Name ?? "unnamed");
                return this;
            }
        }

        if (IsCompleted)
        {
            if (!rerunIfCompleted)
            {
                DebugX.Logger(LogChannels.DevTools).Error("CoroutineX was completed. If you want to restart it, call \"Reset\" method before. Name: {Name}", Name ?? "unnamed");
                return this;
            }

            Reset();
        }

        CurrentState = State.Running;
        _coroutine = CoroutineXExecutor.Instance.StartCoroutine(RunEnumerator());

        return this;
    }

    private IEnumerator RunEnumerator()
    {
        while (true)
        {
            if (!_enumerator.MoveNext())
                break;

            yield return _enumerator.Current;
        }

        CurrentState = State.Completed;

        if (AutoDestroy)
            Destroy();
    }

    /// <summary>
    /// Stops the Coroutine if it in playing state.
    /// </summary>
    /// <returns>The Coroutine.</returns>
    /// <exception cref="PlayControlException"></exception>
    public CoroutineX Stop()
    {
        if (!IsRunning)
            return this;

        if (_coroutine != null)
            CoroutineXExecutor.Instance.StopCoroutine(_coroutine);

        CurrentState = State.Stopped;

        return this;
    }

    /// <summary>
    /// Resets the Coroutine to initial state.
    /// </summary>
    /// <returns>The Coroutine</returns>
    /// <exception cref="PlayControlException"></exception>
    public CoroutineX Reset()
    {
        if (IsReseted || IsDestroyed)
            return this;

        if (_coroutine != null)
            CoroutineXExecutor.Instance.StopCoroutine(_coroutine);

        _enumerator = _enumerable.GetEnumerator();
        CurrentState = State.Reseted;

        return this;
    }

    /// <summary>
    /// Shorthand for:
    /// <code>
    /// Reset().Run()
    /// </code>
    /// </summary>
    /// <returns>The Coroutine.</returns>
    public CoroutineX Rerun() => Reset().Run();
    #endregion

    #region Owning
    /// <summary>
    /// Sets owner to the Coroutine.
    /// </summary>
    /// <param name="component">Component whose game object used as owner.</param>
    /// <returns>The Coroutine.</returns>
    public CoroutineX SetOwner(Component component) => SetOwner(component.gameObject);

    /// <summary>
    /// Sets owner to the Coroutine.
    /// </summary>
    /// <param name="ownerGameObject">Owner for the Coroutine.</param>
    /// <returns>The Coroutine.</returns>
    public CoroutineX SetOwner(GameObject ownerGameObject)
    {
        if (IsDestroyed)
        {
            DebugX.Logger(LogChannels.DevTools).Error("Destroyed CoroutineX can't change its owner. Name: {Name}", Name ?? "unnamed");
            return this;
        }

        if (!IsOwned && ownerGameObject == null || IsOwned && Owner.gameObject == ownerGameObject)
            return this;

        if (!IsOwned)
            CoroutineXExecutor.Instance.Owner.Remove(this);
        else
        {
            Owner.Remove(this);
            Owner.TryDestroy();
        }

        if (ownerGameObject == null)
        {
            Owner = null;
            CoroutineXExecutor.Instance.Owner.Add(this);
            IsOwned = false;
        }
        else
        {
            Owner = ownerGameObject.TryGetComponent(out CoroutineXOwner existingOwner) ? existingOwner : ownerGameObject.AddComponent<CoroutineXOwner>();
            Owner.Add(this);
            IsOwned = true;

            if (!Owner.gameObject.activeInHierarchy)
                Stop();
        }

        return this;
    }

    /// <summary>
    /// Make Coroutine unowned.
    /// </summary>
    /// <returns>The Coroutine.</returns>
    public CoroutineX MakeUnowned() => SetOwner((GameObject)null);

    internal void OnOwnerDeactivate()
    {
        // CoroutinesExecutor can be null when we close the application.
        if (!CoroutineXExecutor.HasInstance)
            return;

        if (IsRunning)
            Stop();
    }
    #endregion

    #region Subscribing
    private CoroutineX OnSubscribe(ref Action<CoroutineX> ev, Action<CoroutineX> action)
    {
        ev += action;
        return this;
    }

    /// <summary>
    /// Subscribe to reset event.
    /// </summary>
    /// <param name="action">Callback to invoke.</param>
    /// <returns>The Coroutine.</returns>
    public CoroutineX OnReseted(Action<CoroutineX> action) => OnSubscribe(ref Reseted, action);

    /// <summary>
    /// Subscribe to run event.
    /// </summary>
    /// <param name="action"><inheritdoc cref="OnReseted(Action{CoroutineX})"/></param>
    /// <returns>The Coroutine.</returns>
    public CoroutineX OnRunning(Action<CoroutineX> action) => OnSubscribe(ref Running, action);

    /// <summary>
    /// Subscribe to stop event.
    /// </summary>
    /// <param name="action"><inheritdoc cref="OnReseted(Action{CoroutineX})"/></param>
    /// <returns>The Coroutine.</returns>
    public CoroutineX OnStopped(Action<CoroutineX> action) => OnSubscribe(ref Stopped, action);

    /// <summary>
    /// Subscribe to completed event.
    /// </summary>
    /// <param name="action"><inheritdoc cref="OnReseted(Action{CoroutineX})"/></param>
    /// <returns>The Coroutine.</returns>
    public CoroutineX OnCompleted(Action<CoroutineX> action) => OnSubscribe(ref Completed, action);

    /// <summary>
    /// Subscribe to destroyed event.
    /// </summary>
    /// <param name="action"><inheritdoc cref="OnReseted(Action{CoroutineX})"/></param>
    /// <returns>The Coroutine.</returns>
    public CoroutineX OnDestroyed(Action<CoroutineX> action) => OnSubscribe(ref Destroyed, action);
    #endregion

    #region Yielders
    /// <summary>
    /// Create an awaiter object, wich knows how to wait until the Coroutine is complete.
    /// </summary>
    /// <returns>Awaiter object.</returns>
    public YieldAwaiter WaitForComplete() => new CompleteAwaiter(this);

    /// <summary>
    /// Create an awaiter object, wich knows how to wait until the Coroutine is stopped.
    /// </summary>
    /// <returns><inheritdoc cref="WaitForComplete()"/></returns>
    public YieldAwaiter WaitForStop() => new StopAwaiter(this);

    /// <summary>
    /// Create an awaiter object, wich knows how to wait until the Coroutine is running.
    /// </summary>
    /// <returns><inheritdoc cref="WaitForComplete()"/></returns>
    public YieldAwaiter WaitForRun() => new RunAwaiter(this);

    /// <summary>
    /// Create an awaiter object, wich knows how to wait until the Coroutine is reseted.
    /// </summary>
    /// <returns><inheritdoc cref="WaitForComplete()"/></returns>
    public YieldAwaiter WaitForReset() => new ResetAwaiter(this);

    /// <summary>
    /// Create an awaiter object, wich knows how to wait until the Coroutine is destroyed.
    /// </summary>
    /// <returns><inheritdoc cref="WaitForComplete()"/></returns>
    public YieldAwaiter WaitForDestroy() => new DestroyAwaiter(this);
    #endregion

    #region Destroying
    /// <summary>
    /// Stop and destroy Coroutine immediatly. You can't run the Coroutine after it destroying.
    /// </summary>
    public void Destroy()
    {
        if (IsDestroyed)
            return;

        if (IsRunning)
            Stop();

        if (!IsOwned)
            CoroutineXExecutor.Instance.Owner.Remove(this);
        else
        {
            Owner.Remove(this);
            Owner.TryDestroy();
            Owner = null;
        }

        CurrentState = State.Destroyed;
    }

    /// <summary>
    /// Sets Coroutine's auto destroying.
    /// </summary>
    /// <param name="autoDestroy"><see langword="true"/> if you need destroy Coroutine after completion.</param>
    /// <returns>The Coroutine.</returns>
    public CoroutineX SetAutoDestroy(bool autoDestroy)
    {
        AutoDestroy = autoDestroy;
        return this;
    }

    /// <summary>
    /// Waits for the coroutine to complete asynchronously.
    /// </summary>
    /// <returns>A Task that completes when the coroutine finishes.</returns>
    public async System.Threading.Tasks.Task WaitForCompletionAsync()
    {
        while (!IsCompleted && !IsDestroyed)
        {
            await System.Threading.Tasks.Task.Yield();
        }
    }

    /// <summary>
    /// Runs a function asynchronously and returns the result.
    /// </summary>
    /// <typeparam name="T">Return type of the function.</typeparam>
    /// <param name="function">Function to execute asynchronously.</param>
    /// <returns>Task containing the result of the function.</returns>
    public static async System.Threading.Tasks.Task<T> RunAsync<T>(System.Func<T> function)
    {
        return await System.Threading.Tasks.Task.Run(function);
    }

    /// <summary>
    /// Runs an action asynchronously.
    /// </summary>
    /// <param name="action">Action to execute asynchronously.</param>
    /// <returns>Task that completes when the action finishes.</returns>
    public static async System.Threading.Tasks.Task RunAsync(System.Action action)
    {
        await System.Threading.Tasks.Task.Run(action);
    }
    #endregion
}

/*public static class CoroutineX
{
	private static CoroutineOwner CoroutineOwner
	{
		get
		{
			if (_coroutineOwner != null) return _coroutineOwner;

			var go = new GameObject("Static Coroutine Owner");
			Object.DontDestroyOnLoad(go);
			go.hideFlags = HideFlags.HideAndDontSave;

			_coroutineOwner = go.AddComponent<CoroutineOwner>();

			return _coroutineOwner;
		}
	}

	private static CoroutineOwner _coroutineOwner;

	/// <summary>
	/// StartCoroutine without MonoBehaviour
	/// </summary>
	public static Coroutine StartCoroutine(this IEnumerator coroutine)
	{
		return CoroutineOwner.StartCoroutine(coroutine);
	}

	/// <summary>
	/// Stop coroutine started with MyCoroutines.StartCoroutine
	/// </summary>
	public static void StopCoroutine(Coroutine coroutine)
	{
		CoroutineOwner.StopCoroutine(coroutine);
	}

	/// <summary>
	/// Stop all coroutines started with MyCoroutines.StartCoroutine
	/// </summary>
	public static void StopAllCoroutines()
	{
		CoroutineOwner.StopAllCoroutines();
	}
}

internal class CoroutineOwner : MonoBehaviour
{
}*/