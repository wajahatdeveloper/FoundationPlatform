using System;
using System.Collections.Generic;
using FoundationPlatform.FrameworkInspector;
using UnityEngine;

namespace FoundationPlatform.TweenX
{
    /// <summary>
    /// Designer-facing tween component: author a list of <see cref="TweenStep"/>s on a GameObject and
    /// play them with no code. Steps run sequentially by default; tick <see cref="TweenStep.JoinPrevious"/>
    /// to start a step at the same time as the one before it (parallel). Under the hood each step spawns
    /// a normal tween with a computed start delay, so it shares the exact same engine, clocks, and
    /// scene-view debugging as code-driven tweens.
    /// </summary>
    [AddComponentMenu("FoundationPlatform/Tween Animator")]
    [DisallowMultipleComponent]
    public sealed class TweenAnimator : MonoBehaviour
    {
        /// <summary>Which property of this GameObject a step drives.</summary>
        public enum TweenTarget
        {
            Move, LocalMove, Scale, Rotate, AnchorPos, Fade, Color,
        }

        [Serializable]
        public sealed class TweenStep
        {
            [LabelText("Property")]
            [EnumToggleButtons]
            public TweenTarget Target = TweenTarget.Move;

            [Tooltip("Start this step at the same time as the previous one (parallel), instead of after it.")]
            public bool JoinPrevious;

            [MinValue(0f)]
            public float Duration = 1f;

            [MinValue(0f)]
            public float Delay;

            // Destination — only the relevant field shows, driven by Target (predicate methods below).
            [ShowIf("UsesVector3")]
            [LabelText("To (Vector3)")]
            public Vector3 ToVector = Vector3.one;

            [ShowIf("UsesVector2")]
            [LabelText("To (Anchored)")]
            public Vector2 ToVector2;

            [ShowIf("UsesFloat")]
            [LabelText("To (Alpha)")]
            [Range(0f, 1f)]
            public float ToFloat = 1f;

            [ShowIf("UsesColor")]
            [LabelText("To (Color)")]
            public Color ToColor = UnityEngine.Color.white;

            // Easing — enum or custom curve.
            public bool UseCurve;

            [HideIf("UseCurve")]
            public Ease Ease = Ease.OutQuad;

            [ShowIf("UseCurve")]
            public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

            [MinValue(-1)]
            [Tooltip("Total plays. 1 = once, -1 = infinite.")]
            public int Loops = 1;

            [ShowIf("IsLooping")]
            public LoopType LoopType = LoopType.Restart;

            /// <summary>Animate FROM <c>To*</c> toward the current value (rather than toward <c>To*</c>).</summary>
            public bool From;

            // Inspector predicates for [ShowIf] — the parity resolver calls these parameterless bools.
            internal bool UsesVector3() => Target is TweenTarget.Move or TweenTarget.LocalMove or TweenTarget.Scale or TweenTarget.Rotate;
            internal bool UsesVector2() => Target == TweenTarget.AnchorPos;
            internal bool UsesFloat() => Target == TweenTarget.Fade;
            internal bool UsesColor() => Target == TweenTarget.Color;
            internal bool IsLooping() => Loops != 1;
        }

        [Title("Playback")]
        [Tooltip("Play automatically when the component is enabled.")]
        public bool PlayOnEnable = true;

        [Tooltip("Time source for every step. Unscaled ignores pause/slow-mo; Deterministic uses the sim clock when available.")]
        public TweenClock Clock = TweenClock.Scaled;

        [Tooltip("Snap positional values to whole units (pixel-perfect).")]
        public bool Snapping;

        [InfoBox("No steps yet. Add one to animate this GameObject.", InfoMessageType.Warning, "@Steps == null || Steps.Count == 0")]
        [Title("Steps")]
        public List<TweenStep> Steps = new();

        private readonly List<TweenHandle> _live = new();

        private void OnEnable()
        {
            if (PlayOnEnable) Play();
        }

        private void OnDisable()
        {
            Stop();
        }

        /// <summary>Start playing all steps. Any steps already running are killed first.</summary>
        [Button("Play", ButtonHeight = ButtonSizes.Medium)]
        public void Play()
        {
            Stop();
            if (Steps == null) return;

            float cursor = 0f;         // running start-time for sequential steps
            float prevStart = 0f;
            for (int i = 0; i < Steps.Count; i++)
            {
                var step = Steps[i];
                float start = step.JoinPrevious && i > 0 ? prevStart : cursor;
                var handle = BuildStep(step, start);
                if (handle.IsActive) _live.Add(handle);

                prevStart = start;
                if (!step.JoinPrevious) cursor = start + step.Delay + step.Duration;
                else cursor = Mathf.Max(cursor, start + step.Delay + step.Duration);
            }
        }

        /// <summary>Kill every step this animator started.</summary>
        [Button("Stop", ButtonHeight = ButtonSizes.Medium)]
        public void Stop()
        {
            for (int i = 0; i < _live.Count; i++) _live[i].Kill();
            _live.Clear();
        }

        private TweenHandle BuildStep(TweenStep step, float startDelay)
        {
            TweenHandle h = default;
            switch (step.Target)
            {
                case TweenTarget.Move: h = transform.TweenMove(step.ToVector, step.Duration); break;
                case TweenTarget.LocalMove: h = transform.TweenLocalMove(step.ToVector, step.Duration); break;
                case TweenTarget.Scale: h = transform.TweenScale(step.ToVector, step.Duration); break;
                case TweenTarget.Rotate: h = transform.TweenRotate(step.ToVector, step.Duration); break;
                case TweenTarget.AnchorPos:
                    if (transform is RectTransform rt) h = rt.TweenAnchorPos(step.ToVector2, step.Duration);
                    break;
                case TweenTarget.Fade:
                    if (TryGetComponent(out CanvasGroup cg)) h = cg.TweenFade(step.ToFloat, step.Duration);
                    else if (TryGetComponent(out UnityEngine.UI.Graphic g)) h = g.TweenFade(step.ToFloat, step.Duration);
                    else if (TryGetComponent(out SpriteRenderer sr)) h = sr.TweenFade(step.ToFloat, step.Duration);
                    break;
                case TweenTarget.Color:
                    if (TryGetComponent(out UnityEngine.UI.Graphic gc)) h = gc.TweenColor(step.ToColor, step.Duration);
                    else if (TryGetComponent(out SpriteRenderer sr2)) h = sr2.TweenColor(step.ToColor, step.Duration);
                    break;
            }

            if (!h.IsActive) return h;

            if (step.UseCurve && step.Curve != null) h.SetEase(step.Curve);
            else h.SetEase(step.Ease);
            h.SetClock(Clock)
             .SetSnapping(Snapping)
             .SetDelay(step.Delay + startDelay)
             .SetLoops(step.Loops, step.LoopType);
            if (step.From) h.From();
            return h;
        }
    }
}
