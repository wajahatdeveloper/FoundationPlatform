using AetherNexus.FoundationPlatform.AetherInspector;
using UnityEngine;
using UnityEngine.Events;

namespace AetherNexus.FoundationPlatform.Behaviours
{
    /// <summary>
    /// A class to handle cooldown related properties and their resource consumption over time.
    /// </summary>
    [System.Serializable]
    public class CooldownTracker
    {
        /// all possible states for the object
        public enum CooldownStates
        {
            Idle,
            Consuming,
            PauseOnEmpty,
            Refilling
        }

        /// if this is true, the cooldown won't do anything
        public bool Unlimited = false;

        /// the time it takes, in seconds, to consume the object
        public float ConsumptionDuration = 2f;

        /// the pause to apply before refilling once the object's been depleted
        public float PauseOnEmptyDuration = 1f;

        /// the duration of the refill, in seconds, if uninterrupted
        public float RefillDuration = 1f;

        /// whether or not the refill can be interrupted by a new Start instruction
        public bool CanInterruptRefill = true;

        /// if true, uses unscaled time (ignores Time.timeScale)
        public bool UseUnscaledTime = false;

        [Header("Events")]
        public UnityEvent OnStarted;
        public UnityEvent OnStopped;
        public UnityEvent OnDepleted;
        public UnityEvent OnRefillStarted;
        public UnityEvent OnRefilled;
        public UnityEvent OnStateChanged;

        [ReadOnly]
        /// the current state of the object
        public CooldownStates CooldownState = CooldownStates.Idle;

        [ReadOnly]
        /// the amount of duration left in the object at any given time
        public float CurrentDurationLeft;

        protected float _emptyReachedTimestamp = 0f;
        protected CooldownStates _lastNotifiedState;

        protected float DeltaTime => UseUnscaledTime ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime;
        protected float TimeNow => UseUnscaledTime ? UnityEngine.Time.unscaledTime : UnityEngine.Time.time;

        /// <summary>
        /// An init method that ensures the object is reset
        /// </summary>
        public virtual void Initialization()
        {
            CurrentDurationLeft = ConsumptionDuration;
            CooldownState = CooldownStates.Idle;
            _emptyReachedTimestamp = 0f;
            _lastNotifiedState = CooldownState;
        }

        /// <summary>
        /// Starts consuming the cooldown object if possible
        /// </summary>
        public virtual void Start()
        {
            if (Ready())
            {
                CooldownState = CooldownStates.Consuming;
                _lastNotifiedState = CooldownStates.Consuming;
                if (OnStarted != null) { OnStarted.Invoke(); }
                if (OnStateChanged != null) { OnStateChanged.Invoke(); }
            }
        }

