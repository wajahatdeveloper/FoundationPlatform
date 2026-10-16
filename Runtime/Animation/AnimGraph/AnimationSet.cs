using System;
using System.Collections.Generic;
using UnityEngine;
using AetherNexus.FoundationPlatform.AetherInspector;
using AetherNexus.FoundationPlatform.Utilities.Menus;

namespace AetherNexus.FoundationPlatform.Animation
{
	[CreateAssetMenu(fileName = "AnimationSet", menuName = "Animation/Animation Set", order = 45)]
	[DesignerFeature(
		"Animation Set",
		"Names the clips a character can play so gameplay asks for \"attack\" rather than a specific clip, letting each character swap its own animations in.",
		"animation set clip motion character attack idle walk run swap variant animator library",
		DesignerFeatureKind.Asset,
		"")]
	[Icon("Packages/com.aethernexus.foundationplatform/Editor/Icons/AnimationSet.png")]
	[DesignerIcon(DesignerSymbol.Animation)]
	[AetherNexus.FoundationPlatform.Attributes.ContentHome("*/**/AnimationSets/**", "*/**/Items/**")]
	public class AnimationSet : ScriptableObject
	{
		[Tooltip("Optional parent animation set to inherit entries from.")]
		public AnimationSet parentSet;

		[Tooltip("Locomotion blend layout asset. Required for locomotion sets; leave null for combat/equipment sets.")]
		public LocomotionBlendProfile blendProfile;

		[Tooltip("Optional validation rules for non-locomotion sets (combat, equipment, etc.).")]
		public AnimationSetValidationProfile validationProfile;

		[Tooltip("All clips in this asset. Each entry's id is what code passes to PlayFromSetStrict to play that row.")]
		[InspectorName("Entries (Assets)")]
		public AnimationSetEntry[] entries = Array.Empty<AnimationSetEntry>();

		public AnimationSetEntry FindEntry(string entryId)
		{
			if (string.IsNullOrEmpty(entryId)) return null;

			if (entries != null)
			{
				for (int i = 0; i < entries.Length; i++)
				{
					var e = entries[i];
					if (e != null && e.id == entryId)
						return e;
				}
			}

			// Called on every play request: walk a bounded depth allocation-free. Revisiting a cycle only re-checks
			// sets that already missed, so the result matches the visited-set walk below.
			var shallow = parentSet;
			for (int depth = 0; depth < AllocationFreeResolveDepth; depth++)
			{
				if (shallow == null) return null;
				var found = FindOwnEntry(shallow, entryId);
				if (found != null) return found;
				shallow = shallow.parentSet;
			}

			var visited = new HashSet<AnimationSet>();
			visited.Add(this);
			var current = parentSet;
			while (current != null)
			{
				if (!visited.Add(current))
				{
					break;
				}
				var found = FindOwnEntry(current, entryId);
				if (found != null) return found;
				current = current.parentSet;
			}

			return null;
		}

		private static AnimationSetEntry FindOwnEntry(AnimationSet set, string entryId)
		{
			if (set.entries == null) return null;
			for (int i = 0; i < set.entries.Length; i++)
			{
				var e = set.entries[i];
				if (e != null && e.id == entryId)
					return e;
			}
			return null;
		}

		public Dictionary<string, AnimationSetEntry> GetResolvedEntries()
		{
			var byId = new Dictionary<string, AnimationSetEntry>(StringComparer.Ordinal);
			var hierarchy = new List<AnimationSet>();
			var visited = new HashSet<AnimationSet>();
			var current = this;
			while (current != null)
			{
				if (!visited.Add(current)) break;
				hierarchy.Add(current);
				current = current.parentSet;
			}

			for (int i = hierarchy.Count - 1; i >= 0; i--)
			{
				var s = hierarchy[i];
				if (s.entries == null) continue;
				for (int j = 0; j < s.entries.Length; j++)
				{
					var e = s.entries[j];
					if (e == null || string.IsNullOrEmpty(e.id)) continue;
					byId[e.id] = e;
				}
			}
			return byId;
		}

		private const int AllocationFreeResolveDepth = 16;

