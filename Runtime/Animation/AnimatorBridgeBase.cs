using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using AetherNexus.FoundationPlatform.AetherInspector;
using AetherNexus.FoundationPlatform;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Animation
{
	/// <summary>
	///  Centralized wrapper around Unity's Animator.
	///  All reads/writes and state queries for an Animator should go through this handler or a subclass.
	///  <para>
	///  Set-entry playback has two halves. The gameplay half — completion callbacks, named clip events,
	///  clip <see cref="AnimationEvent"/>s and the clip time root motion is sampled at — runs on a timeline
	///  advanced only by <see cref="AdvanceGameplayTimeline"/> from the simulation step, and runs whether or
	///  not the graph exists. The graph is presentation: it is played when it is live and may be throttled,
	///  culled or rebuilt without changing anything the simulation sees.
	///  </para>
	/// </summary>
	[RequireComponent(typeof(PlayableGraphBridge))]
	[RequireComponent(typeof(Animator))]
	public abstract class AnimatorBridgeBase : MonoBehaviour
	{
		/// <summary>True while the presentation graph is built and bound. Gameplay playback does not depend on it.</summary>
		public bool IsReady { get; protected set; }

		protected void AssertReady()
		{
			if (!IsReady) throw new InvalidOperationException($"{GetType().Name} on '{name}': animation system is not ready.");
		}
		
		[SerializeField] protected Animator animator;
		protected int layerCount;
		protected PlayableGraphBridge animancer;

		[SerializeField]
		[LabelText("Registered Overlay Sets (auto)")]
		[Tooltip("Lookup table for PlayFromSetStrict / equipment / combat overlay sets. Populated automatically as sets are registered — designers normally leave this alone.")]
		protected List<AnimationSet> animationSets = new List<AnimationSet>();

		public Action OnAnimatorMove_Event;
		public Action<int> OnAnimatorIK_Event; // int layerIndex

		public PlayableState ActiveSequenceState { get; protected set; }

		private int _activeSequenceGeneration;
		private int _animationSetPlayGeneration;
		private int _activeSequenceLayerIndex = -1;
		private Dictionary<string, AnimationSet> _animationSetByName;
		private Dictionary<AnimationClip, (string setName, string entryId)> _clipToSetEntry;

		private static readonly object AvatarMaskCacheLock = new object();
		private static AvatarMask[] _avatarMaskCache;

		private struct TimelineSlot
		{
			public bool Active;
			public AnimationSetEntry Entry;
			public float StartClipTime;
			public float ClipTime;
			public float HoldClipTime;
			public float Speed;
			public float Elapsed;
			public float CompleteAt;
			public bool EndsOnComplete;
			public bool Started;
			public bool Presented;
			public Action OnComplete;
		}

		private readonly TimelineSlot[] _timeline = new TimelineSlot[AnimLayer.ActionOneShot + 1];

		// AnimationClip.events copies the array on every read.
		private static readonly Dictionary<AnimationClip, AnimationEvent[]> ClipEventCache = new Dictionary<AnimationClip, AnimationEvent[]>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			ClipEventCache.Clear();
			lock (AvatarMaskCacheLock)
				_avatarMaskCache = null;
		}

		protected bool CanPlayPresentation => IsReady && animancer != null && animancer.IsValid;

		/// <summary>
		///  Direct access to the underlying Unity Animator.
		///  Prefer using other gateway methods on this handler where possible.
		/// </summary>
		public Animator Animator => animator;

		public PlayableGraphBridge Animancer => animancer;

		/// <summary>Read-only view of the assigned animation sets, for editor tooling (e.g. the Animation Test Bench).</summary>
		public IReadOnlyList<AnimationSet> AnimationSets => animationSets;

		/// <summary>
		///  Number of layers on the underlying Animator. Returns 0 if using an Animancer-only setup (no AnimatorController).
		/// </summary>
		public int LayerCount => layerCount;

		protected virtual void Awake()
		{
			if (animancer == null) { animancer = GetComponent<PlayableGraphBridge>(); }
			if (animator == null) { animator = GetComponent<Animator>(); }
			if (animator == null)
			{
				throw new MissingComponentException(
					$"{nameof(AnimatorBridgeBase)} requires an {nameof(Animator)} component on the same GameObject.");
			}
			else
			{
				layerCount = animator.runtimeAnimatorController != null ? animator.layerCount : 0;
			}
		}

		#region Animator properties

		/// <summary>
		///  Gets or sets the playback speed of the Animator.
		/// </summary>
		public float Speed
		{
			get => animator.speed;
			set => animator.speed = value;
		}

		/// <summary>
		///  Gets or sets whether root motion is applied by the Animator.
		/// </summary>
		public bool ApplyRootMotion
		{
			get => animator.applyRootMotion;
			set => animator.applyRootMotion = value;
		}

		/// <summary>
		///  Gets or sets the Animator update mode.
		/// </summary>
		public AnimatorUpdateMode UpdateMode
		{
			get => animator.updateMode;
			set => animator.updateMode = value;
		}

		/// <summary>
		///  Gets or sets the Animator culling mode.
		/// </summary>
		public AnimatorCullingMode CullingMode
		{
			get => animator.cullingMode;
			set => animator.cullingMode = value;
		}

		/// <summary>
		///  World-space root position of the Animator hierarchy.
		/// </summary>
		public Vector3 RootPosition
		{
			get => animator.rootPosition;
			set => animator.rootPosition = value;
		}

		/// <summary>
		///  World-space root rotation of the Animator hierarchy.
		/// </summary>
		public Quaternion RootRotation
		{
			get => animator.rootRotation;
			set => animator.rootRotation = value;
		}

		#endregion

		private AnimationSet FindAnimationSetByName(string setName)
		{
			if (_animationSetByName == null) RebuildAnimationSetLookup();
			return _animationSetByName.TryGetValue(setName, out var set) ? set : null;
		}



		private void RebuildAnimationSetLookup()
		{
			_animationSetByName = new Dictionary<string, AnimationSet>(animationSets.Count, StringComparer.Ordinal);
			_clipToSetEntry = new Dictionary<AnimationClip, (string, string)>();

			// Process in topological order (ancestors before descendants) and overwrite without ContainsKey check,
			// so child sets always take precedence over parent sets for inherited entries in _clipToSetEntry.
			// This prevents inherited clips from being attributed to the parent set when both are registered.
			var sorted = GetTopologicallySortedSets();
			foreach (var animationSet in sorted)
			{
				_animationSetByName.TryAdd(animationSet.name, animationSet);
				foreach (var animationSetEntry in animationSet.GetResolvedEntries().Values)
				{
					if (animationSetEntry.clip?.Clip != null)
						_clipToSetEntry[animationSetEntry.clip.Clip] = (animationSet.name, animationSetEntry.id);
				}
			}
		}

		private List<AnimationSet> GetTopologicallySortedSets()
		{
			var result = new List<AnimationSet>(animationSets.Count);
			var visited = new HashSet<AnimationSet>();
			var registered = new HashSet<AnimationSet>();
			foreach (var s in animationSets)
				if (s != null) registered.Add(s);

			foreach (var set in animationSets)
			{
				if (set != null)
					VisitAnimationSet(set, registered, visited, result);
			}
			return result;
		}

		private static void VisitAnimationSet(AnimationSet set, HashSet<AnimationSet> registered, HashSet<AnimationSet> visited, List<AnimationSet> result)
		{
			if (!visited.Add(set)) return;
			if (set.parentSet != null && registered.Contains(set.parentSet))
				VisitAnimationSet(set.parentSet, registered, visited, result);
			result.Add(set);
		}

		/// <summary>Call after mutating <see cref="animationSets"/> so the name→set and clip→entry caches stay coherent.</summary>
		protected void InvalidateAnimationSetLookup()
		{
			_animationSetByName = null;
			_clipToSetEntry = null;
		}

		private static bool IsPlayableAnimationSetEntry(AnimationSetEntry entry)
		{
			if (entry == null) return false;
			if (entry.clip == null) return false;
			if (entry.clip.Clip == null) return false;
			return true;
		}

		/// <summary>True when <paramref name="setName"/> is registered and <paramref name="entryId"/> has a clip.</summary>
		protected bool HasPlayableSetEntry(string setName, string entryId)
		{
			if (string.IsNullOrEmpty(setName) || string.IsNullOrEmpty(entryId)) return false;
			AnimationSet set = FindAnimationSetByName(setName);
			return IsPlayableAnimationSetEntry(set?.FindEntry(entryId));
		}

		/// <summary>Called immediately before an AnimationSet entry begins playback.</summary>
		protected virtual void OnAnimationSetEntryPlayStarted(AnimationSetEntry entry)
		{
			_animationSetPlayGeneration++;
		}

		protected int AnimationSetPlayGeneration => _animationSetPlayGeneration;

		protected bool IsCurrentAnimationSetPlay(int generation) => generation == _animationSetPlayGeneration;

		/// <summary>Wraps the on-complete callback for AnimationSet entry playback.</summary>
		protected virtual Action WrapAnimationSetEntryOnComplete(AnimationSetEntry entry, Action onComplete) => onComplete;

		/// <summary>Optional per-frame callback while a set entry plays (normalized time, elapsed seconds).</summary>
		protected virtual Action<float, float> GetAnimationUpdateCallbackForEntry(AnimationSetEntry entry) => null;

		/// <summary>
		/// A clip <see cref="AnimationEvent"/> crossed on the gameplay timeline. The graph's own copy of the
		/// same event is presentation and must not reach gameplay listeners.
		/// </summary>
		protected virtual void OnGameplayAnimationEvent(AnimationEvent animationEvent) { }

		// ─── Gameplay timeline ──────────────────────────────────────────────

		/// <summary>
		/// Advances every set entry playing on the gameplay timeline by one simulation step. Completions due
		/// from the previous step fire first, so a root-motion clip's final segment is still consumed by the
		/// motor on the step it reaches the end.
		/// </summary>
		public void AdvanceGameplayTimeline(float deltaTime)
		{
			for (var layer = 0; layer < _timeline.Length; layer++)
			{
				ref var slot = ref _timeline[layer];
				if (!slot.Active || slot.OnComplete == null || slot.Elapsed < slot.CompleteAt)
					continue;

				var onComplete = slot.OnComplete;
				slot.OnComplete = null;
				if (slot.EndsOnComplete)
					slot = default;
				onComplete();
			}

			for (var layer = 0; layer < _timeline.Length; layer++)
			{
				ref var slot = ref _timeline[layer];
				if (!slot.Active)
					continue;

				var entry = slot.Entry;
				var from = slot.ClipTime;
				var firstStep = !slot.Started;
				slot.Started = true;
				slot.Elapsed += deltaTime;
				slot.ClipTime = ResolveClipTime(slot);
				FireCrossedEvents(entry, from, slot.ClipTime, firstStep);
			}
		}

		/// <summary>Clip time, in seconds, the gameplay timeline has reached for the entry playing on <paramref name="layerIndex"/>.</summary>
		public bool TryGetGameplayClipTime(int layerIndex, out AnimationSetEntry entry, out float clipTime)
		{
			var slot = _timeline[layerIndex];
			entry = slot.Entry;
			clipTime = slot.ClipTime;
			return slot.Active;
		}

		protected void CancelGameplayTimeline(int layerIndex) => _timeline[layerIndex] = default;

		/// <summary>
		/// Multiplies the playback rate of the entry playing on <paramref name="layerIndex"/>: the gameplay timeline
		/// (completion, events, root motion) and the presented <paramref name="state"/> together, so they stay in step.
		/// <paramref name="state"/> is null when the entry was started without presentation.
		/// </summary>
		protected void ScaleEntryPlaybackRate(int layerIndex, PlayableState state, float rate)
		{
			if (rate <= 0f)
				throw new ArgumentOutOfRangeException(nameof(rate), rate, $"{nameof(AnimatorBridgeBase)} on '{name}': playback rate must be positive.");

			ref var slot = ref _timeline[layerIndex];
			if (!slot.Active)
				throw new InvalidOperationException($"{nameof(AnimatorBridgeBase)} on '{name}': no entry is playing on layer {layerIndex} to rescale.");

			slot.Speed *= rate;
			if (!float.IsPositiveInfinity(slot.CompleteAt))
				slot.CompleteAt = slot.Elapsed + (slot.CompleteAt - slot.Elapsed) / rate;
			if (state != null)
				state.Speed *= rate;
		}

		private void StartGameplayTimeline(int layerIndex, AnimationSetEntry entry, float startNormalizedTime, float fadeSeconds, Action onComplete)
		{
			StartGameplayTimeline(layerIndex, entry, startNormalizedTime, fadeSeconds, onComplete, float.PositiveInfinity, 0f);
		}

		private void StartGameplayTimeline(int layerIndex, AnimationSetEntry entry, float startNormalizedTime, float fadeSeconds,
		                                   Action onComplete, float holdClipTime, float holdSeconds)
		{
			var clip = entry.clip.Clip;
			var speed = entry.clip.Speed;
			if (speed <= 0f)
			{
				throw new InvalidOperationException(
					$"{nameof(AnimatorBridgeBase)} on '{name}': entry '{entry.id}' has playback speed {speed}. Gameplay timing needs a positive speed.");
			}

			var startClipTime = Mathf.Clamp01(startNormalizedTime) * clip.length;
			float completeAt;
			if (entry.clip.IsLooping)
			{
				// A looping state's completion is the end of its blend in, not of the clip.
				completeAt = Mathf.Max(0f, fadeSeconds);
			}
			else if (!float.IsPositiveInfinity(holdClipTime))
			{
				completeAt = (Mathf.Max(0f, holdClipTime - startClipTime) + holdSeconds) / speed;
			}
			else
			{
				completeAt = Mathf.Max(0f, clip.length - startClipTime) / speed;
			}

			_timeline[layerIndex] = new TimelineSlot
			{
				Active = true,
				Entry = entry,
				StartClipTime = startClipTime,
				ClipTime = startClipTime,
				HoldClipTime = holdClipTime,
				Speed = speed,
				CompleteAt = completeAt,
				EndsOnComplete = !entry.clip.IsLooping,
				Presented = true,
				OnComplete = onComplete,
			};
		}

		private static float ResolveClipTime(in TimelineSlot slot)
		{
			var clipTime = slot.StartClipTime + slot.Elapsed * slot.Speed;
			if (clipTime > slot.HoldClipTime)
				clipTime = slot.HoldClipTime;
			if (!slot.Entry.clip.IsLooping)
				clipTime = Mathf.Min(clipTime, slot.Entry.clip.Clip.length);
			return clipTime;
		}

		// Looping clips keep an unwrapped clip time, so an event re-fires on every cycle crossed.
		private void FireCrossedEvents(AnimationSetEntry entry, float from, float to, bool includeFrom)
		{
			var clip = entry.clip.Clip;
			var length = clip.length;
			if (length <= 0f || to < from)
				return;

			var firstCycle = Mathf.FloorToInt(from / length);
			var lastCycle = Mathf.FloorToInt(to / length);
			if (!entry.clip.IsLooping)
			{
				firstCycle = 0;
				lastCycle = 0;
			}

			for (var cycle = firstCycle; cycle <= lastCycle; cycle++)
			{
				var offset = cycle * length;

				if (entry.clip.HasEvents)
				{
					var named = entry.clip.events;
					for (var i = 0; i < named.Length; i++)
					{
						var evt = named[i];
						if (evt == null || string.IsNullOrWhiteSpace(evt.eventName))
							continue;
						if (IsCrossed(offset + Mathf.Clamp01(evt.normalizedTime) * length, from, to, includeFrom))
							animancer.Events.Fire(evt.eventName);
					}
				}

				var clipEvents = GetClipEvents(clip);
				for (var i = 0; i < clipEvents.Length; i++)
				{
					if (IsCrossed(offset + clipEvents[i].time, from, to, includeFrom))
						OnGameplayAnimationEvent(clipEvents[i]);
				}
			}
		}

		private static bool IsCrossed(float time, float from, float to, bool includeFrom) =>
			(includeFrom ? time >= from : time > from) && time <= to;

		public static AnimationEvent[] GetClipEvents(AnimationClip clip)
		{
			if (!ClipEventCache.TryGetValue(clip, out var events))
			{
				events = clip.events;
				ClipEventCache.Add(clip, events);
			}

			return events;
		}

		private void CancelActiveSetSequence()
		{
			_activeSequenceGeneration++;
			if (_activeSequenceLayerIndex >= 0)
			{
				CancelGameplayTimeline(_activeSequenceLayerIndex);

				// Layer 0 (Locomotion) must not be stopped here; its transition back to the default
				// state (e.g. stance mixer) is the caller's responsibility via TransitionBackFromLayer.
				if (_activeSequenceLayerIndex != AnimLayer.Locomotion && CanPlayPresentation)
					animancer.Layers[_activeSequenceLayerIndex].Stop();
			}
			_activeSequenceLayerIndex = -1;
			ActiveSequenceState = null;
		}

		/// <summary>
		/// Strict variant; throws when setup or sequence data is invalid.
		/// </summary>
		public void PlayFromSetSequenceStrict(string setName, string firstEntryId, Action onComplete)
		{
			var sequenceIds = ResolveSequencePlayback(setName, firstEntryId);
			CancelActiveSetSequence();
			PlaySequenceStep(0, setName, sequenceIds, onComplete, _activeSequenceGeneration);
		}

		public void CancelActiveSetSequencePlayback()
		{
			CancelActiveSetSequence();
		}

		/// <summary>
		/// Plays a sequence from the primary set when the chain resolves there; otherwise uses <paramref name="fallbackSetName"/>.
		/// </summary>
		public void PlayFromSetSequenceWithFallbackStrict(string primarySetName, string fallbackSetName, string firstEntryId,
		                                                Action onComplete)
		{
			AnimationSet primary = FindAnimationSetByName(primarySetName);
			if (primary == null)
			{
				throw new InvalidOperationException($"AnimatorBridgeBase: Animation set '{primarySetName}' not found.");
			}

			AnimationSetEntry entry = primary?.FindEntry(firstEntryId);
			if (IsPlayableAnimationSetEntry(entry))
			{
				PlayFromSetSequenceStrict(primarySetName, firstEntryId, onComplete);
				return;
			}

			PlayFromSetSequenceStrict(fallbackSetName, firstEntryId, onComplete);
		}

		private IReadOnlyList<string> ResolveSequencePlayback(string setName, string firstEntryId)
		{
			AnimationSet set = FindAnimationSetByName(setName);
			if (set == null)
			{
				throw new InvalidOperationException($"AnimatorBridgeBase: Animation set '{setName}' not found.");
			}

			return AnimationSetSequenceUtility.CollectSequenceEntryIds(set, firstEntryId);
		}

		private void PlaySequenceStep(int stepIndex, string setName, IReadOnlyList<string> sequenceIds, Action onComplete, int generation)
		{
			if (generation != _activeSequenceGeneration) return;

			AnimationSet set = FindAnimationSetByName(setName);
			var entryId = sequenceIds[stepIndex];
			var entry = set?.FindEntry(entryId);
			var isTerminal = stepIndex == sequenceIds.Count - 1;

			AnimationSetSequenceUtility.ValidateSequenceEntryForPlayback(entry, set.name, isTerminal);

			var sourceEntry = stepIndex > 0 ? set?.FindEntry(sequenceIds[stepIndex - 1]) : null;

			if (sourceEntry != null && sourceEntry.mask != entry.mask)
			{
				throw new InvalidOperationException(
					$"AnimatorBridgeBase: sequence mask mismatch between '{sourceEntry.id}' and '{entry.id}' in set '{set.name}'.");
			}

			float transitionIn = (stepIndex == 0)
				? entry.clip.FadeDuration
				: AnimationSetSequenceUtility.ResolveTransitionInForLink(sourceEntry, entry);

			var transitionBack = isTerminal
				&& AnimationSetSequenceUtility.ResolveTerminalTransitionBack(entry, sourceEntry);

			OnAnimationSetEntryPlayStarted(entry);
			int playGeneration = AnimationSetPlayGeneration;

			var layerIndex = ResolveLayerIndex(entry);
			if (sourceEntry != null)
			{
				var sourceLayerIndex = ResolveLayerIndex(sourceEntry);
				if (sourceLayerIndex != layerIndex)
					CancelGameplayTimeline(sourceLayerIndex);
			}

			_activeSequenceLayerIndex = layerIndex;

			var wrappedOnComplete = WrapAnimationSetEntryOnComplete(entry, () =>
			{
				if (generation != _activeSequenceGeneration) return;
				if (!IsCurrentAnimationSetPlay(playGeneration)) return;

				if (isTerminal)
				{
					if (transitionBack)
					{
						// Release back to the blend/base layer using a transition-OUT duration for
						// the terminal entry, not the transition-IN computed for blending into this step.
						float releaseFade = AnimationSetSequenceUtility.ResolveTransitionOutForLink(entry);
						TransitionBackFromLayer(layerIndex, releaseFade);
					}
					CancelActiveSetSequence();
					onComplete?.Invoke();
				}
				else
				{
					PlaySequenceStep(stepIndex + 1, setName, sequenceIds, onComplete, generation);
				}
			});

			// A link hold freezes the step at its hold point for a fixed time before the next step plays.
			var holds = entry.link != null && entry.link.useLinkHold && !isTerminal;
			var holdClipTime = holds
				? Mathf.Clamp01(entry.link.holdStartNormalizedTime) * entry.clip.Clip.length
				: float.PositiveInfinity;
			var holdSeconds = holds ? entry.link.holdDurationSeconds : 0f;
			StartGameplayTimeline(layerIndex, entry, 0f, transitionIn, wrappedOnComplete, holdClipTime, holdSeconds);

			if (!CanPlayPresentation)
			{
				ActiveSequenceState = null;
				return;
			}

			var layer = animancer.Layers[layerIndex];
			layer.Mask = ResolveAvatarMask(entry);

			if (sourceEntry != null)
			{
				var sourceLayerIndex = ResolveLayerIndex(sourceEntry);
				if (sourceLayerIndex != layerIndex)
				{
					animancer.Layers[sourceLayerIndex].StartFade(0f, transitionIn);
					// Layer 0 is always at weight 1; StartFade is only needed for non-locomotion layers.
					if (layerIndex != AnimLayer.Locomotion)
						layer.StartFade(1f, transitionIn);
				}
			}
			else
			{
				// Layer 0 (Locomotion) always has the stance mixer present, so layer.Play() will
				// cross-fade from it naturally with no bind-pose gap. Non-locomotion layers need
				// their weight set explicitly before playing.
				if (layerIndex != AnimLayer.Locomotion)
					layer.Weight = 1f;
			}

			var state = layer.Play(entry.clip, transitionIn);
			ActiveSequenceState = state;

			if (holds)
			{
				state.Events().Add(entry.link.holdStartNormalizedTime, () =>
				{
					if (generation == _activeSequenceGeneration)
						state.Speed = 0f;
				});
			}
		}

		private PlayableState PlayFromPlayableAnimationSetEntry(AnimationSet set, AnimationSetEntry entry, Action onComplete)
		{
			return PlayFromPlayableAnimationSetEntry(set, entry, onComplete, Mathf.Clamp01(entry.startNormalizedTime));
		}

		private PlayableState PlayFromPlayableAnimationSetEntry(AnimationSet set, AnimationSetEntry entry, Action onComplete, float startNormalizedTime)
		{
			return PlayFromPlayableAnimationSetEntry(set, entry, onComplete, startNormalizedTime, -1f);
		}

		private PlayableState PlayFromPlayableAnimationSetEntry(AnimationSet set, AnimationSetEntry entry, Action onComplete, float startNormalizedTime, float fadeDurationSeconds)
		{
			return PlayFromPlayableAnimationSetEntry(set, entry, onComplete, startNormalizedTime, fadeDurationSeconds, true);
		}

		// fadeDurationSeconds < 0 keeps the entry's authored ClipTransitionData fade. present = false runs the
		// gameplay timeline only: completion, events and root motion as normal, nothing played on the graph.
		private PlayableState PlayFromPlayableAnimationSetEntry(AnimationSet set, AnimationSetEntry entry, Action onComplete, float startNormalizedTime, float fadeDurationSeconds, bool present)
		{
			if (entry.clip.IsLooping && onComplete != null)
			{
				string id = string.IsNullOrEmpty(entry.id) ? entry.clip.Clip.name : entry.id;
				Debug.LogWarning(
					$"AnimatorBridgeBase: AnimationSet entry '{id}' is marked looping but an OnComplete callback was provided. " +
					"Looping playback ends the blend transition, so OnComplete can run long before the motion finishes. " +
					"Use isLooping = false for one-shot clips (e.g. jump) that chain to another state.",
					this);
			}

			OnAnimationSetEntryPlayStarted(entry);
			int playGeneration = AnimationSetPlayGeneration;
			var wrappedOnComplete = WrapAnimationSetEntryOnComplete(entry, onComplete);

			var layerIndex = ResolveLayerIndex(entry);
			var fade = fadeDurationSeconds >= 0f ? fadeDurationSeconds : entry.clip.FadeDuration;

			Action timelineComplete = wrappedOnComplete;
			if (!entry.clip.IsLooping)
			{
				timelineComplete = () =>
				{
					if (!IsCurrentAnimationSetPlay(playGeneration))
						return;
					if (entry.transitionBack)
						TransitionBackFromLayer(layerIndex, entry.clip.FadeDuration);
					wrappedOnComplete?.Invoke();
				};
			}

			StartGameplayTimeline(layerIndex, entry, startNormalizedTime, fade, timelineComplete);
			_timeline[layerIndex].Presented = present;

			if (!present || !CanPlayPresentation)
				return null;

			var layer = animancer.Layers[layerIndex];
			layer.Mask = ResolveAvatarMask(entry);

			// A previous TransitionBackFromLayer fades overlay-layer weight to 0; restore it or
			// this play is invisible. Locomotion (layer 0) is pinned at weight 1 by design.
			if (layerIndex != AnimLayer.Locomotion)
				layer.Weight = 1f;

			var state = layer.Play(entry.clip, fadeDurationSeconds);
			if (startNormalizedTime > 0f)
			{
				state.NormalizedTime = startNormalizedTime;
			}
			else if (!entry.clip.IsLooping)
			{
				state.NormalizedTime = 0f;
			}

			// Presentation only: the graph may reach its end before or after the timeline does. Whichever
			// is first releases the pose; the gameplay completion always comes from the timeline.
			if (!entry.clip.IsLooping && entry.transitionBack)
			{
				state.Events().OnEnd = () =>
				{
					if (IsCurrentAnimationSetPlay(playGeneration))
						TransitionBackFromLayer(layerIndex, entry.clip.FadeDuration);
				};
			}

			return state;
		}

		/// <summary>
		/// Replays whatever the gameplay timeline is running onto a freshly built graph, at the clip time the
		/// timeline has reached, so a unit whose graph was culled or rebuilt resumes its pose mid-clip.
		/// </summary>
		protected void ResyncPresentationToTimeline()
		{
			if (!CanPlayPresentation)
				return;

			for (var layerIndex = 0; layerIndex < _timeline.Length; layerIndex++)
			{
				var slot = _timeline[layerIndex];
				if (!slot.Active || !slot.Presented)
					continue;

				var layer = animancer.Layers[layerIndex];
				layer.Mask = ResolveAvatarMask(slot.Entry);
				if (layerIndex != AnimLayer.Locomotion)
					layer.Weight = 1f;

				var state = layer.Play(slot.Entry.clip, 0f);
				var length = slot.Entry.clip.Clip.length;
				state.NormalizedTime = slot.Entry.clip.IsLooping
					? Mathf.Repeat(slot.ClipTime, length) / length
					: Mathf.Clamp01(slot.ClipTime / length);
				if (slot.ClipTime >= slot.HoldClipTime)
					state.Speed = 0f;
			}
		}

		/// <summary>
		///  Strict preview-only seek path for editor scrubbing.
		///  Uses zero blend and no transition-back so normalized-time seeks are deterministic in both directions.
		/// </summary>
		public void PlayFromSetPreviewStrict(string setName, string entryId, float startNormalizedTime)
		{
			AnimationSet set = FindAnimationSetByName(setName);
			if (set == null)
				throw new InvalidOperationException($"AnimatorBridgeBase: Animation set '{setName}' not found.");
			AnimationSetEntry entry = set?.FindEntry(entryId);
			if (entry == null)
				throw new InvalidOperationException($"AnimatorBridgeBase: Entry '{entryId}' not found in set '{setName}'.");
			if (entry.clip == null)
				throw new InvalidOperationException($"AnimatorBridgeBase: Entry '{entryId}' in set '{setName}' has no clip data.");
			if (entry.clip.Clip == null)
				throw new InvalidOperationException($"AnimatorBridgeBase: Entry '{entryId}' in set '{setName}' has no clip assigned.");

			var previewLayerIndex = ResolveLayerIndex(entry);
			var layer = animancer.Layers[previewLayerIndex];
			layer.Mask = ResolveAvatarMask(entry);
			if (previewLayerIndex != AnimLayer.Locomotion)
				layer.Weight = 1f;
			var state = layer.Play(entry.clip, 0f);
			state.NormalizedTime = Mathf.Clamp01(startNormalizedTime);
			animancer.Evaluate();
		}

		/// <summary>Scales Animancer clip timer (0 = pause) for debugging; does not affect locomotion-only paths.</summary>
		public void SetDebugPlaybackTimeScale(float scale)
		{
			animancer.Speed = scale;
		}

		/// <summary>
		/// Plays an animation from an assigned AnimationSet by set name and entry id at runtime.
		/// </summary>
		public PlayableState PlayFromSetStrict(string setName, string entryId, Action onComplete)
		{
			CancelActiveSetSequence();
			AnimationSet set = FindAnimationSetByName(setName);
			if (set == null) throw new InvalidOperationException($"AnimatorBridgeBase: Animation set '{setName}' not found.");
			AnimationSetEntry entry = set?.FindEntry(entryId);
			if (entry == null) throw new InvalidOperationException($"AnimatorBridgeBase: Entry '{entryId}' not found in set '{setName}'.");
			if (entry.clip == null) throw new InvalidOperationException($"AnimatorBridgeBase: Entry '{entryId}' in set '{setName}' has no clip data.");
			if (entry.clip.Clip == null) throw new InvalidOperationException($"AnimatorBridgeBase: Entry '{entryId}' in set '{setName}' has no clip assigned.");
			return PlayFromPlayableAnimationSetEntry(set, entry, onComplete);
		}

		/// <summary>
		///  <see cref="PlayFromSetStrict(string, string, Action)"/> with optional normalized start time into the clip (for debugging / scrub).
		/// </summary>
		public PlayableState PlayFromSetStrict(string setName, string entryId, Action onComplete, float startNormalizedTime)
		{
			CancelActiveSetSequence();
			AnimationSet set = FindAnimationSetByName(setName); if (set == null)
				throw new InvalidOperationException($"AnimatorBridgeBase: Animation set '{setName}' not found.");
			AnimationSetEntry entry = set?.FindEntry(entryId); if (entry == null) throw new InvalidOperationException($"AnimatorBridgeBase: Entry '{entryId}' not found in set '{setName}'.");
			if (entry.clip == null) throw new InvalidOperationException($"AnimatorBridgeBase: Entry '{entryId}' in set '{setName}' has no clip data.");
			if (entry.clip.Clip == null) throw new InvalidOperationException($"AnimatorBridgeBase: Entry '{entryId}' in set '{setName}' has no clip assigned.");
			return PlayFromPlayableAnimationSetEntry(set, entry, onComplete, Mathf.Clamp01(startNormalizedTime));
		}

		/// <summary>
		///  <see cref="PlayFromSetStrict(string, string, Action)"/> with an explicit crossfade duration.
		///  Pass a negative value to keep the entry's authored fade. Used when one clip cuts into another
		///  mid-motion (combo cancel), where the blend belongs to the transition, not to the clip.
		/// </summary>
		public PlayableState PlayFromSetStrictWithFade(string setName, string entryId, Action onComplete, float fadeDurationSeconds)
		{
			return PlayFromSetStrictWithFade(setName, entryId, onComplete, fadeDurationSeconds, true);
		}

		/// <summary>
		///  <see cref="PlayFromSetStrictWithFade(string, string, Action, float)"/> that can skip the graph:
		///  <paramref name="present"/> false runs the entry's gameplay timing without drawing it.
		/// </summary>
		public PlayableState PlayFromSetStrictWithFade(string setName, string entryId, Action onComplete, float fadeDurationSeconds, bool present)
		{
			CancelActiveSetSequence();
			AnimationSet set = FindAnimationSetByName(setName);
			if (set == null) throw new InvalidOperationException($"AnimatorBridgeBase: Animation set '{setName}' not found.");
			AnimationSetEntry entry = set?.FindEntry(entryId);
			if (entry == null) throw new InvalidOperationException($"AnimatorBridgeBase: Entry '{entryId}' not found in set '{setName}'.");
			if (entry.clip == null) throw new InvalidOperationException($"AnimatorBridgeBase: Entry '{entryId}' in set '{setName}' has no clip data.");
			if (entry.clip.Clip == null) throw new InvalidOperationException($"AnimatorBridgeBase: Entry '{entryId}' in set '{setName}' has no clip assigned.");
			return PlayFromPlayableAnimationSetEntry(set, entry, onComplete, Mathf.Clamp01(entry.startNormalizedTime), fadeDurationSeconds, present);
		}

		/// <summary>
		/// Strict variant that tries fallback set when primary has no playable entry.
		/// </summary>
		public PlayableState PlayFromSetWithFallbackStrict(string primarySetName, string fallbackSetName, string entryId, Action onComplete)
		{
			AnimationSet primary = FindAnimationSetByName(primarySetName);
			if (primary == null) throw new InvalidOperationException($"AnimatorBridgeBase: Animation set '{primarySetName}' not found.");
			AnimationSetEntry entry = primary?.FindEntry(entryId);
			if (IsPlayableAnimationSetEntry(entry))
			{
				return PlayFromPlayableAnimationSetEntry(primary, entry, onComplete);
			}

			AnimationSet fallback = FindAnimationSetByName(fallbackSetName);
			if (fallback == null) throw new InvalidOperationException($"AnimatorBridgeBase: Fallback animation set '{fallbackSetName}' not found.");
			entry = fallback?.FindEntry(entryId);
			if (!IsPlayableAnimationSetEntry(entry))
				throw new InvalidOperationException($"AnimatorBridgeBase: Entry '{entryId}' has no valid playable entry in '{primarySetName}' or '{fallbackSetName}'.");
			return PlayFromPlayableAnimationSetEntry(fallback, entry, onComplete);
		}

		/// <summary>Appends play-layer debug lines for runtime overlays.</summary>
		public virtual void AppendAnimationDebug(StringBuilder sb)
		{
			sb.AppendLine("--- Play Layers ---");
			if (animancer == null)
			{
				sb.AppendLine("  (no Animancer)");
				return;
			}

			sb.AppendLine(animancer.Graph.ToString());
		}

		/// <summary>O(1) reverse-lookup: maps an AnimationClip to its registered set name and entry id.</summary>
		protected bool TryGetClipEntry(AnimationClip clip, out string setName, out string entryId)
		{
			if (_clipToSetEntry == null)
				RebuildAnimationSetLookup();

			if (_clipToSetEntry.TryGetValue(clip, out var found))
			{
				setName  = found.setName;
				entryId  = found.entryId;
				return true;
			}
			setName  = null;
			entryId  = null;
			return false;
		}

		/// <summary>Resolves the currently playing clip to an AnimationSet set name and entry id, if any.</summary>
		public virtual bool TryGetCurrentSetAndEntry(out string setName, out string entryId)
		{
			setName = null;
			entryId = null;
			
			if (animancer == null) return false;

			if (_clipToSetEntry == null) RebuildAnimationSetLookup();

			if (animancer.Layers != null)
			{
				foreach (var layer in animancer.Layers)
				{
					if (layer.CurrentState != null && layer.CurrentState.Weight > 0.01f && layer.CurrentState is ClipState clipState && clipState.Clip != null)
					{
						if (_clipToSetEntry.TryGetValue(clipState.Clip, out var found))
						{
							setName = found.setName;
							entryId = found.entryId;
							return true;
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Crossfades a raw clip on the graph, presentation only: nothing waits on it and nothing happens when
		/// the graph is not live. Callers that need the clip's end or its events schedule them on the
		/// simulation clock from the clip's length and <see cref="GetClipEvents"/>.
		/// </summary>
		public void PlayPresentationClip(AnimationClip clip, AnimationClipInfo clipInfo, AnimationMask mask, int layerIndex, bool transitionBack)
		{
			if (!CanPlayPresentation)
				return;

			int resolvedLayer;
			if (layerIndex >= 0)
				resolvedLayer = layerIndex;
			else if (mask == AnimationMask.FullBody)
				resolvedLayer = AnimLayer.Locomotion;
			else
				resolvedLayer = AnimLayer.ActionOneShot;

			var layer = animancer.Layers[resolvedLayer];
			layer.Mask = GetAvatarMask(mask);
			var transitionDuration = clipInfo != null ? clipInfo.transitionInAndOut.x : 0.25f;

			if (resolvedLayer != AnimLayer.Locomotion)
				layer.Weight = 1f;

			var state = layer.Play(clip, transitionDuration);

			if (transitionBack)
				state.Events().OnEnd = () => TransitionBackFromLayer(resolvedLayer, transitionDuration);
		}

		public virtual void PlayLoopingAnimation(AnimationClip clip, AnimationMask mask)
		{
			PlayLoopingAnimation(clip, mask, false, 0.1f);
		}

		public virtual void PlayLoopingAnimation(AnimationClip clip, AnimationMask mask, bool isActAsAnimatorOutput)
		{
			PlayLoopingAnimation(clip, mask, isActAsAnimatorOutput, 0.1f);
		}

		public virtual void PlayLoopingAnimation(AnimationClip clip, AnimationMask mask, bool isActAsAnimatorOutput, float transitionIn)
		{
			AssertReady();
			var loopLayerIndex = mask == AnimationMask.FullBody ? AnimLayer.Locomotion : AnimLayer.LoopingOverride;
			var layer = animancer.Layers[loopLayerIndex];
			layer.Mask = GetAvatarMask(mask);
			if (loopLayerIndex != AnimLayer.Locomotion)
				layer.Weight = 1f;
			layer.Play(clip, transitionIn);
		}

		public virtual void StopLoopingAnimations(bool transition)
		{
			animancer.Layers[AnimLayer.LoopingOverride].Stop(transition ? 0.1f : 0f);
			// Full-body looping animations play on layer 0; restore it via the virtual override.
			TransitionBackFromLayer(AnimLayer.Locomotion, transition ? 0.1f : 0f);
		}



		#region Avatar Mask Helpers

		public static class AnimLayer
		{
			public const int Locomotion     = 0;
			public const int LoopingOverride = 1;
			public const int ActionOneShot  = 2;
		}

		/// <summary>
		/// Transitions a layer back to its default state.
		/// Base: stops the layer (for layers 1+). Subclasses override for layer 0 to restore their locomotion state.
		/// </summary>
		protected virtual void TransitionBackFromLayer(int layerIndex, float fadeTime)
		{
			if (layerIndex != AnimLayer.Locomotion && CanPlayPresentation)
				animancer.Layers[layerIndex].StartFade(0f, fadeTime);
		}

		/// <summary>Resolves which Animancer layer index an entry should play on. Entry.layerIndex overrides auto heuristic.</summary>
		protected static int ResolveLayerIndex(AnimationSetEntry entry)
		{
			if (entry.layerIndex >= 0) return entry.layerIndex;
			if (entry.maskAsset == null && entry.mask == AnimationMask.FullBody)
				return AnimLayer.Locomotion;
			return entry.clip.IsLooping ? AnimLayer.LoopingOverride : AnimLayer.ActionOneShot;
		}

		/// <summary>Returns the entry's direct maskAsset if assigned; falls back to enum-based GetAvatarMask.</summary>
		protected AvatarMask ResolveAvatarMask(AnimationSetEntry entry)
		{
			return entry.maskAsset != null ? entry.maskAsset : GetAvatarMask(entry.mask);
		}

		public AvatarMask GetAvatarMask(AnimationMask mask)
		{
			if (mask == AnimationMask.FullBody)
				return null;

			EnsureAvatarMaskCache();
			var idx = (int)mask;
			if (idx < 0 || idx >= _avatarMaskCache.Length || _avatarMaskCache[idx] == null)
				throw new InvalidOperationException(
					$"{nameof(AnimatorBridgeBase)} on '{name}': Avatar mask for {mask} missing from Resources/AvatarMasks/AnimationMask_{mask}.mask.");

			return _avatarMaskCache[idx];
		}

		private static void EnsureAvatarMaskCache()
		{
			if (_avatarMaskCache != null)
				return;

			lock (AvatarMaskCacheLock)
			{
				if (_avatarMaskCache != null)
					return;

				var values = (AnimationMask[])Enum.GetValues(typeof(AnimationMask));
				var max = 0;
				for (var i = 0; i < values.Length; i++)
				{
					var v = (int)values[i];
					if (v > max) max = v;
				}

				var cache = new AvatarMask[max + 1];
				for (var i = 0; i < values.Length; i++)
				{
					var value = values[i];
					if (value == AnimationMask.FullBody)
						continue;

					var path = $"AvatarMasks/AnimationMask_{value}";
					var loaded = Resources.Load<AvatarMask>(path);
					if (loaded == null)
						throw new InvalidOperationException(
							$"{nameof(AnimatorBridgeBase)}: failed to Resources.Load AvatarMask at '{path}'. Expected asset under a Resources/AvatarMasks folder.");
					cache[(int)value] = loaded;
				}

				_avatarMaskCache = cache;
			}
		}

		#endregion
	}
}