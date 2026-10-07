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

        // Last weight pushed to each mixer input. Only this class writes them, so an unchanged child skips the
        // native call; a locomotion stance tree re-pushes every input of every stance each frame otherwise.
        private readonly List<float> _appliedWeights = new List<float>();

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
            _appliedWeights.Add(0f);
            if (Mixer.GetInputCount() < _children.Count)
                Mixer.SetInputCount(_children.Count);
            Mixer.ConnectInput(index, state.Playable, 0, 0f);
            if (state.Playable.IsValid()) state.Playable.Play();
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            bool valid = IsValid;
            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                if (child != null)
                {
                    child.Update(deltaTime);
                    float weight = child.Weight;
                    if (valid && _appliedWeights[i] != weight)
                    {
                        Mixer.SetInputWeight(i, weight);
                        _appliedWeights[i] = weight;
                    }
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
        private const float AngleFactor = 2f;

        public Vector2 Parameter { get; set; }

        // Thresholds are read as a fixed set: reassign the array to change them, never edit it in place.
        public Vector2[] Thresholds
        {
            get => _thresholds;
            set
            {
                _thresholds = value;
                _pairCount = -1;
            }
        }

        private Vector2[] _thresholds;
        private float[] _weightsBuffer;

        // The threshold-to-threshold half of the gradient-band weight depends only on the thresholds, so it is
        // built once: weight(i,j) = 1 - (paramDistance(i) * _pairRadial[i,j] + paramAngle(i) * _pairAngular[i,j]).
        private float[] _magnitudes;
        private float[] _pairRadial;
        private float[] _pairAngular;
        private int _pairCount = -1;

        // The stance tree updates every stance blend several times a frame with the same parameter.
        private Vector2 _weightedParameter;
        private int _weightedChildCount = -1;

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

        private void BuildPairTerms(int childCount)
        {
            _magnitudes = new float[childCount];
            _pairRadial = new float[childCount * childCount];
            _pairAngular = new float[childCount * childCount];

            for (int i = 0; i < childCount; i++)
                _magnitudes[i] = _thresholds[i].magnitude;

            for (int i = 0; i < childCount; i++)
            {
                float magnitudeI = _magnitudes[i];
                for (int j = 0; j < childCount; j++)
                {
                    if (j == i) continue;
                    float magnitudeJ = _magnitudes[j];
                    float averageMagnitude = (magnitudeJ + magnitudeI) * 0.5f;
                    if (averageMagnitude < 0.0001f) averageMagnitude = 1f;

                    float angleIToJ = SignedAngle(_thresholds[i], _thresholds[j]) * AngleFactor;
                    Vector2 polarIToJ = new Vector2((magnitudeJ - magnitudeI) / averageMagnitude, angleIToJ);
                    float sqrMag = polarIToJ.sqrMagnitude;
                    if (sqrMag > 0.0001f) polarIToJ /= sqrMag;
                    else polarIToJ = Vector2.zero;

                    _pairRadial[i * childCount + j] = polarIToJ.x / averageMagnitude;
                    _pairAngular[i * childCount + j] = polarIToJ.y;
                }
            }

            _pairCount = childCount;
            _weightedChildCount = -1;
        }

        public override void Update(float deltaTime)
        {
            if (_thresholds != null && _children.Count > 0)
            {
                int childCount = _children.Count;
                Vector2 parameter = Parameter;
                bool weightsCurrent = _weightedChildCount == childCount && _pairCount == childCount
                    && parameter.x == _weightedParameter.x && parameter.y == _weightedParameter.y;

                if (!weightsCurrent)
                {
                    if (childCount == 1)
                    {
                        _children[0].Weight = 1f;
                    }
                    else
                    {
                        if (_pairCount != childCount)
                            BuildPairTerms(childCount);
                        if (_weightsBuffer == null || _weightsBuffer.Length < childCount)
                            _weightsBuffer = new float[childCount];

                        float totalWeight = 0f;
                        float parameterMagnitude = parameter.magnitude;

                        for (int i = 0; i < childCount; i++)
                        {
                            float differenceIToParameter = parameterMagnitude - _magnitudes[i];
                            float angleIToParameter = SignedAngle(_thresholds[i], parameter) * AngleFactor;
                            int row = i * childCount;

                            float weight = 1f;
                            for (int j = 0; j < childCount; j++)
                            {
                                if (j == i) continue;
                                float newWeight = 1f - (differenceIToParameter * _pairRadial[row + j]
                                                        + angleIToParameter * _pairAngular[row + j]);
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

                    _weightedParameter = parameter;
                    _weightedChildCount = childCount;
                }
            }
            base.Update(deltaTime);
        }
    }
}
