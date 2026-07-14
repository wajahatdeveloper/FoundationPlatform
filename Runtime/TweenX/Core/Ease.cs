using UnityEngine;

namespace AetherNexus.FoundationPlatform.TweenX
{
    /// <summary>
    /// Easing curve applied to a tween's normalized time. Names mirror the industry-standard
    /// easings.net vocabulary so they read the same as DOTween. Actual formulas live in
    /// <c>MathX</c> (the project's shared easing library) — this enum just selects one.
    /// For a fully custom shape, use an <see cref="AnimationCurve"/> instead (see
    /// <c>Tween.SetEase(AnimationCurve)</c>).
    /// </summary>
    public enum Ease
    {
        Linear,
        InQuad, OutQuad, InOutQuad,
        InCubic, OutCubic, InOutCubic,
        InQuart, OutQuart, InOutQuart,
        InQuint, OutQuint, InOutQuint,
        InSine, OutSine, InOutSine,
        InExpo, OutExpo, InOutExpo,
        InCirc, OutCirc, InOutCirc,
        InBack, OutBack, InOutBack,
        InElastic, OutElastic, InOutElastic,
        InBounce, OutBounce, InOutBounce,
    }

    /// <summary>
    /// Maps an <see cref="Ease"/> to its shaping function. Uses a plain <c>switch</c> (no
    /// dictionary, no delegate allocation) so evaluation is branch-cheap and allocation-free
    /// on the tween hot path. All formulas are pure functions of <paramref name="t"/> in [0,1],
    /// which is what keeps <see cref="TweenClock.Deterministic"/> tweens reproducible.
    /// </summary>
    public static class EaseEvaluator
    {
        /// <summary>Shape a normalized time <paramref name="t"/> (expected 0..1) by <paramref name="ease"/>.</summary>
        public static float Evaluate(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.Linear: return MathX.Linear(t);
                case Ease.InQuad: return MathX.EaseInQuad(t);
                case Ease.OutQuad: return MathX.EaseOutQuad(t);
                case Ease.InOutQuad: return MathX.EaseInOutQuad(t);
                case Ease.InCubic: return MathX.EaseInCubic(t);
                case Ease.OutCubic: return MathX.EaseOutCubic(t);
                case Ease.InOutCubic: return MathX.EaseInOutCubic(t);
                case Ease.InQuart: return MathX.EaseInQuart(t);
                case Ease.OutQuart: return MathX.EaseOutQuart(t);
                case Ease.InOutQuart: return MathX.EaseInOutQuart(t);
                case Ease.InQuint: return MathX.EaseInQuint(t);
                case Ease.OutQuint: return MathX.EaseOutQuint(t);
                case Ease.InOutQuint: return MathX.EaseInOutQuint(t);
                case Ease.InSine: return MathX.EaseInSine(t);
                case Ease.OutSine: return MathX.EaseOutSine(t);
                case Ease.InOutSine: return MathX.EaseInOutSine(t);
                case Ease.InExpo: return MathX.EaseInExpo(t);
                case Ease.OutExpo: return MathX.EaseOutExpo(t);
                case Ease.InOutExpo: return MathX.EaseInOutExpo(t);
                case Ease.InCirc: return MathX.EaseInCirc(t);
                case Ease.OutCirc: return MathX.EaseOutCirc(t);
                case Ease.InOutCirc: return MathX.EaseInOutCirc(t);
                case Ease.InBack: return MathX.EaseInBack(t);
                case Ease.OutBack: return MathX.EaseOutBack(t);
                case Ease.InOutBack: return MathX.EaseInOutBack(t);
                case Ease.InElastic: return MathX.EaseInElastic(t);
                case Ease.OutElastic: return MathX.EaseOutElastic(t);
                case Ease.InOutElastic: return MathX.EaseInOutElastic(t);
                case Ease.InBounce: return MathX.EaseInBounce(t);
                case Ease.OutBounce: return MathX.EaseOutBounce(t);
                case Ease.InOutBounce: return MathX.EaseInOutBounce(t);
                default: return t;
            }
        }
    }
}