        public virtual bool Ready()
        {
            if (Unlimited)
            {
                return true;
            }

            if (CooldownState == CooldownStates.Idle)
            {
                return true;
            }

            if ((CooldownState == CooldownStates.Refilling) && (CanInterruptRefill))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Stops consuming the object
        /// </summary>
        public virtual void Stop()
        {
            if (CooldownState == CooldownStates.Consuming)
            {
                CooldownState = CooldownStates.PauseOnEmpty;
                _emptyReachedTimestamp = TimeNow;
            }
            else if (CooldownState == CooldownStates.Refilling)
            {
                // stopping during refill brings back to idle
                CooldownState = CooldownStates.Idle;
            }

            if (OnStopped != null) { OnStopped.Invoke(); }
            if (_lastNotifiedState != CooldownState)
            {
                _lastNotifiedState = CooldownState;
                if (OnStateChanged != null) { OnStateChanged.Invoke(); }
            }
        }

        public float Progress
        {
            get
            {
                if (Unlimited)
                {
                    return 1f;
                }

                // Progress always represents the normalized fill (0 empty ? 1 full)
                if (ConsumptionDuration <= 0f)
                {
                    return 1f;
                }
                return Mathf.Clamp01(CurrentDurationLeft / ConsumptionDuration);
            }
        }

        /// <summary>
        /// Processes the object's state machine
        /// </summary>
        public virtual void Update()
        {
            if (Unlimited)
            {
                return;
            }

            switch (CooldownState)
            {
                case CooldownStates.Idle:
                    break;

                case CooldownStates.Consuming:
                    CurrentDurationLeft = CurrentDurationLeft - DeltaTime;
                    if (CurrentDurationLeft <= 0f)
                    {
                        CurrentDurationLeft = 0f;
                        _emptyReachedTimestamp = TimeNow;
                        CooldownState = CooldownStates.PauseOnEmpty;
                        if (OnDepleted != null) { OnDepleted.Invoke(); }
                    }

                    break;

                case CooldownStates.PauseOnEmpty:
                    if (TimeNow - _emptyReachedTimestamp >= PauseOnEmptyDuration)
                    {
                        CooldownState = CooldownStates.Refilling;
                        if (OnRefillStarted != null) { OnRefillStarted.Invoke(); }
                    }

                    break;

                case CooldownStates.Refilling:
                    // Refill towards full over RefillDuration seconds
                    if (RefillDuration <= 0f)
                    {
                        CurrentDurationLeft = ConsumptionDuration;
                        CooldownState = CooldownStates.Idle;
                        if (OnRefilled != null) { OnRefilled.Invoke(); }
                        break;
                    }

                    float refillPerSecond = ConsumptionDuration / RefillDuration;
                    CurrentDurationLeft += refillPerSecond * DeltaTime;
                    if (CurrentDurationLeft >= ConsumptionDuration)
                    {
                        CurrentDurationLeft = ConsumptionDuration;
                        CooldownState = CooldownStates.Idle;
                        if (OnRefilled != null) { OnRefilled.Invoke(); }
                    }

                    break;
            }

            if (_lastNotifiedState != CooldownState)
            {
                _lastNotifiedState = CooldownState;
                if (OnStateChanged != null) { OnStateChanged.Invoke(); }
            }
        }

        // Utilities
        public virtual void ResetCooldown()
        {
            Initialization();
        }

        public virtual void ForceDeplete()
        {
            if (Unlimited)
            {
                return;
            }
            CurrentDurationLeft = 0f;
            _emptyReachedTimestamp = TimeNow;
            CooldownState = CooldownStates.PauseOnEmpty;
            if (OnDepleted != null) { OnDepleted.Invoke(); }
            if (OnStateChanged != null) { OnStateChanged.Invoke(); }
        }

        public virtual void ForceRefill()
        {
            CurrentDurationLeft = ConsumptionDuration;
            CooldownState = CooldownStates.Idle;
            if (OnRefilled != null) { OnRefilled.Invoke(); }
            if (OnStateChanged != null) { OnStateChanged.Invoke(); }
        }

        public virtual bool StartIfReady()
        {
            if (Ready())
            {
                Start();
                return true;
            }
            return false;
        }

        public virtual void SetUnlimited(bool unlimited)
        {
            Unlimited = unlimited;
            if (Unlimited)
            {
                CooldownState = CooldownStates.Idle;
                CurrentDurationLeft = ConsumptionDuration;
                if (OnStateChanged != null) { OnStateChanged.Invoke(); }
            }
        }

        public virtual void SetDurations(float consumptionDuration, float pauseOnEmptyDuration, float refillDuration)
        {
            ConsumptionDuration = Mathf.Max(0f, consumptionDuration);
            PauseOnEmptyDuration = Mathf.Max(0f, pauseOnEmptyDuration);
            RefillDuration = Mathf.Max(0f, refillDuration);
        }

        public bool IsEmpty => CurrentDurationLeft <= 0f;
        public bool IsFull => CurrentDurationLeft >= ConsumptionDuration;
        public float Remaining => Mathf.Max(0f, CurrentDurationLeft);
    }
}