using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AetherNexus.FoundationPlatform.Animation
{
    public class MixerState : PlayableState
    {
        public AnimationMixerPlayable Mixer { get; private set; }
        protected List<PlayableState> _children = new List<PlayableState>();

        public MixerState(PlayableGraph graph) : this(graph, 0) {}

        public MixerState(PlayableGraph graph, int childCount)
        {
            Mixer = AnimationMixerPlayable.Create(graph, childCount);
            Playable = Mixer;
            if (Mixer.IsValid()) Mixer.Play();
        }

        public PlayableState GetChild(int index)
        {
            if (index >= 0 && index < _children.Count) return _children[index];
            return null;
        }

        public void SetChildWeight(int index, float weight)
        {
            var child = GetChild(index);
            if (child != null) child.Weight = weight;
        }

        public void AddChild(PlayableState state)
        {
            int index = _children.Count;
            _children.Add(state);
            if (Mixer.GetInputCount() < _children.Count)
                Mixer.SetInputCount(_children.Count);
            Mixer.ConnectInput(index, state.Playable, 0, 0f);
            if (state.Playable.IsValid()) state.Playable.Play();
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                if (child != null)
                {
                    child.Update(deltaTime);
                    if (IsValid) Mixer.SetInputWeight(i, child.Weight);
                }
            }
        }

        public override void Destroy()
        {
            foreach (var child in _children)
                child?.Destroy();
            base.Destroy();
        }
    }

    public class ManualMixerState : MixerState
    {
        public ManualMixerState(PlayableGraph graph) : this(graph, 0) {}

        public ManualMixerState(PlayableGraph graph, int childCount) : base(graph, childCount) {}
    }

    public class LinearMixerState : MixerState
    {
        public float Parameter { get; set; }
        public float[] Thresholds { get; set; }

        public LinearMixerState(PlayableGraph graph) : this(graph, 0) {}

        public LinearMixerState(PlayableGraph graph, int childCount) : base(graph, childCount) {}

        public override void Update(float deltaTime)
        {
            if (Thresholds != null && _children.Count > 0)
            {
                int childCount = _children.Count;
                if (childCount == 1)
                {
                    _children[0].Weight = 1f;
                }
                else
                {
                    float param = Parameter;
                    for (int i = 0; i < childCount; i++) _children[i].Weight = 0f;

                    if (param <= Thresholds[0])
                    {
                        _children[0].Weight = 1f;
                    }
                    else if (param >= Thresholds[childCount - 1])
                    {
                        _children[childCount - 1].Weight = 1f;
                    }
                    else
                    {
                        for (int i = 0; i < childCount - 1; i++)
                        {
                            if (param >= Thresholds[i] && param <= Thresholds[i + 1])
                            {
                                float t = (param - Thresholds[i]) / (Thresholds[i + 1] - Thresholds[i]);
                                _children[i].Weight = 1f - t;
                                _children[i + 1].Weight = t;
                                break;
                            }
                        }
                    }
                }
            }
            base.Update(deltaTime);
        }
    }

    public class DirectionalMixerState : MixerState
    {
        public Vector2 Parameter { get; set; }
        public Vector2[] Thresholds { get; set; }

        private float[] _weightsBuffer;

        public DirectionalMixerState(PlayableGraph graph) : this(graph, 0) {}

        public DirectionalMixerState(PlayableGraph graph, int childCount) : base(graph, childCount)
        {
            _weightsBuffer = new float[Mathf.Max(childCount, 9)];
        }

        private static float SignedAngle(Vector2 a, Vector2 b)
        {
            if ((a.x == 0 && a.y == 0) || (b.x == 0 && b.y == 0)) return 0;
            return Mathf.Atan2(a.x * b.y - a.y * b.x, a.x * b.x + a.y * b.y);
        }

        public override void Update(float deltaTime)
        {
            if (Thresholds != null && _children.Count > 0)
            {
                int childCount = _children.Count;

                if (_weightsBuffer == null || _weightsBuffer.Length < childCount)
                    _weightsBuffer = new float[childCount];

                if (childCount == 1)
                {
                    _children[0].Weight = 1f;
                }
                else
                {
                    float totalWeight = 0f;
                    float parameterMagnitude = Parameter.magnitude;
                    const float AngleFactor = 2f;

                    for (int i = 0; i < childCount; i++)
                    {
                        Vector2 thresholdI = Thresholds[i];
                        float magnitudeI = thresholdI.magnitude;
                        float differenceIToParameter = parameterMagnitude - magnitudeI;
                        float angleIToParameter = SignedAngle(thresholdI, Parameter) * AngleFactor;

                        float weight = 1f;

                        for (int j = 0; j < childCount; j++)
                        {
                            if (j == i) continue;
                            Vector2 thresholdJ = Thresholds[j];
                            float magnitudeJ = thresholdJ.magnitude;
                            float averageMagnitude = (magnitudeJ + magnitudeI) * 0.5f;
                            if (averageMagnitude < 0.0001f) averageMagnitude = 1f;

                            float differenceIToJ = magnitudeJ - magnitudeI;
                            float angleIToJ = SignedAngle(thresholdI, thresholdJ) * AngleFactor;

                            Vector2 polarIToJ = new Vector2(differenceIToJ / averageMagnitude, angleIToJ);
                            float sqrMag = polarIToJ.sqrMagnitude;
                            if (sqrMag > 0.0001f) polarIToJ /= sqrMag;
                            else polarIToJ = Vector2.zero;

                            Vector2 polarIToParameter = new Vector2(differenceIToParameter / averageMagnitude, angleIToParameter);
                            float newWeight = 1f - Vector2.Dot(polarIToParameter, polarIToJ);
                            if (weight > newWeight) weight = newWeight;
                        }

                        if (weight < 0.01f) weight = 0f;
                        _weightsBuffer[i] = weight;
                        totalWeight += weight;
                    }

                    if (totalWeight > 0f)
                        for (int i = 0; i < childCount; i++) _children[i].Weight = _weightsBuffer[i] / totalWeight;
                    else
                        for (int i = 0; i < childCount; i++) _children[i].Weight = 0f;
                }
            }
            base.Update(deltaTime);
        }
    }
}
