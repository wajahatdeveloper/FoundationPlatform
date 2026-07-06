using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoundationPlatform.Animation
{
    public class PlayableStateEvents
    {
        private readonly PlayableState _state;

        public Action OnEnd;

        private struct EventEntry
        {
            public float NormalizedTime;
            public Action Callback;
            public bool HasTriggered;
        }

        private List<EventEntry> _events;
        private float _prevNormalizedTime;

        public PlayableStateEvents(PlayableState state)
        {
            _state = state;
        }

        public void Add(float normalizedTime, Action callback)
        {
            if (_events == null) _events = new List<EventEntry>(4);
            _events.Add(new EventEntry { NormalizedTime = normalizedTime, Callback = callback, HasTriggered = false });
        }

        public void Update()
        {
            if (!_state.IsValid) return;

            float currentNormTime = _state.NormalizedTime;
            bool isPlayingForward = _state.EffectiveSpeed >= 0f;
            bool isLooping = _state.IsLooping;

            // Wrap/re-arm logic only applies to looping states. Non-looping playables keep
            // advancing raw time past the clip end while the state fades out, which would
            // otherwise register as a "loop" and spuriously re-fire every event.
            bool completedLoop = isLooping &&
                (isPlayingForward
                    ? Mathf.FloorToInt(currentNormTime) > Mathf.FloorToInt(_prevNormalizedTime)
                    : Mathf.FloorToInt(currentNormTime) < Mathf.FloorToInt(_prevNormalizedTime));

            // Looping: fractional normalized time [0,1) so events fire each cycle.
            // Non-looping: clamped normalized time so end-of-clip events still fire exactly once.
            float fracNorm = isLooping
                ? currentNormTime - Mathf.Floor(currentNormTime)
                : Mathf.Clamp01(currentNormTime);

            if (_events != null)
            {
                for (int i = 0; i < _events.Count; i++)
                {
                    var entry = _events[i];

                    if (completedLoop)
                    {
                        // Fire events the previous cycle never reached (frame jumped across the
                        // loop boundary) before re-arming them for the new cycle.
                        if (!entry.HasTriggered && isPlayingForward)
                        {
                            _events[i] = entry;
                            entry.Callback?.Invoke();
                        }
                        entry.HasTriggered = false;
                    }

                    if (!entry.HasTriggered)
                    {
                        if ((isPlayingForward && fracNorm >= entry.NormalizedTime) ||
                            (!isPlayingForward && fracNorm <= entry.NormalizedTime))
                        {
                            entry.HasTriggered = true;
                            _events[i] = entry;
                            entry.Callback?.Invoke();
                        }
                        else
                        {
                            _events[i] = entry;
                        }
                    }
                }
            }

            if (OnEnd != null)
            {
                // Non-looping clips: NormalizedTime reaches or exceeds 1
                // Looping clips used as one-shots: fires after the first full loop
                if (currentNormTime >= 1f)
                {
                    var cb = OnEnd;
                    OnEnd = null;
                    cb.Invoke();
                }
            }

            _prevNormalizedTime = currentNormTime;
        }
    }
}
