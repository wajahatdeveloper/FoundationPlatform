using System;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Animation
{
	[Serializable]
	public struct LocomotionBlendParams
	{
		[Min(0f)]
		[Tooltip("Damping time for direction and stance weight transitions (Animancer FadeDuration semantics).")]
		public float fadeDuration;

		[Min(0f)]
		[Tooltip("Velocity magnitude below which locomotion is considered idle.")]
		public float idleVelocityThreshold;

		public static LocomotionBlendParams Default => new()
		{
			fadeDuration = 0.12f,
			idleVelocityThreshold = 0.04f,
		};
	}
}
