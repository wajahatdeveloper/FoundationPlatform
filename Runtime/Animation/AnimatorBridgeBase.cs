using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using FoundationPlatform;
using UnityEngine;
using FoundationPlatform.FrameworkInspector;

namespace FoundationPlatform.Animation
{
	/// <summary>
	///  Centralized wrapper around Unity's Animator.
	///  All reads/writes and state queries for an Animator should go through this handler or a subclass.
	/// </summary>
	[RequireComponent(typeof(PlayableGraphBridge))]
	[RequireComponent(typeof(Animator))]
	public abstract class AnimatorBridgeBase : MonoBehaviour
	{
		public bool IsReady { get; protected set; }

		protected void AssertReady()
		{
			if (!IsReady) throw new InvalidOperationException($"{GetType().Name} on '{name}': animation system is not ready.");
		}
		
		[SerializeField] protected Animator animator;
		protected int layerCount;
		protected PlayableGraphBridge animancer;

		[SerializeField]
		[InspectorName("Animation Sets (Assets)")]
		protected List<AnimationSet> animationSets = new List<AnimationSet>();

		public Action OnAnimatorMove_Event;
		public Action<int> OnAnimatorIK_Event; // int layerIndex

		public PlayableState ActiveSequenceState { get; protected set; }

		private int _activeSequenceGeneration;
		private int _activeSequenceLayerIndex = -1;
		private Dictionary<string, AnimationSet> _animationSetByName;
		private Dictionary<AnimationClip, (string setName, string entryId)> _clipToSetEntry;

		[SerializeField]
		[LabelText("Avatar Masks")]
		[Tooltip("The _serializedMasks array is indexed by (int)AnimationMask. Create these assets and assign to the array in that order:\n\nIndex\tAnimationMask\tMask description\n0\tFullBody\tLeave slot null (FullBody = no mask)\n1\tArm\tBoth arms, both hands IK + fingers\n2\tUpperBody\tBoth arms, hands IK + fingers, body, head\n3\tRightHand\tRightFingers + RightHandIK\n4\tLeftHand\tLeftFingers + LeftHandIK\n5\tUpperBodyWithRoot\tUpperBody + Root node\n6\tRightArm\tRightArm, RightFingers, RightHandIK\n7\tUpperBodyWithoutArm\tBody + Head only\n8\tLowerBody\tBoth legs + feet IK\n9\tLowerBodyWithRoot\tLowerBody + Root node\n10\tLeftLeg\tLeftLeg + LeftFootIK\n11\tRightLeg\tRightLeg + RightFootIK")]
		private AvatarMask[] _serializedMasks;

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
			animator = GetComponent<Animator>();
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

		private static AnimationSetEntry FindEntryById(AnimationSet set, string entryId)
		{
			if (set == null) return null;
			return set.FindEntry(entryId);
		}

		private static bool IsPlayableAnimationSetEntry(AnimationSetEntry entry)
		{
			if (entry == null) return false;
			if (entry.clip == null) return false;
			if (entry.clip.Clip == null) return false;
			return true;
		}

		/// <summary>Called immediately before an AnimationSet entry begins playback.</summary>
		protected virtual void OnAnimationSetEntryPlayStarted(AnimationSetEntry entry) { }

		/// <summary>Wraps the on-complete callback for AnimationSet entry playback.</summary>
		protected virtual Action WrapAnimationSetEntryOnComplete(AnimationSetEntry entry, Action onComplete) => onComplete;

		/// <summary>Optional per-frame callback while a set entry plays (normalized time, elapsed seconds).</summary>
		protected virtual Action<float, float> GetAnimationUpdateCallbackForEntry(AnimationSetEntry entry) => null;

