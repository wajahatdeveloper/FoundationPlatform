using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoundationPlatform.TweenX
{
    /// <summary>
    /// Non-generic base for a running tween. Owns the whole lifecycle/state machine
    /// (<see cref="Step"/>) so <see cref="TweenManager"/> can tick a heterogeneous list without
    /// knowing the value type. Concrete value handling lives in <see cref="Tween{T}"/>.
    /// Instances are pooled and reused — never hold a raw <c>Tween</c> reference across frames;
    /// use a <see cref="TweenHandle"/> instead.
    /// </summary>
    public abstract class Tween
    {
        // ---- Identity / pooling ----
        internal int Id;
        internal int Generation;   // bumped on every recycle → invalidates stale handles
        internal bool IsAlive;     // false once completed/killed and eligible for recycle
        internal bool Paused;

        // ---- Timing / shape ----
        internal float Duration;
        internal float Delay;
        internal float DelayRemaining;
        internal float Elapsed;
        internal Ease Ease = Ease.Linear;
        internal AnimationCurve Curve;   // when non-null, overrides Ease
        internal float TimeScale = 1f;
        internal TweenClock Clock = TweenClock.Scaled;

        // ---- Looping ----
        internal int LoopCount = 1;      // total plays; <0 = infinite
        internal int LoopsDone;
        internal LoopType LoopType = LoopType.Restart;

        // ---- Modifiers ----
        internal bool Snapping;
        internal bool Relative;
        internal bool FromMode;
        internal bool Started;

        // ---- Target tracking / auto-kill ----
        internal UnityEngine.Object TargetObject;  // when it dies, the tween auto-kills
        internal bool HasTarget;
        internal GameObject LinkedObject;
        internal bool HasLink;
        internal int GroupId;                        // SetId — for KillById

        // ---- Callbacks ----
        internal Action OnStartCb;
        internal Action OnUpdateCb;
        internal Action OnStepCompleteCb;
        internal Action OnCompleteCb;
        internal Action OnKillCb;

        /// <summary>Normalized progress 0..1 of the current step (after delay).</summary>
        internal float Progress => Duration <= 0f ? (Started ? 1f : 0f) : Mathf.Clamp01(Elapsed / Duration);

        /// <summary>
        /// Advance the tween by <paramref name="dt"/> seconds (already clock- and scale-resolved).
        /// Returns false when the tween has finished and should be recycled. Virtual so
        /// <c>Sequence</c> can drive its own composite timeline instead.
        /// </summary>
        internal virtual bool Step(float dt)
        {
            if (!IsAlive) return false;
            if (Paused) return true;

            // Auto-kill if the animated target (or explicit link) was destroyed/disabled.
            if (HasTarget && TargetObject == null) return false;
            if (HasLink && (LinkedObject == null || !LinkedObject.activeInHierarchy)) return false;

            float remaining = dt;

            if (DelayRemaining > 0f)
            {
                DelayRemaining -= remaining;
                if (DelayRemaining > 0f) return true;
                remaining = -DelayRemaining;   // spill leftover into the first real step
                DelayRemaining = 0f;
            }

            if (!Started)
            {
                ResolveStartValues();
                Started = true;
                OnStartCb?.Invoke();
            }

            Elapsed += remaining;
            float linT = Duration <= 0f ? 1f : Mathf.Clamp01(Elapsed / Duration);

            bool backward = LoopType == LoopType.Yoyo && (LoopsDone & 1) == 1;
            float shaped = backward ? 1f - linT : linT;
            ApplyEased(EvaluateEase(shaped));
            OnUpdateCb?.Invoke();

            if (linT < 1f) return true;

            // Reached end of this play.
            OnStepCompleteCb?.Invoke();
            bool moreLoops = LoopCount < 0 || LoopsDone < LoopCount - 1;
            if (moreLoops)
            {
                LoopsDone++;
                Elapsed = 0f;
                if (LoopType == LoopType.Incremental) OnIncrementLoop();
                return true;
            }

            OnCompleteCb?.Invoke();
            return false;   // done → recycle
        }

        /// <summary>Ease/curve lookup shared by all typed tweens.</summary>
        internal float EvaluateEase(float t) => Curve != null ? Curve.Evaluate(t) : EaseEvaluator.Evaluate(Ease, t);

        /// <summary>
        /// Apply this tween's value at an absolute local time (no loop/callback bookkeeping). Used by
        /// <c>Sequence</c> to scrub its children off a shared playhead. Captures start values on first use.
        /// </summary>
        internal void SampleAt(float localTime)
        {
            if (!Started) { ResolveStartValues(); Started = true; }
            Elapsed = localTime;
            float linT = Duration <= 0f ? 1f : Mathf.Clamp01(localTime / Duration);
            ApplyEased(EvaluateEase(linT));
        }

        // ---- Typed hooks implemented by Tween<T> ----
        internal abstract void ResolveStartValues();
        internal abstract void ApplyEased(float easedFactor);
        internal abstract void OnIncrementLoop();
        internal abstract void SnapToStart();
        internal abstract void SnapToEnd(bool backward);

        /// <summary>Push this instance onto the correctly-typed stack in <paramref name="pools"/> (keyed by concrete type).</summary>
        internal abstract void ReturnToPool(Dictionary<Type, object> pools);

        internal virtual void Reset()
        {
            // Identity kept; Generation bumped by the pool on recycle.
            IsAlive = false; Paused = false;
            Duration = 0f; Delay = 0f; DelayRemaining = 0f; Elapsed = 0f;
            Ease = Ease.Linear; Curve = null; TimeScale = 1f; Clock = TweenClock.Scaled;
            LoopCount = 1; LoopsDone = 0; LoopType = LoopType.Restart;
            Snapping = false; Relative = false; FromMode = false; Started = false;
            TargetObject = null; HasTarget = false; LinkedObject = null; HasLink = false; GroupId = 0;
            OnStartCb = OnUpdateCb = OnStepCompleteCb = OnCompleteCb = OnKillCb = null;
        }
    }

    /// <summary>
    /// Value-typed tween. Reads the current value via <see cref="Getter"/>, writes via
    /// <see cref="Setter"/>, and blends with a cached <see cref="Interpolator{T}"/>. All delegates
    /// are supplied once at creation by the fluent extension methods; the tick loop never allocates.
    /// </summary>
    public sealed class Tween<T> : Tween
    {
        internal Func<T> Getter;
        internal Action<T> Setter;
        internal Interpolator<T> Lerp;
        internal Adder<T> Add;              // optional — enables Relative + Incremental
        internal Func<T, bool, T> Snapper;  // optional — (value, enabled) → snapped value

        internal T From;
        internal T To;
        internal T EndValue;   // the destination as originally requested (before From/Relative resolution)

        internal override void ResolveStartValues()
        {
            T current = Getter != null ? Getter() : default;
            if (FromMode)
            {
                // From(): animate from the requested value toward wherever the target is now.
                From = EndValue;
                To = current;
            }
            else
            {
                From = current;
                To = Relative && Add != null ? Add(current, EndValue) : EndValue;
            }
        }

        internal override void ApplyEased(float easedFactor)
        {
            if (Setter == null) return;
            T value = Lerp(From, To, easedFactor);
            if (Snapping && Snapper != null) value = Snapper(value, true);
            Setter(value);
        }

        internal override void OnIncrementLoop()
        {
            if (Add == null) return;             // non-numeric type → behaves like Restart
            T delta = Add(To, Negate(From));     // delta = To - From
            From = To;
            To = Add(To, delta);
        }

        // Incremental needs subtraction; express as add-of-negation via the interpolator at t = -1
        // when no dedicated negate exists. For the numeric types we register an Adder for, this holds.
        private T Negate(T v) => Lerp(v, default, 2f); // v + (0 - v)*2 = -v

        internal override void SnapToStart()
        {
            if (Setter == null) return;
            if (!Started) ResolveStartValues();
            Setter(From);
        }

        internal override void SnapToEnd(bool backward)
        {
            if (Setter == null) return;
            if (!Started) ResolveStartValues();
            Setter(backward ? From : To);
        }

        internal override void ReturnToPool(Dictionary<Type, object> pools)
        {
            var key = typeof(Tween<T>);
            if (!pools.TryGetValue(key, out var s))
            {
                s = new Stack<Tween<T>>();
                pools[key] = s;
            }
            ((Stack<Tween<T>>)s).Push(this);
        }

        internal override void Reset()
        {
            base.Reset();
            Getter = null; Setter = null; Lerp = null; Add = null; Snapper = null;
            From = default; To = default; EndValue = default;
        }
    }
}
