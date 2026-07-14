using System;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.TweenX
{
    /// <summary>How a tween repeats once it reaches the end of a step.</summary>
    public enum LoopType
    {
        /// <summary>Jump back to the start value and play forward again.</summary>
        Restart = 0,

        /// <summary>Ping-pong: play forward, then backward, then forward…</summary>
        Yoyo = 1,

        /// <summary>Each loop starts where the last ended (from += delta). Requires a numeric type; falls back to Restart otherwise.</summary>
        Incremental = 2,
    }

    /// <summary>
    /// A safe, value-type reference to a running tween. Holds an id plus a generation stamp; every
    /// control call re-validates the generation against the live tween, so once a tween completes
    /// or is killed (and its pooled object is reused for something else) all further calls through
    /// a stale handle become harmless no-ops — no use-after-free, no accidental control of an
    /// unrelated tween. A <c>default(TweenHandle)</c> is always inert.
    /// </summary>
    public readonly struct TweenHandle : IEquatable<TweenHandle>
    {
        internal readonly int Id;
        internal readonly int Generation;

        internal TweenHandle(int id, int generation)
        {
            Id = id;
            Generation = generation;
        }

        /// <summary>True while this handle still points at a live tween.</summary>
        public bool IsActive => TweenManager.IsActive(this);

        /// <summary>Normalized progress 0..1 of the current step (post-delay). 0 if not active.</summary>
        public float Progress => TweenManager.GetProgress(this);

        // ---- Fluent configuration (only meaningful before / during play; no-op if stale) ----

        public TweenHandle SetEase(Ease ease) { TweenManager.SetEase(this, ease); return this; }
        public TweenHandle SetEase(AnimationCurve curve) { TweenManager.SetEase(this, curve); return this; }
        public TweenHandle SetLoops(int count, LoopType type = LoopType.Restart) { TweenManager.SetLoops(this, count, type); return this; }
        public TweenHandle SetDelay(float seconds) { TweenManager.SetDelay(this, seconds); return this; }
        public TweenHandle SetClock(TweenClock clock) { TweenManager.SetClock(this, clock); return this; }
        public TweenHandle SetTimeScale(float scale) { TweenManager.SetTimeScale(this, scale); return this; }
        public TweenHandle SetSnapping(bool snapping = true) { TweenManager.SetSnapping(this, snapping); return this; }
        public TweenHandle SetRelative(bool relative = true) { TweenManager.SetRelative(this, relative); return this; }
        public TweenHandle From() { TweenManager.SetFromCurrentSwap(this); return this; }
        public TweenHandle SetId(int id) { TweenManager.SetGroupId(this, id); return this; }

        /// <summary>Auto-kill this tween when <paramref name="target"/> is destroyed or disabled.</summary>
        public TweenHandle SetLink(GameObject target) { TweenManager.SetLink(this, target); return this; }

        // ---- Callbacks ----

        public TweenHandle OnStart(Action cb) { TweenManager.SetOnStart(this, cb); return this; }
        public TweenHandle OnUpdate(Action cb) { TweenManager.SetOnUpdate(this, cb); return this; }
        public TweenHandle OnStepComplete(Action cb) { TweenManager.SetOnStepComplete(this, cb); return this; }
        public TweenHandle OnComplete(Action cb) { TweenManager.SetOnComplete(this, cb); return this; }
        public TweenHandle OnKill(Action cb) { TweenManager.SetOnKill(this, cb); return this; }

        // ---- Playback control ----

        /// <summary>Pause ticking; retains progress.</summary>
        public TweenHandle Pause() { TweenManager.Pause(this); return this; }

        /// <summary>Resume from where it was paused.</summary>
        public TweenHandle Play() { TweenManager.Play(this); return this; }

        /// <summary>Snap to the end value, fire OnComplete, then kill (unless looping infinitely).</summary>
        public TweenHandle Complete() { TweenManager.Complete(this); return this; }

        /// <summary>Snap back to the start value and pause at the beginning.</summary>
        public TweenHandle Rewind() { TweenManager.Rewind(this); return this; }

        /// <summary>Stop and recycle immediately. Fires OnKill.</summary>
        public void Kill() => TweenManager.Kill(this);

        public bool Equals(TweenHandle other) => Id == other.Id && Generation == other.Generation;
        public override bool Equals(object obj) => obj is TweenHandle h && Equals(h);
        public override int GetHashCode() => (Id * 397) ^ Generation;
    }
}
