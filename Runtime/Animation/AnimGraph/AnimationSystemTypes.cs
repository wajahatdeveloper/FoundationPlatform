using System;
using UnityEngine;

namespace FoundationPlatform.Animation
{
	/// <summary>
	/// Animation mask for specifying which body parts an animation affects.
	/// </summary>
	public enum AnimationMask
	{
		FullBody = 0,
		[System.Obsolete("Use AnimationMask.FullBody instead.")]
		Null = 0,
		Arm,
		UpperBody,
		RightHand,
		LeftHand,
		UpperBodyWithRoot,
		RightArm,
		LeftArm,
		UpperBodyWithoutArm,
		LowerBody,
		LowerBodyWithRoot,
		LeftLeg,
		RightLeg
	}

	/// <summary>
	/// Persistent masked equipment overlay played on the loop layer above locomotion blending.
	/// </summary>
	public readonly struct EquipmentOverlaySpec
	{
		public EquipmentOverlaySpec(AnimationClip clip, AnimationMask mask)
		{
			Clip = clip;
			Mask = mask;
		}

		public AnimationClip Clip { get; }
		public AnimationMask Mask { get; }
	}

	/// <summary>
	/// Animation clip information wrapper.
	/// </summary>
	[Serializable]
	public class AnimationClipInfo
	{
		public AnimationClip clip;
		public Vector2 transitionInAndOut = new Vector2(0.2f, 0.2f);

		public static implicit operator AnimationClip(AnimationClipInfo info)
		{
			return info?.clip;
		}
	}

	/// <summary>
	/// Action data for triggering callbacks during animation playback.
	/// </summary>
	[Serializable]
	public class ActionData
	{
		[Range(0, 1)]
		public float normalizeTime;
		public Action action;
		public bool actionInvoked;
	}
}
