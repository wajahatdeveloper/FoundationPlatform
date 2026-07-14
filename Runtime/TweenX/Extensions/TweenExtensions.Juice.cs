using AetherNexus.FoundationPlatform.TweenX;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Juice" tween entry points — punch, shake, flash, blink — in the global namespace. Punch/shake
/// build a <see cref="JuiceTween"/>; flash/blink are thin wrappers over color/alpha yoyo tweens.
/// </summary>
namespace AetherNexus.FoundationPlatform.TweenX
{
public static class JuiceTweenExtensions
{
    private static int _shakeSeed;   // varies successive shakes; pass an explicit seed for reproducibility

    // ---- Punch (overshoot then settle back to origin) ----

    public static TweenHandle TweenPunchPosition(this Transform tr, Vector3 punch, float duration, float vibrato = 10f)
        => BuildJuice(tr, JuiceTween.Kind.Punch, JuiceTween.Channel.Position, punch, duration, vibrato, 0);

    public static TweenHandle TweenPunchLocalPosition(this Transform tr, Vector3 punch, float duration, float vibrato = 10f)
        => BuildJuice(tr, JuiceTween.Kind.Punch, JuiceTween.Channel.LocalPosition, punch, duration, vibrato, 0);

    public static TweenHandle TweenPunchScale(this Transform tr, Vector3 punch, float duration, float vibrato = 10f)
        => BuildJuice(tr, JuiceTween.Kind.Punch, JuiceTween.Channel.Scale, punch, duration, vibrato, 0);

    public static TweenHandle TweenPunchRotation(this Transform tr, Vector3 punch, float duration, float vibrato = 10f)
        => BuildJuice(tr, JuiceTween.Kind.Punch, JuiceTween.Channel.Rotation, punch, duration, vibrato, 0);

    // ---- Shake (decaying random jitter, back to origin) ----

    public static TweenHandle TweenShakePosition(this Transform tr, Vector3 strength, float duration, float vibrato = 10f, int seed = -1)
        => BuildJuice(tr, JuiceTween.Kind.Shake, JuiceTween.Channel.LocalPosition, strength, duration, vibrato, ResolveSeed(seed));

    public static TweenHandle TweenShakePosition(this Transform tr, float strength, float duration, float vibrato = 10f, int seed = -1)
        => tr.TweenShakePosition(new Vector3(strength, strength, strength), duration, vibrato, seed);

    public static TweenHandle TweenShakeRotation(this Transform tr, Vector3 strength, float duration, float vibrato = 10f, int seed = -1)
        => BuildJuice(tr, JuiceTween.Kind.Shake, JuiceTween.Channel.Rotation, strength, duration, vibrato, ResolveSeed(seed));

    public static TweenHandle TweenShakeScale(this Transform tr, Vector3 strength, float duration, float vibrato = 10f, int seed = -1)
        => BuildJuice(tr, JuiceTween.Kind.Shake, JuiceTween.Channel.Scale, strength, duration, vibrato, ResolveSeed(seed));

    // ---- Flash (pulse a color and back) ----

    public static TweenHandle TweenFlash(this Graphic g, Color flashColor, float duration, int flashes = 1)
        => g.TweenColor(flashColor, HalfStep(duration, flashes)).SetLoops(FlashLoops(flashes), LoopType.Yoyo);

    public static TweenHandle TweenFlash(this SpriteRenderer sr, Color flashColor, float duration, int flashes = 1)
        => sr.TweenColor(flashColor, HalfStep(duration, flashes)).SetLoops(FlashLoops(flashes), LoopType.Yoyo);

    // ---- Blink (pulse alpha to a value and back) ----

    public static TweenHandle TweenBlink(this CanvasGroup cg, float minAlpha, float duration, int blinks = 1)
        => cg.TweenFade(minAlpha, HalfStep(duration, blinks)).SetLoops(FlashLoops(blinks), LoopType.Yoyo);

    public static TweenHandle TweenBlink(this Graphic g, float minAlpha, float duration, int blinks = 1)
        => g.TweenFade(minAlpha, HalfStep(duration, blinks)).SetLoops(FlashLoops(blinks), LoopType.Yoyo);

    public static TweenHandle TweenBlink(this SpriteRenderer sr, float minAlpha, float duration, int blinks = 1)
        => sr.TweenFade(minAlpha, HalfStep(duration, blinks)).SetLoops(FlashLoops(blinks), LoopType.Yoyo);

    // ---- helpers ----

    private static TweenHandle BuildJuice(Transform tr, JuiceTween.Kind kind, JuiceTween.Channel channel,
        Vector3 strength, float duration, float vibrato, int seed)
    {
        var t = TweenManager.RentPooled<JuiceTween>();
        t.Init(tr, kind, channel, strength, vibrato, seed, duration);
        TweenManager.Register(t);
        return t.AsHandle();
    }

    private static int ResolveSeed(int seed) => seed >= 0 ? seed : unchecked(_shakeSeed++);
    private static int FlashLoops(int count) => Mathf.Max(1, count) * 2;              // out + back per flash
    private static float HalfStep(float duration, int count) => duration / (Mathf.Max(1, count) * 2f);
}
}
