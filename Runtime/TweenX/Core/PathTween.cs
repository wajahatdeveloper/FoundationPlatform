using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoundationPlatform.TweenX
{
    /// <summary>Interpolation shape for a <see cref="PathTween"/>.</summary>
    public enum PathType
    {
        /// <summary>Straight segments between waypoints.</summary>
        Linear = 0,

        /// <summary>Smooth Catmull-Rom spline through the waypoints.</summary>
        CatmullRom = 1,
    }

    /// <summary>
    /// Moves a <see cref="Transform"/> along a series of waypoints. The current position is used as the
    /// implicit first point, so you only supply the destinations. Eased normalized time maps uniformly
    /// across segments (not arc-length parameterized — even spacing of waypoints gives even motion).
    /// Created via <c>transform.TweenPath(...)</c>.
    /// </summary>
    public sealed class PathTween : Tween
    {
        private Transform _target;
        private bool _local;
        private PathType _type;
        private Vector3[] _waypoints;   // caller-supplied destinations
        private Vector3[] _points;      // [current, ...waypoints], rebuilt on start

        internal void Init(Transform target, Vector3[] waypoints, PathType type, bool local, float duration)
        {
            _target = target;
            _waypoints = waypoints;
            _type = type;
            _local = local;
            Duration = Mathf.Max(0f, duration);
            TargetObject = target;
            HasTarget = target != null;
        }

        internal override void ResolveStartValues()
        {
            int wp = _waypoints?.Length ?? 0;
            int n = wp + 1;
            if (_points == null || _points.Length != n) _points = new Vector3[n];
            _points[0] = _local ? _target.localPosition : _target.position;
            for (int i = 0; i < wp; i++) _points[i + 1] = _waypoints[i];
        }

        internal override void ApplyEased(float easedFactor)
        {
            if (_target == null) return;
            Vector3 pos = Eval(easedFactor);
            if (Snapping) pos = new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), Mathf.Round(pos.z));
            if (_local) _target.localPosition = pos; else _target.position = pos;
        }

        private Vector3 Eval(float t)
        {
            var pts = _points;
            if (pts == null || pts.Length == 0) return Vector3.zero;
            if (pts.Length == 1) return pts[0];

            t = Mathf.Clamp01(t);
            int last = pts.Length - 1;
            float scaled = t * last;
            int i = Mathf.Min((int)scaled, last - 1);
            float lt = scaled - i;

            if (_type == PathType.Linear)
                return Vector3.LerpUnclamped(pts[i], pts[i + 1], lt);

            Vector3 p0 = pts[Mathf.Max(i - 1, 0)];
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[i + 1];
            Vector3 p3 = pts[Mathf.Min(i + 2, last)];
            return CatmullRom(p0, p1, p2, p3, lt);
        }

        // Standard Catmull-Rom (tension 0.5).
        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * ((2f * p1) +
                           (-p0 + p2) * t +
                           (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                           (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        internal override void OnIncrementLoop() { }
        internal override void SnapToStart() { if (!Started) ResolveStartValues(); ApplyEased(0f); }
        internal override void SnapToEnd(bool backward) { if (!Started) ResolveStartValues(); ApplyEased(backward ? 0f : 1f); }

        internal override void ReturnToPool(Dictionary<Type, object> pools)
        {
            if (!pools.TryGetValue(typeof(PathTween), out var s))
            {
                s = new Stack<PathTween>();
                pools[typeof(PathTween)] = s;
            }
            ((Stack<PathTween>)s).Push(this);
        }

        internal override void Reset()
        {
            _target = null;
            _waypoints = null;
            _local = false;
            _type = PathType.CatmullRom;
            // _points buffer kept for reuse across pooled lives.
            base.Reset();
        }

        /// <summary>The path's world-space evaluation points (start + waypoints) — for editor gizmo drawing.</summary>
        internal Vector3[] EditorPoints => _points;
    }
}
