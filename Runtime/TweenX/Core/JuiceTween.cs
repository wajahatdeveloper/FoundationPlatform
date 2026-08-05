using System;
using System.Collections.Generic;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.TweenX
{
    /// <summary>
    /// "Juice" tween: a decaying oscillation applied around a captured origin — the engine behind punch
    /// and shake on position / local-position / scale / rotation. Both effects return exactly to the
    /// origin at the end. Oscillation is a pure function of time (no RNG state). Reproducibility on
    /// <see cref="TweenClock.Deterministic"/> requires an explicit <c>seed &gt;= 0</c>; the default
    /// auto-seed (<c>-1</c>) is call-order/session dependent and not reproducible. Created via the
    /// <c>TweenPunch*</c> / <c>TweenShake*</c> extensions.
    /// </summary>
    public sealed class JuiceTween : Tween
    {
        public enum Kind { Punch, Shake }
        public enum Channel { Position, LocalPosition, Scale, Rotation }

        private Transform _target;
        private Kind _kind;
        private Channel _channel;
        private Vector3 _strength;
        private float _vibrato;
        private int _seed;
        private Vector3 _origin;

        internal void Init(Transform target, Kind kind, Channel channel, Vector3 strength, float vibrato, int seed, float duration)
        {
            _target = target;
            _kind = kind;
            _channel = channel;
            _strength = strength;
            _vibrato = Mathf.Max(1f, vibrato);
            _seed = seed;
            Duration = Mathf.Max(0.0001f, duration);
            TargetObject = target;
            HasTarget = target != null;
            Ease = Ease.Linear;   // juice shapes time itself; don't double-ease
        }

        internal override void ResolveStartValues()
        {
            switch (_channel)
            {
                case Channel.Position: _origin = _target.position; break;
                case Channel.LocalPosition: _origin = _target.localPosition; break;
                case Channel.Scale: _origin = _target.localScale; break;
                case Channel.Rotation: _origin = _target.localEulerAngles; break;
            }
        }

        internal override void ApplyEased(float f)
        {
            if (_target == null) return;
            Vector3 offset = _kind == Kind.Punch ? PunchOffset(f) : ShakeOffset(f);
            Vector3 v = _origin + offset;
            switch (_channel)
            {
                case Channel.Position: _target.position = v; break;
                case Channel.LocalPosition: _target.localPosition = v; break;
                case Channel.Scale: _target.localScale = v; break;
                case Channel.Rotation: _target.localEulerAngles = v; break;
            }
        }

        private Vector3 PunchOffset(float f)
        {
            float decay = 1f - f;
            float osc = Mathf.Sin(f * _vibrato * Mathf.PI * 2f);
            return _strength * (osc * decay);
        }

        private Vector3 ShakeOffset(float f)
        {
            float decay = 1f - f;
            float x = f * _vibrato;
            return new Vector3(
                _strength.x * decay * NoiseSin(x, _seed),
                _strength.y * decay * NoiseSin(x, _seed + 31),
                _strength.z * decay * NoiseSin(x, _seed + 71));
        }

        // Deterministic pseudo-random in [-1,1]: high-frequency sine hash, no state.
        private static float NoiseSin(float x, int salt)
        {
            float v = Mathf.Sin(x * 12.9898f + salt * 78.233f) * 43758.5453f;
            return (v - Mathf.Floor(v)) * 2f - 1f;
        }

        internal override void OnIncrementLoop() { }

        internal override void SnapToStart()
        {
            if (!Started) ResolveStartValues();
            RestoreOrigin();
        }

        internal override void SnapToEnd(bool backward)
        {
            if (!Started) ResolveStartValues();
            RestoreOrigin();   // both punch and shake resolve back to the origin
        }

        private void RestoreOrigin()
        {
            if (_target == null) return;
            switch (_channel)
            {
                case Channel.Position: _target.position = _origin; break;
                case Channel.LocalPosition: _target.localPosition = _origin; break;
                case Channel.Scale: _target.localScale = _origin; break;
                case Channel.Rotation: _target.localEulerAngles = _origin; break;
            }
        }

        internal override void ReturnToPool(Dictionary<Type, object> pools)
        {
            if (!pools.TryGetValue(typeof(JuiceTween), out var s))
            {
                s = new Stack<JuiceTween>();
                pools[typeof(JuiceTween)] = s;
            }
            ((Stack<JuiceTween>)s).Push(this);
        }

        internal override void Reset()
        {
            _target = null;
            _strength = Vector3.zero;
            _vibrato = 10f;
            _seed = 0;
            _origin = Vector3.zero;
            base.Reset();
        }
    }
}
