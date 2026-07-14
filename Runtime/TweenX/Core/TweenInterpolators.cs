using System;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.TweenX
{
    /// <summary>Pure interpolation between two values of <typeparamref name="T"/> at factor <c>t</c>.</summary>
    public delegate T Interpolator<T>(T a, T b, float t);

    /// <summary>Adds two values of <typeparamref name="T"/> (used for relative + incremental-loop tweens).</summary>
    public delegate T Adder<T>(T a, T b);

    /// <summary>
    /// Cached, allocation-free interpolators and adders for the value types the tween system
    /// supports. Each delegate is a <c>static readonly</c> singleton created once at type init,
    /// so assigning one onto a tween costs nothing at runtime and the tick loop never allocates.
    /// <c>Unclamped</c> interpolation is used on purpose: overshoot eases (Back/Elastic/Bounce)
    /// push the factor outside [0,1] and must not be clamped.
    /// </summary>
    public static class TweenInterpolators
    {
        public static readonly Interpolator<float> Float = (a, b, t) => a + (b - a) * t;
        public static readonly Interpolator<int> Int = (a, b, t) => Mathf.RoundToInt(a + (b - a) * t);
        public static readonly Interpolator<Vector2> Vector2 = (a, b, t) => a + (b - a) * t;
        public static readonly Interpolator<Vector3> Vector3 = (a, b, t) => a + (b - a) * t;
        public static readonly Interpolator<Vector4> Vector4 = (a, b, t) => a + (b - a) * t;
        public static readonly Interpolator<Color> Color = (a, b, t) => a + (b - a) * t;
        public static readonly Interpolator<Quaternion> Quaternion = UnityEngine.Quaternion.SlerpUnclamped;
        public static readonly Interpolator<Rect> Rect = (a, b, t) => new Rect(
            a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t,
            a.width + (b.width - a.width) * t, a.height + (b.height - a.height) * t);

        public static readonly Adder<float> AddFloat = (a, b) => a + b;
        public static readonly Adder<int> AddInt = (a, b) => a + b;
        public static readonly Adder<Vector2> AddVector2 = (a, b) => a + b;
        public static readonly Adder<Vector3> AddVector3 = (a, b) => a + b;
        public static readonly Adder<Vector4> AddVector4 = (a, b) => a + b;
        public static readonly Adder<Color> AddColor = (a, b) => a + b;
        // Quaternion / Rect have no meaningful additive delta → relative/incremental unsupported.

        // Snappers — round to whole units when snapping is enabled (mainly for pixel-perfect position).
        public static readonly Func<float, bool, float> SnapFloat = (v, on) => on ? Mathf.Round(v) : v;
        public static readonly Func<Vector2, bool, Vector2> SnapVector2 = (v, on) =>
            on ? new Vector2(Mathf.Round(v.x), Mathf.Round(v.y)) : v;
        public static readonly Func<Vector3, bool, Vector3> SnapVector3 = (v, on) =>
            on ? new Vector3(Mathf.Round(v.x), Mathf.Round(v.y), Mathf.Round(v.z)) : v;
    }
}
