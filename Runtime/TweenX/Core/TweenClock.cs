using UnityEngine;

namespace AetherNexus.FoundationPlatform.TweenX
{
    /// <summary>
    /// Which time source drives a tween. Presentation clocks (<see cref="Unscaled"/>,
    /// <see cref="Scaled"/>) are built in and always available. <see cref="Deterministic"/>
    /// is a seam: it is empty until an external system (e.g. GameEngineCore's simulation loop)
    /// registers an <see cref="ITweenClock"/> for it via <c>TweenManager.RegisterClock</c>.
    /// Until then, <see cref="Deterministic"/> transparently falls back to <see cref="Scaled"/>
    /// with a one-time warning.
    /// </summary>
    public enum TweenClock
    {
        /// <summary>Ignores <see cref="Time.timeScale"/> — keeps running while the game is paused. Best for UI/menus.</summary>
        Unscaled = 0,

        /// <summary>Follows <see cref="Time.timeScale"/> — pauses/slows with the game. Default for gameplay-facing visuals.</summary>
        Scaled = 1,

        /// <summary>Advances on the fixed-step deterministic simulation clock, when one is registered. For reproducible, gameplay-affecting motion.</summary>
        Deterministic = 2,
    }

    /// <summary>
    /// A time source a tween can advance on. Implementations return the delta (in seconds)
    /// to apply this tick. Kept as an interface so the deterministic simulation clock can be
    /// injected from a higher assembly without FoundationPlatform depending on it.
    /// </summary>
    public interface ITweenClock
    {
        /// <summary>Seconds elapsed for this tick on this clock's timeline.</summary>
        float DeltaTime { get; }
    }

    /// <summary>Unity real time, unaffected by <see cref="Time.timeScale"/>.</summary>
    public sealed class UnscaledTweenClock : ITweenClock
    {
        public float DeltaTime => Time.unscaledDeltaTime;
    }

    /// <summary>Unity scaled time, affected by <see cref="Time.timeScale"/>.</summary>
    public sealed class ScaledTweenClock : ITweenClock
    {
        public float DeltaTime => Time.deltaTime;
    }
}
