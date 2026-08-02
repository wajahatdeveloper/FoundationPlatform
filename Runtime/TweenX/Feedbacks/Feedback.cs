using System;
using System.Collections.Generic;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.TweenX.Feedbacks
{
    /// <summary>
    /// Shared state passed to every feedback when a <see cref="FeedbackPlayer"/> plays. Owns the tween
    /// handles the feedbacks spawn so the player can stop them all, and provides delayed scheduling for
    /// non-tween feedbacks (audio, events, time-freeze).
    /// </summary>
    public sealed class FeedbackContext
    {
        public GameObject Owner;
        public Transform Transform;
        public TweenClock Clock = TweenClock.Scaled;

        private readonly List<TweenHandle> _handles = new(16);
        private readonly List<Action> _mustRunOnStop = new(4);

        internal void Begin(GameObject owner, TweenClock clock)
        {
            Owner = owner;
            Transform = owner != null ? owner.transform : null;
            Clock = clock;
            _handles.Clear();
            _mustRunOnStop.Clear();
        }

        /// <summary>Register a spawned tween so <see cref="Stop"/> can kill it.</summary>
        public TweenHandle Track(TweenHandle h)
        {
            if (h.IsActive) _handles.Add(h);
            return h;
        }

        /// <summary>
        /// Register a cleanup that must run even if this context is stopped before its tween(s)
        /// complete naturally (e.g. restoring global state a feedback mutated, like <see cref="Time.timeScale"/>).
        /// Killing a tracked tween only fires its kill callback, never its complete callback, so anything
        /// that MUST run on both paths (complete or kill) belongs here, not in <see cref="Schedule"/>'s
        /// completion action alone.
        /// </summary>
        public void RegisterCleanup(Action cleanup)
        {
            if (cleanup != null) _mustRunOnStop.Add(cleanup);
        }

        /// <summary>Remove a cleanup previously registered via <see cref="RegisterCleanup"/> — call this once it has run naturally.</summary>
        public void UnregisterCleanup(Action cleanup) => _mustRunOnStop.Remove(cleanup);

        /// <summary>Run <paramref name="action"/> after <paramref name="delay"/> seconds via a zero-length tween.</summary>
        public void Schedule(float delay, Action action, bool unscaled = false)
        {
            if (action == null) return;
            if (delay <= 0f) { action(); return; }
            var h = TweenValue.Float(() => 0f, _ => { }, 0f, 0f)
                .SetDelay(delay)
                .SetClock(unscaled ? TweenClock.Unscaled : Clock)
                .OnComplete(action);
            Track(h);
        }

        /// <summary>Kill every tween spawned by the feedbacks this context served.</summary>
        public void Stop()
        {
            // Run must-run cleanups before killing handles: killing a tween only fires its kill
            // callback, never its complete callback, so a cleanup relying on OnComplete would
            // otherwise be silently dropped here.
            if (_mustRunOnStop.Count > 0)
            {
                var cleanups = _mustRunOnStop.ToArray();
                _mustRunOnStop.Clear();
                for (int i = 0; i < cleanups.Length; i++) cleanups[i]();
            }
            for (int i = 0; i < _handles.Count; i++) _handles[i].Kill();
            _handles.Clear();
        }
    }

    /// <summary>
    /// One composable effect in a <see cref="FeedbackPlayer"/>. Subclass and implement
    /// <see cref="Execute"/>; the base handles the enabled toggle and per-feedback delay. Concrete
    /// subclasses are discovered automatically by the editor's Add-Feedback dropdown (via TypeCache),
    /// so adding a new feedback type is drop-in — no registration, no central switch.
    /// </summary>
    [Serializable]
    public abstract class Feedback
    {
        [Tooltip("Uncheck to skip this feedback without deleting it.")]
        public bool Active = true;

        [Tooltip("Optional label shown in the inspector list.")]
        public string Label;

        [Tooltip("Seconds to wait after Play() before this feedback fires.")]
        [Min(0f)]
        public float Delay;

        /// <summary>Friendly name for the inspector row (defaults to the type name, de-suffixed).</summary>
        public virtual string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(Label)) return Label;
                string n = GetType().Name;
                return n.StartsWith("Feedback") ? n.Substring("Feedback".Length) : n;
            }
        }

        /// <summary>Called by the player. Applies the delay, then runs <see cref="Execute"/>.</summary>
        public void Play(FeedbackContext ctx)
        {
            if (!Active) return;
            if (Delay > 0f) ctx.Schedule(Delay, () => Execute(ctx));
            else Execute(ctx);
        }

        /// <summary>Do the actual work: spawn tweens (track them via <paramref name="ctx"/>), play audio, etc.</summary>
        protected abstract void Execute(FeedbackContext ctx);

        /// <summary>Resolve the transform this feedback acts on — an explicit override or the owner's.</summary>
        protected static Transform ResolveTransform(FeedbackContext ctx, Transform explicitTarget)
            => explicitTarget != null ? explicitTarget : ctx.Transform;
    }
}
