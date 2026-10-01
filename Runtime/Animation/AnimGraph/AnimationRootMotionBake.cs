using System;
using AetherNexus.FoundationPlatform.AetherInspector;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Animation
{
	/// <summary>
	/// Root trajectory of one clip, sampled in the Editor so simulation can read root motion without
	/// evaluating the animation graph. The graph only runs where the clip is drawn, so motion read from it
	/// would differ between peers looking at the unit from different distances.
	/// <para>
	/// Positions and rotations are cumulative from clip time 0, in the clip's own frame and in the metres
	/// of the clip's source rig. Every character playing the clip travels the same distance; that is the
	/// price of not reading the playing avatar, whose graph may not be running.
	/// </para>
	/// </summary>
	[Serializable]
	public sealed class AnimationRootMotionBake
	{
		[ReadOnly]
		[Tooltip("Samples per second of clip time. Written by the bake.")]
		public float sampleRate;

		[ReadOnly]
		[Tooltip("Clip length in seconds when it was baked.")]
		public float clipLength;

		[ReadOnly]
		[Tooltip("Import hash of the clip at bake time. A mismatch means the clip changed and must be re-baked.")]
		public string sourceHash = string.Empty;

		[HideInInspector] public Vector3[] positions = Array.Empty<Vector3>();
		[HideInInspector] public Quaternion[] rotations = Array.Empty<Quaternion>();

		public bool IsBaked =>
			sampleRate > 0f && clipLength > 0f
			&& positions != null && positions.Length >= 2
			&& rotations != null && rotations.Length == positions.Length;

		/// <summary>
		/// Root motion between two clip times, in the root frame at <paramref name="fromTime"/>: the position
		/// delta is rotated into that frame and the rotation delta is local, so callers apply them as
		/// <c>bodyRotation * delta</c> and <c>bodyRotation * rotationDelta</c>. Looping clips unwrap across
		/// the loop seam.
		/// </summary>
		public void SampleDelta(float fromTime, float toTime, bool looping, out Vector3 localDelta, out Quaternion localRotationDelta)
		{
			if (!IsBaked)
				throw new InvalidOperationException("[Animation:ERROR:RootMotion] SampleDelta on an unbaked clip.");

			Sample(fromTime, looping, out var p0, out var r0);
			Sample(toTime, looping, out var p1, out var r1);

			var inverse = Quaternion.Inverse(r0);
			localDelta = inverse * (p1 - p0);
			localRotationDelta = inverse * r1;
		}

		private void Sample(float time, bool looping, out Vector3 position, out Quaternion rotation)
		{
			var cycles = 0;
			if (looping)
			{
				cycles = Mathf.FloorToInt(time / clipLength);
				time -= cycles * clipLength;
			}
			else
			{
				time = Mathf.Clamp(time, 0f, clipLength);
			}

			var last = positions.Length - 1;
			var f = Mathf.Clamp(time * sampleRate, 0f, last);
			var i = Mathf.Min((int)f, last - 1);
			var t = f - i;

			position = Vector3.LerpUnclamped(positions[i], positions[i + 1], t);
			rotation = Quaternion.SlerpUnclamped(rotations[i], rotations[i + 1], t);

			if (cycles == 0)
				return;

			// Each full cycle ends where the next begins, offset by the cycle's own displacement and turn.
			var cycleRotation = Quaternion.Inverse(rotations[0]) * rotations[last];
			var cycleDelta = positions[last] - positions[0];
			var accumulatedPosition = Vector3.zero;
			var accumulatedRotation = Quaternion.identity;
			for (var c = 0; c < cycles; c++)
			{
				accumulatedPosition += accumulatedRotation * cycleDelta;
				accumulatedRotation *= cycleRotation;
			}

			position = accumulatedPosition + accumulatedRotation * position;
			rotation = accumulatedRotation * rotation;
		}
	}
}