		public LocomotionBlendProfile ResolvedBlendProfile
		{
			get
			{
				if (blendProfile != null) return blendProfile;

				// Read per sim tick: walk a bounded depth allocation-free. Revisiting a cycle only re-checks sets
				// whose profile is already null, so the result matches the visited-set walk below.
				var shallow = parentSet;
				for (int depth = 0; depth < AllocationFreeResolveDepth; depth++)
				{
					if (shallow == null) return null;
					if (shallow.blendProfile != null) return shallow.blendProfile;
					shallow = shallow.parentSet;
				}

				var visited = new HashSet<AnimationSet>();
				visited.Add(this);
				var current = parentSet;
				while (current != null)
				{
					if (!visited.Add(current)) break;
					if (current.blendProfile != null) return current.blendProfile;
					current = current.parentSet;
				}
				return null;
			}
		}

		public AnimationSetValidationProfile ResolvedValidationProfile
		{
			get
			{
				if (validationProfile != null) return validationProfile;
				var visited = new HashSet<AnimationSet>();
				visited.Add(this);
				var current = parentSet;
				while (current != null)
				{
					if (!visited.Add(current)) break;
					if (current.validationProfile != null) return current.validationProfile;
					current = current.parentSet;
				}
				return null;
			}
		}

		private void OnValidate()
		{
			if (entries == null) return;

			var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
			for (var i = 0; i < entries.Length; i++)
			{
				var e = entries[i];
				if (e == null) continue;
				if (string.IsNullOrEmpty(e.id)) continue;
				if (!seen.Add(e.id))
					Debug.LogWarning($"[AnimationSet] '{name}': duplicate entry id '{e.id}'. Generated constants and runtime lookups will be ambiguous.", this);
			}

			var resolvedVal = ResolvedValidationProfile;
			if (resolvedVal != null)
			{
				var warnings = resolvedVal.Validate(this);
				for (var i = 0; i < warnings.Count; i++)
					Debug.LogWarning($"[AnimationSet] '{name}': {warnings[i]}", this);
			}
		}
	}
	public enum RootMotionMode
	{
		None,
		RotationOnly,
		PositionOnly,
		PositionAndRotation
	}

	[Serializable]
	public class AnimationTagWindow
	{
		public const int MaxPerEntry = 64;

		[Tooltip("Gameplay tag held on the character while the clip's gameplay timeline is inside the window (e.g. State.Combat.Attacking). Overlapping holds of the same tag stack, so a crossfade between two entries carrying it never drops it.")]
		public string tagName;

		[Tooltip("Normalized clip range (0 = start, 1 = end) the tag is held for. Checked against where the timeline is now, not whether it crossed the start, so a mid-window start still holds it. Looping clips hold it once per cycle.")]
		[MinMaxSlider(0f, 1f, true)]
		public Vector2 normalizedWindow = new Vector2(0f, 1f);
	}

	[Serializable]
	public class AnimationSourceStartOffset
	{
		[Tooltip("Entry id that must be playing on the same layer when this entry starts for the offset to apply (e.g. Fall_Short into Land).")]
		public string sourceEntryId;

		[Tooltip("Normalized time this entry starts from when it follows the source entry. Replaces Start Normalized Time for that transition only.")]
		[PropertyRange(0f, 1f)]
		public float startNormalizedTime;
	}

	[Serializable]
	public class AnimationEntryCurve
	{
		[Tooltip("Name code queries with TryGetTimelineCurveValue. Unique per entry; compared trimmed and case-insensitive.")]
		public string curveName;

		[Tooltip("Value over the clip. X = normalized clip time (0..1), Y = value. Sampled on the gameplay timeline only when queried.")]
		public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
	}

	[Serializable]
	public class AnimationSetEntry
	{
		[Tooltip("Unique key used with PlayFromSetStrict (and similar). Duplicate ids in one set produce editor warnings and ambiguous lookups.")]
		public string id;

		[Tooltip("Optional inspector grouping and codegen namespace for this entry.")]
		public string category;

		[Tooltip("Transition containing the clip and fade duration.")]
		public ClipTransitionData clip;

		[Tooltip("Normalized time playback starts from (0 = clip start). Raise it to skip an authored lead-in, " +
		         "e.g. a death clip that stands still for a moment before it collapses. Ignored by sequence playback " +
		         "and by debug scrub, which pass their own start time.")]
		[PropertyRange(0f, 1f)]
		public float startNormalizedTime;

