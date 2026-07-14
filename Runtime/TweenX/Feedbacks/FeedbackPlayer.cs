using System.Collections.Generic;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.TweenX.Feedbacks
{
    /// <summary>
    /// Plays a designer-authored list of composable <see cref="Feedback"/>s on one trigger — the
    /// FEEL/MMFeedbacks-style "juice" layer built on the tween core. Call <see cref="Play"/> from code,
    /// a UnityEvent, or enable with <see cref="PlayOnEnable"/>. Each feedback fires (respecting its own
    /// delay) and its tweens are tracked so <see cref="Stop"/> can cancel the whole burst.
    ///
    /// <para>The feedback list is polymorphic (<c>[SerializeReference]</c>); the inspector's
    /// Add-Feedback dropdown auto-lists every concrete feedback type via TypeCache — no central registry.</para>
    /// </summary>
    [AddComponentMenu("FoundationPlatform/Feedback Player")]
    [DisallowMultipleComponent]
    public sealed class FeedbackPlayer : MonoBehaviour
    {
        [Tooltip("Play automatically when this component is enabled.")]
        public bool PlayOnEnable;

        [Tooltip("Time source for the feedbacks' tweens.")]
        public TweenClock Clock = TweenClock.Scaled;

        [SerializeReference]
        public List<Feedback> Feedbacks = new();

        private readonly FeedbackContext _ctx = new();

        private void OnEnable() { if (PlayOnEnable) Play(); }
        private void OnDisable() { Stop(); }

        /// <summary>Fire all active feedbacks. Any burst still running is stopped first.</summary>
        public void Play()
        {
            _ctx.Stop();
            _ctx.Begin(gameObject, Clock);
            if (Feedbacks == null) return;
            for (int i = 0; i < Feedbacks.Count; i++) Feedbacks[i]?.Play(_ctx);
        }

        /// <summary>Cancel every tween spawned by the last <see cref="Play"/>.</summary>
        public void Stop() => _ctx.Stop();
    }
}
