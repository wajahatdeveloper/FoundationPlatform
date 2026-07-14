using System;
using System.Collections.Generic;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.TweenX
{
    /// <summary>
    /// A timeline that composes tweens, intervals, and callbacks into one controllable unit. Build it
    /// fluently, then it ticks like a single tween:
    /// <code>
    /// Sequence.Create()
    ///     .Append(transform.TweenMove(a, 1f).SetEase(Ease.OutQuad))
    ///     .Join(transform.TweenScale(1.5f, 1f))       // parallel with the move
    ///     .AppendInterval(0.25f)
    ///     .AppendCallback(() =&gt; Fire())
    ///     .SetLoops(-1)
    ///     .Play();
    /// </code>
    /// Child tweens are <em>adopted</em> out of the manager's active set and driven off the sequence's
    /// own playhead (their individual delay/loops/clock are ignored — use <see cref="AppendInterval"/>
    /// and the sequence's own loop/clock instead). Yoyo looping isn't supported at the sequence level;
    /// <see cref="SetLoops"/> always restarts.
    /// </summary>
    public sealed class Sequence : Tween
    {
        private struct Entry
        {
            public Tween Tween;
            public float Start;
            public Action Callback;
            public bool IsCallback;
            public bool Fired;
        }

        private readonly List<Entry> _entries = new(8);
        private float _playhead;
        private float _buildCursor;    // where the next Append lands
        private float _lastEntryStart; // start of the last Append/Insert, for Join

        /// <summary>Create an empty sequence, already registered and ready to build.</summary>
        public static Sequence Create()
        {
            var seq = TweenManager.RentPooled<Sequence>();
            TweenManager.Register(seq);
            return seq;
        }

        // ---------------------------------------------------------------- building

        // Adopt a child tween out of the manager, rejecting nested sequences (not supported —
        // a Sequence is driven by its own Step, not by SampleAt, so it can't be a child).
        private Tween AdoptChild(TweenHandle handle)
        {
            var t = TweenManager.Adopt(handle);
            if (t is Sequence)
            {
                Debug.LogWarning("[TweenX] Nesting a Sequence inside another Sequence is not supported; ignored.");
                TweenManager.RecycleDetached(t);
                return null;
            }
            return t;
        }

        /// <summary>Add a tween after everything appended so far.</summary>
        public Sequence Append(TweenHandle handle)
        {
            var t = AdoptChild(handle);
            if (t == null) return this;
            _lastEntryStart = _buildCursor;
            _entries.Add(new Entry { Tween = t, Start = _buildCursor });
            _buildCursor += t.Duration;
            Duration = Mathf.Max(Duration, _buildCursor);
            return this;
        }

        /// <summary>Add a tween that starts together with the previous Append/Insert (parallel).</summary>
        public Sequence Join(TweenHandle handle)
        {
            var t = AdoptChild(handle);
            if (t == null) return this;
            _entries.Add(new Entry { Tween = t, Start = _lastEntryStart });
            _buildCursor = Mathf.Max(_buildCursor, _lastEntryStart + t.Duration);
            Duration = Mathf.Max(Duration, _buildCursor);
            return this;
        }

        /// <summary>Insert a tween at an absolute time. Does not move the append cursor.</summary>
        public Sequence Insert(float atTime, TweenHandle handle)
        {
            var t = AdoptChild(handle);
            if (t == null) return this;
            _lastEntryStart = atTime;
            _entries.Add(new Entry { Tween = t, Start = atTime });
            Duration = Mathf.Max(Duration, atTime + t.Duration);
            return this;
        }

        /// <summary>Advance the append cursor by <paramref name="seconds"/> (empty gap).</summary>
        public Sequence AppendInterval(float seconds)
        {
            _buildCursor += Mathf.Max(0f, seconds);
            Duration = Mathf.Max(Duration, _buildCursor);
            return this;
        }

        /// <summary>Fire a callback when the playhead reaches the current append cursor.</summary>
        public Sequence AppendCallback(Action callback)
        {
            if (callback != null)
                _entries.Add(new Entry { Callback = callback, Start = _buildCursor, IsCallback = true });
            return this;
        }

        /// <summary>Fire a callback at an absolute time.</summary>
        public Sequence InsertCallback(float atTime, Action callback)
        {
            if (callback != null)
                _entries.Add(new Entry { Callback = callback, Start = atTime, IsCallback = true });
            Duration = Mathf.Max(Duration, atTime);
            return this;
        }

        // ---------------------------------------------------------------- fluent config

        public Sequence SetLoops(int count) { LoopCount = count; LoopType = LoopType.Restart; return this; }
        public Sequence SetClock(TweenClock clock) { Clock = clock; return this; }
        public Sequence SetTimeScale(float scale) { TimeScale = scale; return this; }
        public Sequence SetId(int id) { GroupId = id; return this; }
        public Sequence SetLink(GameObject go) { LinkedObject = go; HasLink = go != null; return this; }
        public Sequence OnStart(Action cb) { OnStartCb = cb; return this; }
        public Sequence OnUpdate(Action cb) { OnUpdateCb = cb; return this; }
        public Sequence OnStepComplete(Action cb) { OnStepCompleteCb = cb; return this; }
        public Sequence OnComplete(Action cb) { OnCompleteCb = cb; return this; }
        public Sequence OnKill(Action cb) { OnKillCb = cb; return this; }

        /// <summary>The control handle for this sequence (Pause/Play/Kill/etc).</summary>
        public TweenHandle Handle => new(Id, Generation);

        public Sequence Play() { Paused = false; return this; }
        public Sequence Pause() { Paused = true; return this; }
        public void Kill() => Handle.Kill();

        // ---------------------------------------------------------------- ticking

        internal override bool Step(float dt)
        {
            if (!IsAlive) return false;
            if (Paused) return true;
            if (HasLink && (LinkedObject == null || !LinkedObject.activeInHierarchy)) return false;

            if (!Started) { Started = true; OnStartCb?.Invoke(); }

            _playhead += dt;
            DriveTo(_playhead);
            OnUpdateCb?.Invoke();

            if (_playhead < Duration) return true;

            OnStepCompleteCb?.Invoke();
            bool moreLoops = LoopCount < 0 || LoopsDone < LoopCount - 1;
            if (moreLoops)
            {
                LoopsDone++;
                _playhead = 0f;
                ResetChildren();
                return true;
            }

            OnCompleteCb?.Invoke();
            return false;
        }

        private void DriveTo(float time)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.IsCallback)
                {
                    if (!e.Fired && time >= e.Start)
                    {
                        e.Callback?.Invoke();
                        e.Fired = true;
                        _entries[i] = e;
                    }
                    continue;
                }

                if (time < e.Start) continue;   // segment hasn't begun — leave target alone
                float local = Mathf.Min(time - e.Start, e.Tween.Duration);
                e.Tween.SampleAt(local);
            }
        }

        private void ResetChildren()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                e.Fired = false;
                if (e.Tween != null) e.Tween.Started = false;   // re-capture From next cycle
                _entries[i] = e;
            }
        }

        // ---------------------------------------------------------------- Tween overrides

        internal override void ResolveStartValues() { }
        internal override void ApplyEased(float easedFactor) { }
        internal override void OnIncrementLoop() { }
        internal override void SnapToStart() { _playhead = 0f; ResetChildren(); }
        internal override void SnapToEnd(bool backward) { DriveTo(Duration); }

        internal override void ReturnToPool(Dictionary<Type, object> pools)
        {
            if (!pools.TryGetValue(typeof(Sequence), out var s))
            {
                s = new Stack<Sequence>();
                pools[typeof(Sequence)] = s;
            }
            ((Stack<Sequence>)s).Push(this);
        }

        internal override void Reset()
        {
            // Recycle adopted children back to their own pools before clearing.
            for (int i = 0; i < _entries.Count; i++)
                if (_entries[i].Tween != null) TweenManager.RecycleDetached(_entries[i].Tween);
            _entries.Clear();
            _playhead = 0f;
            _buildCursor = 0f;
            _lastEntryStart = 0f;
            base.Reset();
        }
    }
}
