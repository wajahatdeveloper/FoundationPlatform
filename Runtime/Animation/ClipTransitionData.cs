using System;
using FoundationPlatform.FrameworkInspector;
using UnityEngine;

namespace FoundationPlatform.Animation
{
	/// <summary>
	///  A single named event fired at a point along a clip's timeline. The <see cref="eventName"/> is
	///  fired into the graph's event dispatcher, invoking any callback registered via
	///  <c>AnimatorBridgeBase.AddEventCallback(eventName, ...)</c>. Because AnimationSets are shared
	///  assets, events are matched by name only — never by direct object/delegate reference.
	/// </summary>
	[Serializable]
	public class AnimationClipEvent
	{
		[Tooltip("Event name fired into the animation event dispatcher. Code subscribes to this name via AddEventCallback.")]
		[ValueDropdown("@FoundationPlatform.AnimationEventNameCatalog.AllNames()", AppendNextDrawer = true)]
		public string eventName;

		[Tooltip("Normalized position along the clip (0 = start, 1 = end) at which the event fires. Re-fires each loop for looping clips.")]
		[PropertyRange(0f, 1f)]
		public float normalizedTime;
	}

	[Serializable]
	public class ClipTransitionData
	{
		[Tooltip("The animation clip to play.")]
		public AnimationClip Clip;

		[Tooltip("How long the transition into this clip should take (in seconds).")]
		public float FadeDuration = 0.25f;

		[Tooltip("The playback speed of the animation.")]
		public float Speed = 1f;

		[Tooltip("Named events fired at authored points along the clip. Wire them to code with AddEventCallback(eventName, ...).")]
		public AnimationClipEvent[] events = Array.Empty<AnimationClipEvent>();

		public bool IsLooping => Clip != null && Clip.isLooping;

		public bool HasEvents => events != null && events.Length > 0;
	}
}
