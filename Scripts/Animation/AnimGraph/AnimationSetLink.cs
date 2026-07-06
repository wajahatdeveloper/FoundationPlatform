using System;
using UnityEngine;

namespace FoundationPlatform.Animation
{
	public enum LinkHoldMode
	{
		FreezeFrame,
		LoopClip,
		LoopSegment
	}

	public struct AnimationSetLinkHoldPlayback
	{
		public bool IsActive;
		public float HoldStartNormalizedTime;
		public float HoldDurationSeconds;
		public LinkHoldMode HoldMode;

		public static AnimationSetLinkHoldPlayback Inactive => default;
	}

	[Serializable]
	public class AnimationSetLink
	{
		[Tooltip("Next entry id after this clip finishes when using PlayFromSetSequence. Ignored by standalone PlayFromSet.")]
		public string nextEntryId;

		[Tooltip("Blend duration into next entry (seconds). 0 uses the next entry clip TransitionInAndOut X.")]
		[Min(0f)]
		public float transitionIn;

		[Tooltip("Blend duration out of this entry when handing off to next (seconds). 0 uses this entry clip TransitionInAndOut Y.")]
		[Min(0f)]
		public float transitionOut;

		[Tooltip("On the penultimate step's link: when true, the terminal entry's transitionBack controls blend release after the sequence handoff. When false, the terminal step never releases to blend (overlay holds until another play or ReleaseToBlendLayer). Mid-chain steps always hand off without releasing. Ignored by standalone PlayFromSet.")]
		public bool useEntryTransitionBackForTerminal = true;

		[Tooltip("When enabled on a non-terminal sequence step: pause or loop at Hold Start Normalized Time for Hold Duration before handing off to Next Entry Id. Ignored by standalone PlayFromSet.")]
		public bool useLinkHold;

		[Tooltip("Normalized clip time (0–1) where the link hold phase begins. Timeline events at or before this time still fire during normal playback.")]
		[Range(0f, 1f)]
		public float holdStartNormalizedTime = 0.9f;

		[Tooltip("How long the hold phase lasts in playback-scaled seconds (same delta as Animancer clip playback for this step).")]
		[Min(0f)]
		public float holdDurationSeconds = 0.1f;

		[Tooltip("FreezeFrame: hold the pose at Hold Start. LoopClip: loop the full clip during hold. LoopSegment: loop from Hold Start to clip end during hold.")]
		public LinkHoldMode holdMode = LinkHoldMode.FreezeFrame;

		public bool HasNext => !string.IsNullOrWhiteSpace(nextEntryId);
	}
}
