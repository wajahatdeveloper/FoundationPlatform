using AetherNexus.FoundationPlatform.TweenX;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fluent tween entry points for uGUI: <see cref="RectTransform"/>, <see cref="CanvasGroup"/>, and
/// <see cref="Graphic"/> (base of Image/RawImage/Text, and TextMeshPro's graphic). Global namespace,
/// zero <c>using</c> required at call sites.
/// </summary>
public static class UITweenExtensions
{
    // ---- RectTransform ----

    public static TweenHandle TweenAnchorPos(this RectTransform rt, Vector2 to, float duration)
        => TweenManager.Create(() => rt.anchoredPosition, v => rt.anchoredPosition = v, to, duration,
            TweenInterpolators.Vector2, TweenInterpolators.AddVector2, TweenInterpolators.SnapVector2, rt).AsHandle();

    public static TweenHandle TweenSizeDelta(this RectTransform rt, Vector2 to, float duration)
        => TweenManager.Create(() => rt.sizeDelta, v => rt.sizeDelta = v, to, duration,
            TweenInterpolators.Vector2, TweenInterpolators.AddVector2, null, rt).AsHandle();

    // ---- CanvasGroup ----

    public static TweenHandle TweenFade(this CanvasGroup cg, float to, float duration)
        => TweenManager.Create(() => cg.alpha, v => cg.alpha = v, to, duration,
            TweenInterpolators.Float, TweenInterpolators.AddFloat, null, cg).AsHandle();

    // ---- Graphic (Image / RawImage / Text / TMP) ----

    public static TweenHandle TweenColor(this Graphic g, Color to, float duration)
        => TweenManager.Create(() => g.color, v => g.color = v, to, duration,
            TweenInterpolators.Color, TweenInterpolators.AddColor, null, g).AsHandle();

    public static TweenHandle TweenFade(this Graphic g, float to, float duration)
        => TweenManager.Create(() => g.color.a, v => { var c = g.color; c.a = v; g.color = c; }, to, duration,
            TweenInterpolators.Float, TweenInterpolators.AddFloat, null, g).AsHandle();
}
