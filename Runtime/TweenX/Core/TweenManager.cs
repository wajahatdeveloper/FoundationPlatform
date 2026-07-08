using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoundationPlatform.TweenX
{
    /// <summary>
    /// Central driver for all tweens. A hidden, <c>DontDestroyOnLoad</c> runner (created via
    /// <see cref="RuntimeInitializeOnLoadMethod"/>, mirroring <c>CoroutineXExecutor</c>) ticks the
    /// presentation clocks every frame; the deterministic clock is ticked externally by whoever
    /// registered it (see <see cref="RegisterClock"/> / <see cref="TickDeterministic"/>).
    ///
    /// <para>Allocation contract: the per-frame tick loop allocates nothing — tweens are pooled per
    /// value type (mirroring GameEngineCore's <c>ActionPool&lt;T&gt;</c>), interpolators are cached
    /// static delegates, and there is no LINQ/closure in the hot path. Creating a tween may allocate
    /// its getter/setter closures the first time (like DOTween); the tween object itself is reused.</para>
    /// </summary>
    public static class TweenManager
    {
        // ---- Active set + id map ----
        private static readonly List<Tween> _active = new(256);
        private static readonly Dictionary<int, Tween> _byId = new(256);
        private static int _idCounter;

        // ---- Per-type pools ----
        private static readonly Dictionary<Type, object> _pools = new();

        // ---- Clocks ----
        private static readonly ITweenClock[] _clocks =
        {
            new UnscaledTweenClock(),  // TweenClock.Unscaled
            new ScaledTweenClock(),    // TweenClock.Scaled
            null,                      // TweenClock.Deterministic — injected via RegisterClock
        };
        private static bool _warnedNoDeterministic;

        /// <summary>Global multiplier applied to all presentation-clock tweens. Does not affect deterministic tweens.</summary>
        public static float GlobalTimeScale = 1f;

        /// <summary>When true, all presentation-clock tweens stop advancing. Deterministic tweens are unaffected.</summary>
        public static bool IsPaused { get; private set; }

        /// <summary>Number of live tweens.</summary>
        public static int ActiveCount => _active.Count;

        // ---------------------------------------------------------------- runner

        private sealed class TweenRunner : MonoBehaviour
        {
            private void Update() => TickPresentation();
            private void OnDestroy() { if (_runner == this) _runner = null; }
        }

        private static TweenRunner _runner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            // Reset statics for domain-reload-disabled play sessions.
            _active.Clear();
            _byId.Clear();
            _idCounter = 0;
            GlobalTimeScale = 1f;
            IsPaused = false;

            if (_runner != null) return;
            var go = new GameObject("TweenRunner") { hideFlags = HideFlags.HideInHierarchy };
            _runner = go.AddComponent<TweenRunner>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        // ---------------------------------------------------------------- clock registration

        /// <summary>
        /// Inject a time source for a clock slot. The higher-level assembly that owns the
        /// deterministic simulation clock calls this to fill <see cref="TweenClock.Deterministic"/>.
        /// </summary>
        public static void RegisterClock(TweenClock slot, ITweenClock clock)
        {
            _clocks[(int)slot] = clock;
        }

        // ---------------------------------------------------------------- creation

        /// <summary>
        /// Build (or reuse from the pool) a typed tween and register it as active. Extension methods
        /// call this, then set <see cref="Tween.TargetObject"/>/<see cref="Tween.Clock"/> and return
        /// <see cref="AsHandle"/>. Not usually called directly by gameplay code.
        /// </summary>
        internal static Tween<T> Create<T>(
            Func<T> getter, Action<T> setter, T to, float duration,
            Interpolator<T> lerp, Adder<T> add = null, Func<T, bool, T> snapper = null,
            UnityEngine.Object target = null, TweenClock clock = TweenClock.Scaled)
        {
            EnsureRunner();
            var t = Rent<T>();
            t.Getter = getter;
            t.Setter = setter;
            t.EndValue = to;
            t.Duration = Mathf.Max(0f, duration);
            t.Lerp = lerp;
            t.Add = add;
            t.Snapper = snapper;
            t.Clock = clock;
            t.TargetObject = target;
            t.HasTarget = target != null;
            t.IsAlive = true;

            t.Id = ++_idCounter;
            _byId[t.Id] = t;
            _active.Add(t);
            return t;
        }

        /// <summary>Create the runner on demand (e.g. tweens started before <see cref="Bootstrap"/> in edit-mode preview).</summary>
        private static void EnsureRunner()
        {
            if (_runner != null || !Application.isPlaying) return;
            var go = new GameObject("TweenRunner") { hideFlags = HideFlags.HideInHierarchy };
            _runner = go.AddComponent<TweenRunner>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        internal static TweenHandle AsHandle(this Tween t) => new(t.Id, t.Generation);

        private static Tween<T> Rent<T>()
        {
            if (_pools.TryGetValue(typeof(Tween<T>), out var poolObj))
            {
                var stack = (Stack<Tween<T>>)poolObj;
                if (stack.Count > 0) return stack.Pop();
            }
            return new Tween<T>();
        }

        private static void Return(Tween t)
        {
            t.Reset();
            unchecked { t.Generation++; }   // invalidate any surviving handles
            t.ReturnToPool(_pools);         // type-safe push, keyed by concrete Tween<T>
        }

        // ---------------------------------------------------------------- ticking

        /// <summary>Advance all presentation-clock (Unscaled/Scaled) tweens by one frame.</summary>
        public static void TickPresentation()
        {
            if (IsPaused || _active.Count == 0) return;

            float scaledDt = Time.deltaTime * GlobalTimeScale;
            float unscaledDt = Time.unscaledDeltaTime * GlobalTimeScale;

            for (int idx = _active.Count - 1; idx >= 0; idx--)
            {
                var t = _active[idx];
                if (t.Clock == TweenClock.Deterministic) continue;

                float baseDt = t.Clock == TweenClock.Unscaled ? unscaledDt : scaledDt;
                if (!t.Step(baseDt * t.TimeScale)) RemoveAt(idx, killed: true);
            }
        }

        /// <summary>
        /// Advance all deterministic-clock tweens. Called once per simulation step by the registered
        /// bridge. Ignores global pause/time-scale so results stay reproducible.
        /// </summary>
        public static void TickDeterministic()
        {
            var clock = _clocks[(int)TweenClock.Deterministic];
            if (clock == null || _active.Count == 0) return;

            float dt = clock.DeltaTime;
            for (int idx = _active.Count - 1; idx >= 0; idx--)
            {
                var t = _active[idx];
                if (t.Clock != TweenClock.Deterministic) continue;
                if (!t.Step(dt * t.TimeScale)) RemoveAt(idx, killed: true);
            }
        }

        private static void RemoveAt(int index, bool killed)
        {
            var t = _active[index];
            if (killed) t.OnKillCb?.Invoke();
            _byId.Remove(t.Id);

            int last = _active.Count - 1;
            _active[index] = _active[last];
            _active.RemoveAt(last);

            t.IsAlive = false;
            Return(t);
        }

        // ---------------------------------------------------------------- resolution / validation

        private static Tween Resolve(in TweenHandle h)
        {
            if (h.Id == 0) return null;
            if (_byId.TryGetValue(h.Id, out var t) && t.Generation == h.Generation && t.IsAlive) return t;
            return null;
        }

        internal static bool IsActive(in TweenHandle h) => Resolve(h) != null;
        internal static float GetProgress(in TweenHandle h) => Resolve(h)?.Progress ?? 0f;

        // ---------------------------------------------------------------- fluent setters (handle)

        internal static void SetEase(in TweenHandle h, Ease e) { var t = Resolve(h); if (t != null) { t.Ease = e; t.Curve = null; } }
        internal static void SetEase(in TweenHandle h, AnimationCurve c) { var t = Resolve(h); if (t != null) t.Curve = c; }
        internal static void SetLoops(in TweenHandle h, int count, LoopType type) { var t = Resolve(h); if (t != null) { t.LoopCount = count; t.LoopType = type; } }
        internal static void SetDelay(in TweenHandle h, float s) { var t = Resolve(h); if (t != null && !t.Started) { t.Delay = s; t.DelayRemaining = s; } }
        internal static void SetClock(in TweenHandle h, TweenClock c)
        {
            var t = Resolve(h);
            if (t == null) return;
            if (c == TweenClock.Deterministic && _clocks[(int)TweenClock.Deterministic] == null)
            {
                if (!_warnedNoDeterministic)
                {
                    Debug.LogWarning("[TweenX] Deterministic clock requested but none registered; falling back to Scaled. " +
                                     "Ensure the simulation bridge is present (GameEngineCore.SimulationClockTweenBridge).");
                    _warnedNoDeterministic = true;
                }
                c = TweenClock.Scaled;
            }
            t.Clock = c;
        }
        internal static void SetTimeScale(in TweenHandle h, float s) { var t = Resolve(h); if (t != null) t.TimeScale = s; }
        internal static void SetSnapping(in TweenHandle h, bool on) { var t = Resolve(h); if (t != null) t.Snapping = on; }
        internal static void SetRelative(in TweenHandle h, bool on) { var t = Resolve(h); if (t != null && !t.Started) t.Relative = on; }
        internal static void SetFromCurrentSwap(in TweenHandle h) { var t = Resolve(h); if (t != null && !t.Started) t.FromMode = true; }
        internal static void SetGroupId(in TweenHandle h, int id) { var t = Resolve(h); if (t != null) t.GroupId = id; }
        internal static void SetLink(in TweenHandle h, GameObject go) { var t = Resolve(h); if (t != null) { t.LinkedObject = go; t.HasLink = go != null; } }

        internal static void SetOnStart(in TweenHandle h, Action cb) { var t = Resolve(h); if (t != null) t.OnStartCb = cb; }
        internal static void SetOnUpdate(in TweenHandle h, Action cb) { var t = Resolve(h); if (t != null) t.OnUpdateCb = cb; }
        internal static void SetOnStepComplete(in TweenHandle h, Action cb) { var t = Resolve(h); if (t != null) t.OnStepCompleteCb = cb; }
        internal static void SetOnComplete(in TweenHandle h, Action cb) { var t = Resolve(h); if (t != null) t.OnCompleteCb = cb; }
        internal static void SetOnKill(in TweenHandle h, Action cb) { var t = Resolve(h); if (t != null) t.OnKillCb = cb; }

        // ---------------------------------------------------------------- playback control (handle)

        internal static void Pause(in TweenHandle h) { var t = Resolve(h); if (t != null) t.Paused = true; }
        internal static void Play(in TweenHandle h) { var t = Resolve(h); if (t != null) t.Paused = false; }

        internal static void Complete(in TweenHandle h)
        {
            var t = Resolve(h);
            if (t == null) return;
            bool backward = t.LoopType == LoopType.Yoyo && (t.LoopsDone & 1) == 1;
            t.SnapToEnd(backward);
            t.OnStepCompleteCb?.Invoke();
            t.OnCompleteCb?.Invoke();
            KillResolved(t);
        }

        internal static void Rewind(in TweenHandle h)
        {
            var t = Resolve(h);
            if (t == null) return;
            t.SnapToStart();
            t.Elapsed = 0f;
            t.LoopsDone = 0;
            t.DelayRemaining = t.Delay;
            t.Started = false;
            t.Paused = true;
        }

        internal static void Kill(in TweenHandle h)
        {
            var t = Resolve(h);
            if (t != null) KillResolved(t);
        }

        private static void KillResolved(Tween t)
        {
            int idx = _active.IndexOf(t);
            if (idx >= 0) RemoveAt(idx, killed: true);
        }

        // ---------------------------------------------------------------- bulk control

        /// <summary>Kill every live tween. Fires their OnKill callbacks.</summary>
        public static void KillAll()
        {
            for (int idx = _active.Count - 1; idx >= 0; idx--) RemoveAt(idx, killed: true);
        }

        /// <summary>Kill all tweens tagged with <paramref name="id"/> via <c>SetId</c>. Returns the count killed.</summary>
        public static int KillById(int id)
        {
            int n = 0;
            for (int idx = _active.Count - 1; idx >= 0; idx--)
                if (_active[idx].GroupId == id) { RemoveAt(idx, killed: true); n++; }
            return n;
        }

        /// <summary>Kill all tweens whose animated target is <paramref name="target"/>. Returns the count killed.</summary>
        public static int KillTweensOf(UnityEngine.Object target)
        {
            if (target == null) return 0;
            int n = 0;
            for (int idx = _active.Count - 1; idx >= 0; idx--)
                if (_active[idx].TargetObject == target) { RemoveAt(idx, killed: true); n++; }
            return n;
        }

        /// <summary>Pause every presentation-clock tween globally.</summary>
        public static void PauseAll() => IsPaused = true;

        /// <summary>Resume global presentation ticking.</summary>
        public static void ResumeAll() => IsPaused = false;

        // ---------------------------------------------------------------- editor / debug read API

        /// <summary>Lightweight snapshot of a live tween for editor/debug display (no internals leaked).</summary>
        public struct TweenInfo
        {
            public int Id;
            public int Generation;
            public UnityEngine.Object Target;
            public string TargetName;
            public string ValueType;
            public float Progress;
            public TweenClock Clock;
            public bool Paused;
            public int LoopsDone;
            public int LoopCount;
            public float Duration;
            public float Elapsed;
            public float DelayRemaining;
        }

        /// <summary>Fill <paramref name="buffer"/> with a snapshot of all live tweens. Optionally filter by target.</summary>
        public static void GetActive(List<TweenInfo> buffer, UnityEngine.Object filterTarget = null)
        {
            buffer.Clear();
            for (int i = 0; i < _active.Count; i++)
            {
                var t = _active[i];
                if (filterTarget != null && t.TargetObject != filterTarget) continue;
                buffer.Add(new TweenInfo
                {
                    Id = t.Id,
                    Generation = t.Generation,
                    Target = t.TargetObject,
                    TargetName = t.TargetObject != null ? t.TargetObject.name : "(no target)",
                    ValueType = ValueTypeName(t),
                    Progress = t.Progress,
                    Clock = t.Clock,
                    Paused = t.Paused,
                    LoopsDone = t.LoopsDone,
                    LoopCount = t.LoopCount,
                    Duration = t.Duration,
                    Elapsed = t.Elapsed,
                    DelayRemaining = t.DelayRemaining,
                });
            }
        }

        /// <summary>Rebuild a control handle from a snapshot (for editor kill/pause buttons).</summary>
        public static TweenHandle HandleOf(in TweenInfo info) => new(info.Id, info.Generation);

        private static string ValueTypeName(Tween t)
        {
            var gt = t.GetType();
            return gt.IsGenericType ? gt.GetGenericArguments()[0].Name : gt.Name;
        }
    }
}
