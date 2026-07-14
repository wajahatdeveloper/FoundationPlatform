using AetherNexus.FoundationPlatform.TweenX;
using UnityEngine;

/// <summary>
/// Fluent tween entry points on <see cref="Transform"/>. Declared in the global namespace (like
/// <c>MathX</c> / <c>CoroutineX</c>) so they're callable with zero <c>using</c> and read like
/// DOTween's <c>transform.DOMove(...)</c>. Every method returns a <see cref="TweenHandle"/> you can
/// chain (<c>.SetEase(...).SetLoops(...)</c>) or store to control later.
/// </summary>
namespace AetherNexus.FoundationPlatform.TweenX
{
public static class TransformTweenExtensions
{
    /// <summary>Tween world position to <paramref name="to"/> over <paramref name="duration"/> seconds.</summary>
    public static TweenHandle TweenMove(this Transform tr, Vector3 to, float duration)
        => TweenManager.Create(() => tr.position, v => tr.position = v, to, duration,
            TweenInterpolators.Vector3, TweenInterpolators.AddVector3, TweenInterpolators.SnapVector3, tr).AsHandle();

    public static TweenHandle TweenMoveX(this Transform tr, float x, float duration)
        => TweenManager.Create(() => tr.position.x, v => { var p = tr.position; p.x = v; tr.position = p; }, x, duration,
            TweenInterpolators.Float, TweenInterpolators.AddFloat, TweenInterpolators.SnapFloat, tr).AsHandle();

    public static TweenHandle TweenMoveY(this Transform tr, float y, float duration)
        => TweenManager.Create(() => tr.position.y, v => { var p = tr.position; p.y = v; tr.position = p; }, y, duration,
            TweenInterpolators.Float, TweenInterpolators.AddFloat, TweenInterpolators.SnapFloat, tr).AsHandle();

    public static TweenHandle TweenMoveZ(this Transform tr, float z, float duration)
        => TweenManager.Create(() => tr.position.z, v => { var p = tr.position; p.z = v; tr.position = p; }, z, duration,
            TweenInterpolators.Float, TweenInterpolators.AddFloat, TweenInterpolators.SnapFloat, tr).AsHandle();

    /// <summary>Tween local position to <paramref name="to"/>.</summary>
    public static TweenHandle TweenLocalMove(this Transform tr, Vector3 to, float duration)
        => TweenManager.Create(() => tr.localPosition, v => tr.localPosition = v, to, duration,
            TweenInterpolators.Vector3, TweenInterpolators.AddVector3, TweenInterpolators.SnapVector3, tr).AsHandle();

    /// <summary>Tween local scale to <paramref name="to"/>.</summary>
    public static TweenHandle TweenScale(this Transform tr, Vector3 to, float duration)
        => TweenManager.Create(() => tr.localScale, v => tr.localScale = v, to, duration,
            TweenInterpolators.Vector3, TweenInterpolators.AddVector3, null, tr).AsHandle();

    /// <summary>Tween uniform local scale to <paramref name="to"/>.</summary>
    public static TweenHandle TweenScale(this Transform tr, float to, float duration)
        => tr.TweenScale(new Vector3(to, to, to), duration);

    /// <summary>Tween world rotation to Euler angles <paramref name="toEuler"/>.</summary>
    public static TweenHandle TweenRotate(this Transform tr, Vector3 toEuler, float duration)
        => TweenManager.Create(() => tr.rotation, v => tr.rotation = v, Quaternion.Euler(toEuler), duration,
            TweenInterpolators.Quaternion, null, null, tr).AsHandle();

    /// <summary>Tween world rotation to <paramref name="to"/> (quaternion, Slerp).</summary>
    public static TweenHandle TweenRotateQuaternion(this Transform tr, Quaternion to, float duration)
        => TweenManager.Create(() => tr.rotation, v => tr.rotation = v, to, duration,
            TweenInterpolators.Quaternion, null, null, tr).AsHandle();

    /// <summary>Tween local rotation to Euler angles <paramref name="toEuler"/>.</summary>
    public static TweenHandle TweenLocalRotate(this Transform tr, Vector3 toEuler, float duration)
        => TweenManager.Create(() => tr.localRotation, v => tr.localRotation = v, Quaternion.Euler(toEuler), duration,
            TweenInterpolators.Quaternion, null, null, tr).AsHandle();
}
}
