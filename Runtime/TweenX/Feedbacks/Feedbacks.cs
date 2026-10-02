using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AetherNexus.FoundationPlatform.TweenX.Feedbacks
{
    /// <summary>Move the transform to a target position.</summary>
    [Serializable]
    public sealed class FeedbackMove : Feedback
    {
        public Transform Target;
        public Vector3 To;
        public bool Local;
        public bool Relative;
        [Min(0f)] public float Duration = 0.3f;
        public Ease Ease = Ease.OutQuad;

        protected override float ActiveDuration => Duration;

        protected override void Execute(FeedbackContext ctx)
        {
            var tr = ResolveTransform(ctx, Target);
            if (tr == null) return;
            var h = (Local ? tr.TweenLocalMove(To, Duration) : tr.TweenMove(To, Duration))
                .SetEase(Ease).SetClock(ctx.Clock);
            if (Relative) h.SetRelative();
            ctx.Track(h);
        }
    }

    /// <summary>Squash-punch the transform's scale, settling back to its original scale.</summary>
    [Serializable]
    public sealed class FeedbackScalePunch : Feedback
    {
        public Transform Target;
        public Vector3 Punch = Vector3.one * 0.2f;
        [Min(0f)] public float Duration = 0.3f;
        [Min(1f)] public float Vibrato = 10f;

        protected override float ActiveDuration => Duration;

        protected override void Execute(FeedbackContext ctx)
        {
            var tr = ResolveTransform(ctx, Target);
            if (tr == null) return;
            ctx.Track(tr.TweenPunchScale(Punch, Duration, Vibrato).SetClock(ctx.Clock));
        }
    }

    /// <summary>Punch the transform's rotation (Euler), settling back.</summary>
    [Serializable]
    public sealed class FeedbackPunchRotation : Feedback
    {
        public Transform Target;
        public Vector3 Punch = new(0f, 0f, 15f);
        [Min(0f)] public float Duration = 0.3f;
        [Min(1f)] public float Vibrato = 10f;

        protected override float ActiveDuration => Duration;

        protected override void Execute(FeedbackContext ctx)
        {
            var tr = ResolveTransform(ctx, Target);
            if (tr == null) return;
            ctx.Track(tr.TweenPunchRotation(Punch, Duration, Vibrato).SetClock(ctx.Clock));
        }
    }

    /// <summary>Shake the transform's local position with decaying jitter.</summary>
    [Serializable]
    public sealed class FeedbackShakePosition : Feedback
    {
        public Transform Target;
        public Vector3 Strength = Vector3.one * 0.3f;
        [Min(0f)] public float Duration = 0.5f;
        [Min(1f)] public float Vibrato = 10f;
        [Tooltip("-1 = auto (varies each play); set >=0 for a reproducible shake.")]
        public int Seed = -1;

        protected override float ActiveDuration => Duration;

        protected override void Execute(FeedbackContext ctx)
        {
            var tr = ResolveTransform(ctx, Target);
            if (tr == null) return;
            ctx.Track(tr.TweenShakePosition(Strength, Duration, Vibrato, Seed).SetClock(ctx.Clock));
        }
    }

    /// <summary>Pulse a UI Graphic or SpriteRenderer color and back.</summary>
    [Serializable]
    public sealed class FeedbackFlash : Feedback
    {
        public Graphic Graphic;
        public SpriteRenderer Sprite;
        public Color FlashColor = Color.white;
        [Min(0f)] public float Duration = 0.2f;
        [Min(1)] public int Flashes = 1;

        protected override float ActiveDuration => Duration;

        protected override void Execute(FeedbackContext ctx)
        {
            if (Graphic != null) ctx.Track(Graphic.TweenFlash(FlashColor, Duration, Flashes).SetClock(ctx.Clock));
            else if (Sprite != null) ctx.Track(Sprite.TweenFlash(FlashColor, Duration, Flashes).SetClock(ctx.Clock));
        }
    }

    /// <summary>Fade a CanvasGroup's alpha.</summary>
    [Serializable]
    public sealed class FeedbackFade : Feedback
    {
        public CanvasGroup CanvasGroup;
        [Tooltip("Snap alpha to From when this feedback fires, so the fade always starts from the same value.")]
        public bool StartFromValue;
        [Range(0f, 1f)] public float From;
        [Range(0f, 1f)] public float To;
        [Min(0f)] public float Duration = 0.25f;
        public Ease Ease = Ease.OutQuad;

        protected override float ActiveDuration => Duration;

        protected override void Execute(FeedbackContext ctx)
        {
            var cg = CanvasGroup != null ? CanvasGroup : (ctx.Owner != null ? ctx.Owner.GetComponent<CanvasGroup>() : null);
            if (cg == null) return;
            if (StartFromValue) cg.alpha = From;
            ctx.Track(cg.TweenFade(To, Duration).SetEase(Ease).SetClock(ctx.Clock));
        }
    }

    /// <summary>Tween the transform's local scale to a target value.</summary>
    [Serializable]
    public sealed class FeedbackScale : Feedback
    {
        public Transform Target;
        [Tooltip("Snap local scale to From when this feedback fires, so the tween always starts from the same value.")]
        public bool StartFromValue;
        public Vector3 From = Vector3.one * 0.8f;
        public Vector3 To = Vector3.one;
        [Min(0f)] public float Duration = 0.25f;
        public Ease Ease = Ease.OutBack;

        protected override float ActiveDuration => Duration;

        protected override void Execute(FeedbackContext ctx)
        {
            var tr = ResolveTransform(ctx, Target);
            if (tr == null) return;
            if (StartFromValue) tr.localScale = From;
            ctx.Track(tr.TweenScale(To, Duration).SetEase(Ease).SetClock(ctx.Clock));
        }
    }

    /// <summary>Play a one-shot audio clip.</summary>
    [Serializable]
    public sealed class FeedbackAudio : Feedback
    {
        public AudioSource Source;
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume = 1f;

        protected override void Execute(FeedbackContext ctx)
        {
            var src = Source != null ? Source : (ctx.Owner != null ? ctx.Owner.GetComponent<AudioSource>() : null);
            if (src != null && Clip != null) src.PlayOneShot(Clip, Volume);
        }
    }

    /// <summary>Shake a camera (defaults to <see cref="Camera.main"/>).</summary>
    [Serializable]
    public sealed class FeedbackCameraShake : Feedback
    {
        public Camera Camera;
        public Vector3 Strength = Vector3.one * 0.3f;
        [Min(0f)] public float Duration = 0.3f;
        [Min(1f)] public float Vibrato = 10f;
        public int Seed = -1;

        protected override float ActiveDuration => Duration;

        protected override void Execute(FeedbackContext ctx)
        {
            var cam = Camera != null ? Camera : Camera.main;
            if (cam == null) return;
            ctx.Track(cam.transform.TweenShakePosition(Strength, Duration, Vibrato, Seed).SetClock(ctx.Clock));
        }
    }

    /// <summary>Invoke a UnityEvent (wire up anything from the inspector).</summary>
    [Serializable]
    public sealed class FeedbackEvent : Feedback
    {
        public UnityEvent OnPlay;

        protected override void Execute(FeedbackContext ctx) => OnPlay?.Invoke();
    }
}
