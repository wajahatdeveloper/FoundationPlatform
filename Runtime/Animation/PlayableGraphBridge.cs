using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AetherNexus.FoundationPlatform.Animation
{
    public class AnimEventDispatcher
    {
        private Dictionary<string, Action> _events = new Dictionary<string, Action>();
        public void AddTo(string name, Action cb) { if(!_events.ContainsKey(name)) _events[name] = cb; else _events[name] += cb; }
        public void Remove(string name, Action cb) { if(_events.ContainsKey(name)) _events[name] -= cb; }
        public void Fire(string name) { if(_events.TryGetValue(name, out var cb)) cb?.Invoke(); }
    }

    public class PlayableGraphBridge : MonoBehaviour
    {
        public PlayableGraph Graph { get; private set; }
        public bool IsValid => Graph.IsValid();
        public bool IsGraphInitialized => IsValid;
        public AnimEventDispatcher Events { get; } = new AnimEventDispatcher();

        private AnimationLayerMixerPlayable _layerMixer;
        private Playable _rootPlayable;
        public List<PlayableLayer> Layers { get; private set; }

        public float Speed
        {
            get => _rootPlayable.IsValid() ? (float)_rootPlayable.GetSpeed() : 1f;
            set { if (_rootPlayable.IsValid()) _rootPlayable.SetSpeed(value); }
        }

        public void InitializeGraph() => InitializeGraph(GetComponent<Animator>());

        public void InitializeGraph(Animator animator) => InitializeGraph(animator, 3);

        public void InitializeGraph(Animator animator, int initialLayerCount)
        {
            if (Graph.IsValid()) Graph.Destroy();

            Graph = PlayableGraph.Create(gameObject.name + " AnimGraph");
            Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            var output = AnimationPlayableOutput.Create(Graph, "Output", animator);
            _layerMixer = AnimationLayerMixerPlayable.Create(Graph, initialLayerCount);
            _rootPlayable = _layerMixer;
            output.SetSourcePlayable(_layerMixer);

            Layers = new List<PlayableLayer>(initialLayerCount);
            for (int i = 0; i < initialLayerCount; i++)
            {
                var layer = new PlayableLayer(this, _layerMixer, i);
                Layers.Add(layer);
                if (i == 0) layer.Weight = 1f;
            }

            Graph.Play();
        }

        public void Evaluate(float deltaTime) { if (IsValid) Graph.Evaluate(deltaTime); }

        public void Evaluate() { if (IsValid) Graph.Evaluate(); }

        public void SwapOutput(Animator animator)
        {
            if (!IsValid) return;
            if (Graph.GetOutputCount() > 0)
                Graph.DestroyOutput(Graph.GetOutput(0));
            var output = AnimationPlayableOutput.Create(Graph, "Output", animator);
            output.SetSourcePlayable(_layerMixer);
        }

        private void Update()
        {
            if (!IsValid) return;
            float dt = Time.deltaTime;
            foreach (var layer in Layers)
                layer.Update(dt);
        }

        private void OnEnable()
        {
            if (Graph.IsValid()) Graph.Play();
        }

        private void OnDisable()
        {
            if (Graph.IsValid()) Graph.Stop();
        }

        private void OnDestroy()
        {
            if (Graph.IsValid()) Graph.Destroy();
        }
    }

    public class PlayableLayer
    {
        private PlayableGraphBridge _bridge;
        private AnimationLayerMixerPlayable _layerMixer;
        private AnimationMixerPlayable _stateMixer;
        public int Index { get; private set; }

        /// <summary>Owning bridge, exposed for editor tooling (scrub-then-evaluate).</summary>
        public PlayableGraphBridge Bridge => _bridge;

        public PlayableState CurrentState { get; private set; }

        public class ActiveState
        {
            public PlayableState State;
            public int Port;
            public float TargetWeight;
            public float FadeSpeed;
        }

        private List<ActiveState> _activeStates = new List<ActiveState>();
        public IReadOnlyList<ActiveState> ActiveStates => _activeStates;
        private int _nextAvailablePort = 0;
        private Queue<int> _freePorts = new Queue<int>();

        public PlayableLayer(PlayableGraphBridge bridge, AnimationLayerMixerPlayable layerMixer, int index)
        {
            _bridge = bridge;
            _layerMixer = layerMixer;
            Index = index;
            _stateMixer = AnimationMixerPlayable.Create(bridge.Graph, 0);
            // INVARIANT: every layer boots connected at weight 1. Overlay-layer visibility is
            // primarily gated by STATE weight — an empty / all-zero state mixer passes the lower
            // layers through instead of overriding with a bind pose, so an idle overlay layer at
            // weight 1 contributes nothing. Do NOT boot overlay layers at weight 0.
            // TransitionBackFromLayer does fade overlay LAYER weight to 0 after a one-shot ends
            // (its last state stays at weight 1), so every play path on layers 1+ restores
            // layer.Weight = 1 before playing (the Weight setter also cancels pending fades).
            _layerMixer.ConnectInput(Index, _stateMixer, 0, 1f);
        }

        private float _layerTargetWeight = 1f;
        private float _layerFadeSpeed = 1000f;

        private static AvatarMask _defaultMask;
        public AvatarMask Mask
        {
            set
            {
                if (value == null)
                {
                    if (_defaultMask == null)
                    {
                        _defaultMask = new AvatarMask();
                        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                            _defaultMask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, true);
                    }
                    value = _defaultMask;
                }
                _layerMixer.SetLayerMaskFromAvatarMask((uint)Index, value);
            }
        }

        public float Weight
        {
            get => _layerMixer.GetInputWeight(Index);
            set
            {
                // Direct set must also cancel any pending StartFade target, otherwise the next
                // Update() fades the layer right back toward the stale target (e.g. Weight = 1
                // after a TransitionBack faded the target to 0 would immediately fade out again).
                _layerTargetWeight = value;
                _layerMixer.SetInputWeight(Index, value);
            }
        }

        public void StartFade(float targetWeight, float fadeDuration)
        {
            _layerTargetWeight = targetWeight;
            _layerFadeSpeed = fadeDuration > 0f ? 1f / fadeDuration : 1000f;
        }

        public ClipState Play(ClipTransitionData transition) => Play(transition, -1f);

        public ClipState Play(ClipTransitionData transition, float fadeDuration)
        {
            var state = new ClipState(_bridge.Graph, transition.Clip);
            state.Speed = transition.Speed;
            float fade = fadeDuration >= 0f ? fadeDuration : transition.FadeDuration;
            WireClipEvents(state, transition);
            return Play(state, fade) as ClipState;
        }

        // Registers each authored clip event as a timed callback that fires the named event into the
        // bridge's dispatcher. This is the single funnel for entry/transition-based playback (PlayFromSet,
        // sequences, jump, etc.), so authored events fire on every such path. Raw-AnimationClip plays
        // (CrossfadeAsync / PlayLoopingAnimation) carry no transition data and therefore no events.
        private void WireClipEvents(PlayableState state, ClipTransitionData transition)
        {
            if (transition == null || !transition.HasEvents)
                return;

            var events = state.Events();
            for (int i = 0; i < transition.events.Length; i++)
            {
                var evt = transition.events[i];
                if (evt == null || string.IsNullOrWhiteSpace(evt.eventName))
                    continue;

                string name = evt.eventName;
                events.Add(Mathf.Clamp01(evt.normalizedTime), () => _bridge.Events.Fire(name));
            }
        }

        public ClipState Play(AnimationClip clip) => Play(clip, 0.25f);

        public ClipState Play(AnimationClip clip, float fadeDuration)
        {
            var state = new ClipState(_bridge.Graph, clip);
            return Play(state, fadeDuration) as ClipState;
        }

        public PlayableState Play(PlayableState state) => Play(state, 0.25f);

        public PlayableState Play(PlayableState state, float fadeDuration)
        {
            if (state == null || !state.IsValid)
                throw new ArgumentException(
                    $"PlayableLayer.Play on layer {Index}: state is null or has no valid playable (null AnimationClip?).");

            if (CurrentState != null && CurrentState != state)
            {
                foreach (var active in _activeStates)
                {
                    if (active.State == CurrentState)
                    {
                        active.TargetWeight = 0f;
                        active.FadeSpeed = fadeDuration > 0f ? 1f / fadeDuration : 1000f;
                    }
                }
            }

            CurrentState = state;
            bool found = false;
            foreach (var active in _activeStates)
            {
                if (active.State == state)
                {
                    active.TargetWeight = 1f;
                    active.FadeSpeed = fadeDuration > 0f ? 1f / fadeDuration : 1000f;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                int port;
                if (_freePorts.Count > 0)
                {
                    port = _freePorts.Dequeue();
                }
                else
                {
                    port = _nextAvailablePort++;
                    if (_stateMixer.GetInputCount() < _nextAvailablePort)
                        _stateMixer.SetInputCount(_nextAvailablePort);
                }

                _stateMixer.ConnectInput(port, state.Playable, 0, fadeDuration <= 0f ? 1f : 0f);
                if (state.Playable.IsValid()) state.Playable.Play();
                state.Weight = fadeDuration <= 0f ? 1f : 0f;

                _activeStates.Add(new ActiveState
                {
                    State = state,
                    Port = port,
                    TargetWeight = 1f,
                    FadeSpeed = fadeDuration > 0f ? 1f / fadeDuration : 1000f
                });
            }

            return state;
        }

        public void Stop() => Stop(0f);

        /// <summary>Fades all active states on this layer to weight 0. 0 duration = instant.</summary>
        public void Stop(float fadeDuration)
        {
            float fadeSpeed = fadeDuration > 0f ? 1f / fadeDuration : 1000f;
            foreach (var active in _activeStates)
            {
                active.TargetWeight = 0f;
                active.FadeSpeed = fadeSpeed;
            }
            CurrentState = null;
        }

        public void Update(float deltaTime)
        {
            float layerW = Weight;
            if (layerW != _layerTargetWeight)
                Weight = Mathf.MoveTowards(layerW, _layerTargetWeight, _layerFadeSpeed * deltaTime);

            for (int i = _activeStates.Count - 1; i >= 0; i--)
            {
                var active = _activeStates[i];
                active.State.Update(deltaTime);

                float currentWeight = _stateMixer.GetInputWeight(active.Port);
                if (currentWeight != active.TargetWeight)
                {
                    currentWeight = Mathf.MoveTowards(currentWeight, active.TargetWeight, active.FadeSpeed * deltaTime);
                    _stateMixer.SetInputWeight(active.Port, currentWeight);
                    active.State.Weight = currentWeight;
                }

                if (currentWeight == 0f && active.TargetWeight == 0f && active.State != CurrentState)
                {
                    active.State.Weight = 0f;
                    // Only ClipStates are transient (a fresh one is created per Play), so only they
                    // are disconnected + destroyed here. MixerStates (stance / blend) are long-lived
                    // and intentionally reused: they stay connected at weight 0 and are re-targeted
                    // on the next Play, so they are deliberately NOT reclaimed.
                    if (active.State is ClipState)
                    {
                        _stateMixer.DisconnectInput(active.Port);
                        _freePorts.Enqueue(active.Port);
                        _activeStates.RemoveAt(i);
                        active.State.Destroy();
                    }
                }
            }
        }
    }
}