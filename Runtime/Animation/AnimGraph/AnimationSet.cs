using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoundationPlatform.Animation
{
	[CreateAssetMenu(fileName = "AnimationSet", menuName = "Foundation/Animation/Animation Set", order = 45)]
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

			var visited = new HashSet<AnimationSet>();
			visited.Add(this);
			var current = parentSet;
			while (current != null)
			{
				if (!visited.Add(current))
				{
					break;
				}
				if (current.entries != null)
				{
					for (int i = 0; i < current.entries.Length; i++)
					{
						var e = current.entries[i];
						if (e != null && e.id == entryId)
							return e;
					}
				}
				current = current.parentSet;
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

		public LocomotionBlendProfile ResolvedBlendProfile
		{
			get
			{
				if (blendProfile != null) return blendProfile;
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
	public class AnimationSetEntry
	{
		[Tooltip("Unique key used with PlayFromSetStrict (and similar). Duplicate ids in one set produce editor warnings and ambiguous lookups.")]
		public string id;

		[Tooltip("Optional inspector grouping and codegen namespace for this entry.")]
		public string category;

		[Tooltip("Transition containing the clip and fade duration.")]
		public ClipTransitionData clip;

		[Tooltip("After a one-shot: when true, the overlay is released back to the layer base (default). When false, the overlay holds until replaced or released.")]
		public bool transitionBack = true;

		[Tooltip("Direct AvatarMask asset reference. Overrides 'mask' enum when assigned.")]
		public AvatarMask maskAsset;

		[Tooltip("Which body layers this clip affects (AnimationMask). FullBody applies no mask (default full-body behavior). Used only when maskAsset is not assigned.")]
		public AnimationMask mask = AnimationMask.FullBody;

		[Tooltip("Explicit Animancer layer index for this clip (-1 = auto: looping→1, one-shot→2). Set explicitly for channeled actions, stun loops, concurrent overlays, etc.")]
		public int layerIndex = -1;

		[Tooltip("Which axes of animator root motion drive the KCC motor for this clip.")]
		public RootMotionMode rootMotionMode = RootMotionMode.None;

		[Tooltip("While this clip plays: block KCC translation from move input. Input-driven rotation still applies. Does not replace rootMotionMode position drive.")]
		public bool suspendTranslation;

		[Tooltip("Optional chain to the next entry when using PlayFromSetSequence. Only the terminal step may be looping. Standalone PlayFromSet ignores link.")]
		public AnimationSetLink link = new();
	}
}