		[Tooltip("Per-predecessor start time: when the listed entry is playing on the same layer as this one starts, playback begins at its offset instead of Start Normalized Time (e.g. match the foot phase coming out of a turn). Ignored by sequence playback and debug scrub.")]
		public AnimationSourceStartOffset[] sourceStartOffsets = Array.Empty<AnimationSourceStartOffset>();

		[Tooltip("Gameplay tags held on the character over a normalized part of this clip, driven by the simulation-clock gameplay timeline (e.g. a cancel window or hitbox phase).")]
		public AnimationTagWindow[] tagWindows = Array.Empty<AnimationTagWindow>();

		[Tooltip("Named float curves over this clip, read on the gameplay timeline with TryGetTimelineCurveValue (e.g. damage scale, IK weight).")]
		public AnimationEntryCurve[] curves = Array.Empty<AnimationEntryCurve>();

		[Tooltip("After a one-shot: when true, the overlay is released back to the layer base (default). When false, the overlay holds until replaced or released.")]
		public bool transitionBack = true;

		[ShowIf(nameof(transitionBack))]
		[Tooltip("Seconds to blend back to the layer base when this one-shot ends (0 = reuse the clip's Fade Duration). Lets a reaction snap in fast and settle out softly.")]
		[Min(0f)]
		public float transitionBackFadeDuration;

		[Tooltip("Direct AvatarMask asset reference. Overrides 'mask' enum when assigned.")]
		public AvatarMask maskAsset;

		[Tooltip("Which body layers this clip affects (AnimationMask). FullBody applies no mask (default full-body behavior). Used only when maskAsset is not assigned.")]
		public AnimationMask mask = AnimationMask.FullBody;

		[Tooltip("Explicit Animancer layer index for this clip (-1 = auto: looping→1, one-shot→2). Set explicitly for channeled actions, stun loops, concurrent overlays, etc.")]
		public int layerIndex = -1;

		[Tooltip("Which axes of root motion drive the KCC motor for this clip. Any mode other than None needs a baked trajectory below.")]
		public RootMotionMode rootMotionMode = RootMotionMode.None;

		[HideIf(nameof(rootMotionMode), RootMotionMode.None)]
		[Tooltip("Root trajectory sampled in the Editor and read by simulation instead of the animation graph, so root motion is identical on every peer. Re-bake with Tools > Rebuild > Animation > Bake Root Motion, or the Apply fix on the set's validation finding.")]
		public AnimationRootMotionBake rootMotionBake = new();

		[ShowIf(nameof(DrivesRootMotionPosition))]
		[Tooltip("Normalized clip range where root motion moves the character. Outside it the clip still plays but translation stays put, e.g. only the push of an attack travels. One-shot clips only.")]
		[MinMaxSlider(0f, 1f, true)]
		public Vector2 rootMotionPositionWindow = new Vector2(0f, 1f);

		[ShowIf(nameof(DrivesRootMotionRotation))]
		[Tooltip("Normalized clip range where root motion turns the character, e.g. release rotation early on a 180 start for responsiveness. One-shot clips only.")]
		[MinMaxSlider(0f, 1f, true)]
		public Vector2 rootMotionRotationWindow = new Vector2(0f, 1f);

		[Tooltip("While this clip plays: block KCC translation from move input. Input-driven rotation still applies. Does not replace rootMotionMode position drive.")]
		public bool suspendTranslation;

		[Tooltip("Optional chain to the next entry when using PlayFromSetSequence. Only the terminal step may be looping. Standalone PlayFromSet ignores link.")]
		public AnimationSetLink link = new();

		public bool DrivesRootMotionPosition =>
			rootMotionMode == RootMotionMode.PositionOnly || rootMotionMode == RootMotionMode.PositionAndRotation;

		public bool DrivesRootMotionRotation =>
			rootMotionMode == RootMotionMode.RotationOnly || rootMotionMode == RootMotionMode.PositionAndRotation;

		public float TransitionBackFade => transitionBackFadeDuration > 0f ? transitionBackFadeDuration : clip.FadeDuration;

		public bool HasTagWindows => tagWindows != null && tagWindows.Length > 0;

		public bool HasCurves => curves != null && curves.Length > 0;
	}
}
