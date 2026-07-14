using AetherNexus.FoundationPlatform.TweenX;
using UnityEngine;

/// <summary>Fluent path-tween entry point on <see cref="Transform"/>. Global namespace.</summary>
namespace AetherNexus.FoundationPlatform.TweenX
{
public static class PathTweenExtensions
{
    /// <summary>
    /// Move this transform along <paramref name="waypoints"/> (current position is the implicit start).
    /// Returns a <see cref="TweenHandle"/> for chaining/control.
    /// </summary>
    public static TweenHandle TweenPath(this Transform tr, Vector3[] waypoints, float duration,
        PathType type = PathType.CatmullRom, bool local = false)
    {
        var t = TweenManager.RentPooled<PathTween>();
        t.Init(tr, waypoints, type, local, duration);
        TweenManager.Register(t);
        return t.AsHandle();
    }
}
}