		private void CancelActiveSetSequence()
		{
			_activeSequenceGeneration++;
			if (_activeSequenceLayerIndex >= 0 && animancer != null && animancer.IsValid)
			{
				// Layer 0 (Locomotion) must not be stopped here; its transition back to the default
				// state (e.g. stance mixer) is the caller's responsibility via TransitionBackFromLayer.
				if (_activeSequenceLayerIndex != AnimLayer.Locomotion)
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
			AssertReady();
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
			AssertReady();

			AnimationSet primary = FindAnimationSetByName(primarySetName);
			if (primary == null)
			{
				throw new InvalidOperationException($"AnimatorBridgeBase: Animation set '{primarySetName}' not found.");
			}

			AnimationSetEntry entry = FindEntryById(primary, firstEntryId);
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
			var entry = FindEntryById(set, entryId);
			var isTerminal = stepIndex == sequenceIds.Count - 1;

			AnimationSetSequenceUtility.ValidateSequenceEntryForPlayback(entry, set.name, isTerminal);

			var sourceEntry = stepIndex > 0 ? FindEntryById(set, sequenceIds[stepIndex - 1]) : null;

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

			var layerIndex = ResolveLayerIndex(entry);
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

			_activeSequenceLayerIndex = layerIndex;

			var state = layer.Play(entry.clip, transitionIn);
			ActiveSequenceState = state;

			var wrappedOnComplete = WrapAnimationSetEntryOnComplete(entry, () =>
			{
				if (generation != _activeSequenceGeneration) return;

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

			if (entry.link != null && entry.link.useLinkHold && !isTerminal)
			{
				var events = state.Events(this);
				events.Add(entry.link.holdStartNormalizedTime, () =>
				{
					if (generation != _activeSequenceGeneration) return;

					var prevSpeed = state.Speed;
					var prevEffectiveSpeed = state.EffectiveSpeed;
					state.Speed = 0f;

					float absSpeed = Mathf.Abs(prevEffectiveSpeed);
					float scaledDelay = absSpeed > 0.0001f ? (entry.link.holdDurationSeconds / absSpeed) : entry.link.holdDurationSeconds;

					StartCoroutine(UnpauseSequenceStateAfterDelay(state, prevSpeed, scaledDelay, wrappedOnComplete, generation));
				});
			}
			else
			{
				state.Events(this).OnEnd = wrappedOnComplete;
			}
		}

		private IEnumerator UnpauseSequenceStateAfterDelay(PlayableState state, float prevSpeed, float delay, Action onComplete, int generation)
		{
			yield return new WaitForSeconds(delay);
			if (generation == _activeSequenceGeneration && state != null && state.IsValid)
			{
				state.Speed = prevSpeed;
				onComplete?.Invoke();
			}
		}

		private PlayableState PlayFromPlayableAnimationSetEntry(AnimationSet set, AnimationSetEntry entry, Action onComplete, float startNormalizedTime = 0f)
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
			var wrappedOnComplete = WrapAnimationSetEntryOnComplete(entry, onComplete);

			var layerIndex = ResolveLayerIndex(entry);
			var layer = animancer.Layers[layerIndex];
			layer.Mask = ResolveAvatarMask(entry);

			// A previous TransitionBackFromLayer fades overlay-layer weight to 0; restore it or
			// this play is invisible. Locomotion (layer 0) is pinned at weight 1 by design.
			if (layerIndex != AnimLayer.Locomotion)
				layer.Weight = 1f;

			var state = layer.Play(entry.clip);
			if (startNormalizedTime > 0f)
			{
				state.NormalizedTime = startNormalizedTime;
			}
			else if (!entry.clip.IsLooping)
			{
				state.NormalizedTime = 0f;
			}

			if (entry.clip.IsLooping)
			{
				if (wrappedOnComplete != null)
				{
					state.Events(this).OnEnd = wrappedOnComplete;
				}
			}
			else
			{
				state.Events(this).OnEnd = () =>
				{
					if (entry.transitionBack)
					{
						TransitionBackFromLayer(layerIndex, entry.clip.FadeDuration);
					}
					wrappedOnComplete?.Invoke();
				};
			}

			return state;
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
			AnimationSetEntry entry = FindEntryById(set, entryId);
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
			AssertReady();
			CancelActiveSetSequence();
			AnimationSet set = FindAnimationSetByName(setName);
			if (set == null) throw new InvalidOperationException($"AnimatorBridgeBase: Animation set '{setName}' not found.");
			AnimationSetEntry entry = FindEntryById(set, entryId);
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
			AssertReady();
			CancelActiveSetSequence();
			AnimationSet set = FindAnimationSetByName(setName); if (set == null)
				throw new InvalidOperationException($"AnimatorBridgeBase: Animation set '{setName}' not found.");
			AnimationSetEntry entry = FindEntryById(set, entryId); if (entry == null) throw new InvalidOperationException($"AnimatorBridgeBase: Entry '{entryId}' not found in set '{setName}'.");
			if (entry.clip == null) throw new InvalidOperationException($"AnimatorBridgeBase: Entry '{entryId}' in set '{setName}' has no clip data.");
			if (entry.clip.Clip == null) throw new InvalidOperationException($"AnimatorBridgeBase: Entry '{entryId}' in set '{setName}' has no clip assigned.");
			return PlayFromPlayableAnimationSetEntry(set, entry, onComplete, Mathf.Clamp01(startNormalizedTime));
		}

		/// <summary>
		/// Strict variant that tries fallback set when primary has no playable entry.
		/// </summary>
		public PlayableState PlayFromSetWithFallbackStrict(string primarySetName, string fallbackSetName, string entryId, Action onComplete)
		{
			AssertReady();
			AnimationSet primary = FindAnimationSetByName(primarySetName);
			if (primary == null) throw new InvalidOperationException($"AnimatorBridgeBase: Animation set '{primarySetName}' not found.");
			AnimationSetEntry entry = FindEntryById(primary, entryId);
			if (IsPlayableAnimationSetEntry(entry))
			{
				return PlayFromPlayableAnimationSetEntry(primary, entry, onComplete);
			}

			AnimationSet fallback = FindAnimationSetByName(fallbackSetName);
			if (fallback == null) throw new InvalidOperationException($"AnimatorBridgeBase: Fallback animation set '{fallbackSetName}' not found.");
			entry = FindEntryById(fallback, entryId);
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

		public virtual IEnumerator CrossfadeAsync(AnimationClip clip, AnimationClipInfo clipInfo, AnimationMask mask, ActionData[] actions = null,
		                                          int layerIndex = -1, bool transitionBack = true)
		{
			AssertReady();
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

			state.Events(this).OnEnd = () =>
			{
				if (transitionBack)
				{
					TransitionBackFromLayer(resolvedLayer, transitionDuration);
				}
			};

			yield return state;
		}

		public virtual void PlayLoopingAnimation(AnimationClip clip, AnimationMask mask, bool isActAsAnimatorOutput = false, float transitionIn = 0.1f)
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
			if (layerIndex != AnimLayer.Locomotion)
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

			var idx = (int)mask;
			if (_serializedMasks == null || idx >= _serializedMasks.Length || _serializedMasks[idx] == null)
				throw new InvalidOperationException(
					$"{nameof(AnimatorBridgeBase)} on '{name}': Avatar mask slot for {mask} is not assigned in the inspector.");

			return _serializedMasks[idx];
		}

		#endregion
	}
}