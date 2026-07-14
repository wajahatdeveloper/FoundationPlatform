using AetherNexus.FoundationPlatform.TweenX;
using UnityEngine;

/// <summary>
/// Fluent tween entry points for renderers/materials: <see cref="SpriteRenderer"/>,
/// <see cref="Material"/>, <see cref="Light"/>. Global namespace.
/// </summary>
public static class RenderingTweenExtensions
{
    // ---- SpriteRenderer ----

    public static TweenHandle TweenColor(this SpriteRenderer sr, Color to, float duration)
        => TweenManager.Create(() => sr.color, v => sr.color = v, to, duration,
            TweenInterpolators.Color, TweenInterpolators.AddColor, null, sr).AsHandle();

    public static TweenHandle TweenFade(this SpriteRenderer sr, float to, float duration)
        => TweenManager.Create(() => sr.color.a, v => { var c = sr.color; c.a = v; sr.color = c; }, to, duration,
            TweenInterpolators.Float, TweenInterpolators.AddFloat, null, sr).AsHandle();

    // ---- Material (instances; mutating shared materials is on the caller) ----

    public static TweenHandle TweenColor(this Material mat, Color to, float duration)
        => TweenManager.Create(() => mat.color, v => mat.color = v, to, duration,
            TweenInterpolators.Color, TweenInterpolators.AddColor, null, mat).AsHandle();

    public static TweenHandle TweenFloat(this Material mat, int propertyId, float to, float duration)
        => TweenManager.Create(() => mat.GetFloat(propertyId), v => mat.SetFloat(propertyId, v), to, duration,
            TweenInterpolators.Float, TweenInterpolators.AddFloat, null, mat).AsHandle();

    public static TweenHandle TweenColor(this Material mat, int propertyId, Color to, float duration)
        => TweenManager.Create(() => mat.GetColor(propertyId), v => mat.SetColor(propertyId, v), to, duration,
            TweenInterpolators.Color, TweenInterpolators.AddColor, null, mat).AsHandle();

    // ---- Light ----

    public static TweenHandle TweenIntensity(this Light light, float to, float duration)
        => TweenManager.Create(() => light.intensity, v => light.intensity = v, to, duration,
            TweenInterpolators.Float, TweenInterpolators.AddFloat, null, light).AsHandle();

    public static TweenHandle TweenColor(this Light light, Color to, float duration)
        => TweenManager.Create(() => light.color, v => light.color = v, to, duration,
            TweenInterpolators.Color, TweenInterpolators.AddColor, null, light).AsHandle();
}
