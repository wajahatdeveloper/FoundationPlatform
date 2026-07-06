using System;
using System.Collections.Generic;
using FoundationPlatform.Animation;
using UnityEngine;

namespace FoundationPlatform.Editor.Animation
{
	public static class AnimationSetValidator
	{
		public static void LogValidation(AnimationSet set)
		{
			if (set == null)
			{
				Debug.LogWarning("[AnimationSet] No AnimationSet to validate.");
				return;
			}

			var byId = BuildEntryMap(set);
			ValidateDuplicateAndMissingClips(set, byId);
			ValidateValidationProfile(set, byId);

			LogLinkChainValidation(set, byId);

			Debug.Log($"[AnimationSet] Validation finished for '{set.name}' ({byId.Count} entries).", set);
		}

		public static void CollectLinkChainValidation(
			AnimationSet set,
			Dictionary<string, AnimationSetEntry> byId,
			List<string> warnings,
			List<string> errors)
		{
			if (set == null || set.entries == null || warnings == null || errors == null)
				return;

			var validatedRoots = new HashSet<string>(StringComparer.Ordinal);

			for (var i = 0; i < set.entries.Length; i++)
			{
				var entry = set.entries[i];
				if (entry == null || string.IsNullOrEmpty(entry.id))
					continue;

				if (entry.link == null || !entry.link.HasNext)
					continue;

				var nextId = entry.link.nextEntryId;
				if (!byId.ContainsKey(nextId))
				{
					errors.Add($"Entry '{entry.id}': link.nextEntryId '{nextId}' does not exist in this set.");
					continue;
				}

				if (entry.clip != null && entry.clip.IsLooping)
				{
					warnings.Add(
						$"Entry '{entry.id}': isLooping with a sequence link; only the terminal step may loop.");
				}

				if (entry.link.useLinkHold)
				{
					try
					{
						AnimationSetSequenceUtility.ValidateLinkHoldConfig(entry.link, entry.id);
					}
					catch (InvalidOperationException ex)
					{
						errors.Add(ex.Message);
					}
				}

				if (validatedRoots.Contains(entry.id))
					continue;

				try
				{
					var sequenceIds = AnimationSetSequenceUtility.CollectSequenceEntryIds(set, entry.id);
					validatedRoots.Add(entry.id);

					for (var stepIndex = 0; stepIndex < sequenceIds.Count; stepIndex++)
					{
						if (!byId.TryGetValue(sequenceIds[stepIndex], out var stepEntry))
							continue;

						var isTerminal = stepIndex == sequenceIds.Count - 1;
						if (stepEntry.clip != null && stepEntry.clip.IsLooping && !isTerminal)
						{
							warnings.Add(
								$"Entry '{stepEntry.id}': isLooping mid-sequence; only terminal '{sequenceIds[sequenceIds.Count - 1]}' may loop.");
						}

						if (stepEntry.rootMotionMode != RootMotionMode.None)
					{
						errors.Add(
							$"Entry '{stepEntry.id}': rootMotionMode={stepEntry.rootMotionMode} cannot be used inside a sequence. " +
							"Workaround: play the root-motion clip standalone via PlayLocomotionClip, then chain to the next " +
							"non-root-motion clip from the onComplete callback.");
					}

						if (isTerminal && stepEntry.clip != null && stepEntry.clip.IsLooping && stepEntry.transitionBack)
						{
							warnings.Add(
								$"Entry '{stepEntry.id}': terminal isLooping; transitionBack is ignored until another play or ReleaseToBlendLayer.");
						}

						if (isTerminal && stepEntry.link != null && stepEntry.link.useLinkHold)
						{
							warnings.Add(
								$"Entry '{stepEntry.id}': useLinkHold on terminal step is ignored (no next entry).");
						}
					}
				}
				catch (InvalidOperationException ex)
				{
					errors.Add(ex.Message);
				}
			}
		}

		private static void LogLinkChainValidation(AnimationSet set, Dictionary<string, AnimationSetEntry> byId)
		{
			var warnings = new List<string>();
			var errors   = new List<string>();
			CollectLinkChainValidation(set, byId, warnings, errors);

			for (var w = 0; w < warnings.Count; w++)
				Debug.LogWarning($"[AnimationSet] {warnings[w]}", set);
			for (var e = 0; e < errors.Count; e++)
				Debug.LogError($"[AnimationSet] {errors[e]}", set);
		}


		public static Dictionary<string, AnimationSetEntry> BuildEntryMap(AnimationSet set)
		{
			if (set == null) return new Dictionary<string, AnimationSetEntry>();
			return set.GetResolvedEntries();
		}

		private static void ValidateDuplicateAndMissingClips(AnimationSet set, Dictionary<string, AnimationSetEntry> byId)
		{
			foreach (var pair in byId)
			{
				if (pair.Value.clip?.Clip == null)
					Debug.LogError($"[AnimationSet] Entry '{pair.Key}' has no AnimationClip assigned in '{set.name}'.", set);
				else
					ValidateRootMotionCurves(set, pair.Key, pair.Value);
			}
		}

		private static void ValidateRootMotionCurves(AnimationSet set, string entryId, AnimationSetEntry entry)
		{
			if (entry.rootMotionMode == RootMotionMode.None)
				return;

			var clip = entry.clip.Clip;
			if (!clip.hasMotionCurves && !clip.hasRootCurves)
			{
				Debug.LogWarning(
					$"[AnimationSet] Entry '{entryId}' uses rootMotionMode={entry.rootMotionMode} but clip '{clip.name}' has no root motion curves in '{set.name}'.",
					set);
			}
		}
		private static void ValidateValidationProfile(AnimationSet set, Dictionary<string, AnimationSetEntry> byId)
		{
			var profile = set.ResolvedValidationProfile;
			if (profile == null)
				return;

			ValidateRequiredIds(set, byId, profile.requiredEntryIds, "[AnimationSet] Missing required entry id");
			ValidateLoopingExpectation(set, byId, profile.requiredLoopingEntryIds, expectLooping: true);
			ValidateLoopingExpectation(set, byId, profile.requiredOneShotEntryIds, expectLooping: false);
		}

		private static void ValidateRequiredIds(
			AnimationSet set,
			Dictionary<string, AnimationSetEntry> byId,
			string[] ids,
			string messagePrefix)
		{
			if (ids == null)
				return;

			for (var i = 0; i < ids.Length; i++)
			{
				var id = ids[i];
				if (string.IsNullOrWhiteSpace(id))
					continue;

				if (!byId.ContainsKey(id))
					Debug.LogError($"{messagePrefix} '{id}' in '{set.name}'.", set);
			}
		}

		private static void ValidateLoopingExpectation(
			AnimationSet set,
			Dictionary<string, AnimationSetEntry> byId,
			string[] ids,
			bool expectLooping)
		{
			if (ids == null)
				return;

			for (var i = 0; i < ids.Length; i++)
			{
				var id = ids[i];
				if (string.IsNullOrWhiteSpace(id))
					continue;

				if (!byId.TryGetValue(id, out var entry))
				{
					Debug.LogError($"[AnimationSet] Missing required entry id '{id}' in '{set.name}'.", set);
					continue;
				}

				if (entry.clip.IsLooping != expectLooping)
				{
					Debug.LogWarning(
						$"[AnimationSet] Entry '{id}' should have isLooping={(expectLooping ? 1 : 0)} in '{set.name}'.",
						set);
				}
			}
		}
	}
}
