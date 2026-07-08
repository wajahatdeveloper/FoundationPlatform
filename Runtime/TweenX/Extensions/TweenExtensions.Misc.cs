using System;
using FoundationPlatform.TweenX;
using UnityEngine;

/// <summary>
/// Fluent tween entry points for audio/camera plus the generic value tweeners. Global namespace.
/// </summary>
public static class MiscTweenExtensions
{
    // ---- AudioSource ----

    public static TweenHandle TweenVolume(this AudioSource src, float to, float duration)
        => TweenManager.Create(() => src.volume, v => src.volume = v, to, duration,
            TweenInterpolators.Float, TweenInterpolators.AddFloat, null, src).AsHandle();

    public static TweenHandle TweenPitch(this AudioSource src, float to, float duration)
        => TweenManager.Create(() => src.pitch, v => src.pitch = v, to, duration,
            TweenInterpolators.Float, TweenInterpolators.AddFloat, null, src).AsHandle();

    // ---- Camera ----

    public static TweenHandle TweenFOV(this Camera cam, float to, float duration)
        => TweenManager.Create(() => cam.fieldOfView, v => cam.fieldOfView = v, to, duration,
            TweenInterpolators.Float, TweenInterpolators.AddFloat, null, cam).AsHandle();

    public static TweenHandle TweenOrthoSize(this Camera cam, float to, float duration)
        => TweenManager.Create(() => cam.orthographicSize, v => cam.orthographicSize = v, to, duration,
            TweenInterpolators.Float, TweenInterpolators.AddFloat, null, cam).AsHandle();
}

/// <summary>
/// Generic value tweeners for anything without a dedicated extension: supply a getter + setter and
/// tween a raw <c>float</c>, <c>Vector3</c>, or <c>Color</c>. Static methods (not extensions) since
/// there is no natural <c>this</c> target.
/// </summary>
public static class TweenValue
{
    /// <summary>Tween an arbitrary float. Pass <paramref name="link"/> to auto-kill when it dies.</summary>
    public static TweenHandle Float(Func<float> getter, Action<float> setter, float to, float duration, UnityEngine.Object link = null)
        => TweenManager.Create(getter, setter, to, duration,
            TweenInterpolators.Float, TweenInterpolators.AddFloat, TweenInterpolators.SnapFloat, link).AsHandle();

    public static TweenHandle Vector3(Func<Vector3> getter, Action<Vector3> setter, Vector3 to, float duration, UnityEngine.Object link = null)
        => TweenManager.Create(getter, setter, to, duration,
            TweenInterpolators.Vector3, TweenInterpolators.AddVector3, TweenInterpolators.SnapVector3, link).AsHandle();

    public static TweenHandle Color(Func<Color> getter, Action<Color> setter, Color to, float duration, UnityEngine.Object link = null)
        => TweenManager.Create(getter, setter, to, duration,
            TweenInterpolators.Color, TweenInterpolators.AddColor, null, link).AsHandle();
}
