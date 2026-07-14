using System;
using System.Collections.Generic;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Animation
{
	[Serializable]
	public struct LocomotionClipCategoryValidation
	{
		[Tooltip("Logical category name used for codegen (e.g. Jump, Land, InAir).")]
		public string category;

		[Tooltip("AnimationSet entry id for this category.")]
		public string entryId;

		[Tooltip("When true, entry should use the looping playable path.")]
		public bool expectLooping;
	}

	[CreateAssetMenu(fileName = "AnimationSetValidationProfile", menuName = "Foundation/Animation/Animation Set Validation Profile", order = 43)]
	public class AnimationSetValidationProfile : ScriptableObject
	{
		[Tooltip("Entry ids that must exist in the AnimationSet with assigned clips.")]
		public string[] requiredEntryIds = Array.Empty<string>();

		[Tooltip("Locomotion clip categories validated at edit time and emitted as codegen constants.")]
		public LocomotionClipCategoryValidation[] requiredCategoryEntries = Array.Empty<LocomotionClipCategoryValidation>();

		[Tooltip("Entry ids that must have isLooping enabled.")]
		public string[] requiredLoopingEntryIds = Array.Empty<string>();

		[Tooltip("Entry ids that must be one-shots (isLooping disabled).")]
		public string[] requiredOneShotEntryIds = Array.Empty<string>();

		[Tooltip("Whether this AnimationSet requires a LocomotionBlendProfile assigned.")]
		public bool requireBlendProfile;

		public IReadOnlyList<string> Validate(AnimationSet set)
		{
			var messages = new List<string>();
			if (set == null) return messages;

			if (requireBlendProfile && set.ResolvedBlendProfile == null)
				messages.Add("Locomotion blend profile is required but none is assigned.");

			var byId = BuildMap(set);

			if (requiredEntryIds != null)
			{
				for (var i = 0; i < requiredEntryIds.Length; i++)
				{
					var id = requiredEntryIds[i];
					if (string.IsNullOrEmpty(id)) continue;
					if (!byId.TryGetValue(id, out var entry) || entry.clip == null || entry.clip.Clip == null)
						messages.Add($"Required entry '{id}' is missing or has no clip assigned.");
				}
			}

			if (requiredCategoryEntries != null)
			{
				var seenCategories = new HashSet<string>(StringComparer.Ordinal);
				for (var i = 0; i < requiredCategoryEntries.Length; i++)
				{
					var cat = requiredCategoryEntries[i];
					if (string.IsNullOrWhiteSpace(cat.category))
					{
						messages.Add($"Required category entry at index {i} has empty category.");
						continue;
					}

					if (!seenCategories.Add(cat.category))
						messages.Add($"Duplicate required category '{cat.category}'.");

					if (string.IsNullOrWhiteSpace(cat.entryId))
					{
						messages.Add($"Required category '{cat.category}' has empty entryId.");
						continue;
					}

					if (!byId.TryGetValue(cat.entryId, out var entry) || entry.clip == null || entry.clip.Clip == null)
						messages.Add($"Required category '{cat.category}' entry '{cat.entryId}' is missing or has no clip assigned.");
					else if (cat.expectLooping && !entry.clip.IsLooping)
						messages.Add($"Required category '{cat.category}' entry '{cat.entryId}' must have isLooping enabled.");
					else if (!cat.expectLooping && entry.clip.IsLooping)
						messages.Add($"Required category '{cat.category}' entry '{cat.entryId}' must be a one-shot (isLooping disabled).");
				}
			}

			if (requiredLoopingEntryIds != null)
			{
				for (var i = 0; i < requiredLoopingEntryIds.Length; i++)
				{
					var id = requiredLoopingEntryIds[i];
					if (string.IsNullOrEmpty(id)) continue;
					if (!byId.TryGetValue(id, out var entry))
						messages.Add($"Required looping entry '{id}' is missing.");
					else if (!entry.clip.IsLooping)
						messages.Add($"Entry '{id}' must have isLooping enabled.");
				}
			}

			if (requiredOneShotEntryIds != null)
			{
				for (var i = 0; i < requiredOneShotEntryIds.Length; i++)
				{
					var id = requiredOneShotEntryIds[i];
					if (string.IsNullOrEmpty(id)) continue;
					if (!byId.TryGetValue(id, out var entry))
						messages.Add($"Required one-shot entry '{id}' is missing.");
					else if (entry.clip.IsLooping)
						messages.Add($"Entry '{id}' must have isLooping disabled (one-shot).");
				}
			}

			return messages;
		}

		private static Dictionary<string, AnimationSetEntry> BuildMap(AnimationSet set)
		{
			if (set == null) return new Dictionary<string, AnimationSetEntry>(StringComparer.Ordinal);
			return set.GetResolvedEntries();
		}
	}
}
