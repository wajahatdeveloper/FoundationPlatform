using System;
using System.Collections.Generic;

namespace FoundationPlatform.Animation
{
	public static class AnimationSetSequenceUtility
	{
		public static IReadOnlyList<string> CollectSequenceEntryIds(AnimationSet set, string firstEntryId)
		{
			if (set == null)
			{
				throw new ArgumentNullException(nameof(set));
			}

			if (string.IsNullOrWhiteSpace(firstEntryId))
			{
				throw new ArgumentException("firstEntryId must be non-empty.", nameof(firstEntryId));
			}

			var result = new List<string>();
			var visited = new HashSet<string>(StringComparer.Ordinal);
			var currentId = firstEntryId;

			while (!string.IsNullOrWhiteSpace(currentId))
			{
				if (!visited.Add(currentId))
				{
					throw new InvalidOperationException(
						$"AnimationSet '{set.name}': sequence cycle detected at entry '{currentId}'.");
				}

				result.Add(currentId);
				var entry = FindEntryById(set, currentId);
				if (entry == null)
				{
					throw new InvalidOperationException(
						$"AnimationSet '{set.name}': sequence references missing entry '{currentId}'.");
				}

				if (entry.link == null || !entry.link.HasNext)
				{
					break;
				}

				currentId = entry.link.nextEntryId;
			}

			if (result.Count == 0)
			{
				throw new InvalidOperationException(
					$"AnimationSet '{set.name}': sequence from '{firstEntryId}' produced no entries.");
			}

			return result;
		}

		public static float ResolveTransitionInForLink(AnimationSetEntry sourceEntry, AnimationSetEntry targetEntry)
		{
			if (sourceEntry.link != null && sourceEntry.link.transitionIn > 0.00001f)
			{
				return sourceEntry.link.transitionIn;
			}

			if (targetEntry.clip == null)
			{
				throw new InvalidOperationException(
					$"AnimationSet entry '{targetEntry.id}' has no clip data for transition-in fallback.");
			}

			return targetEntry.clip.FadeDuration;
		}

		public static float ResolveTransitionOutForLink(AnimationSetEntry sourceEntry)
		{
			if (sourceEntry.link != null && sourceEntry.link.transitionOut > 0.00001f)
			{
				return sourceEntry.link.transitionOut;
			}

			if (sourceEntry.clip == null)
			{
				throw new InvalidOperationException(
					$"AnimationSet entry '{sourceEntry.id}' has no clip data for transition-out fallback.");
			}

			return sourceEntry.clip.FadeDuration;
		}

		public static void ValidateSequenceEntryForPlayback(AnimationSetEntry entry, string setName, bool isTerminal)
		{
			if (entry == null)
			{
				throw new InvalidOperationException($"AnimationSet '{setName}': sequence entry is null.");
			}

			if (entry.clip == null || entry.clip.Clip == null)
			{
				throw new InvalidOperationException(
					$"AnimationSet '{setName}': entry '{entry.id}' has no clip assigned.");
			}

			if (entry.clip.IsLooping && !isTerminal)
			{
				throw new InvalidOperationException(
					$"AnimationSet '{setName}': entry '{entry.id}' is looping; only the terminal sequence step may loop.");
			}

			if (entry.rootMotionMode != RootMotionMode.None)
			{
				throw new InvalidOperationException(
					$"AnimationSet '{setName}': entry '{entry.id}' uses rootMotionMode={entry.rootMotionMode} inside a sequence, which is not supported. " +
					"Play the root-motion clip standalone via PlayLocomotionClip and chain to the next clip from its onComplete callback.");
			}
		}

		/// <summary>
		/// Whether the terminal step should release back to the blend layer after the sequence handoff completes.
		/// Penultimate link can disable use of the terminal entry's <see cref="AnimationSetEntry.transitionBack"/>.
		/// </summary>
		public static bool ResolveTerminalTransitionBack(AnimationSetEntry terminal, AnimationSetEntry penultimate)
		{
			if (terminal == null)
			{
				throw new ArgumentNullException(nameof(terminal));
			}

			if (penultimate != null
			    && penultimate.link != null
			    && penultimate.link.HasNext
			    && !penultimate.link.useEntryTransitionBackForTerminal)
			{
				return false;
			}

			return terminal.transitionBack;
		}

		public static AnimationSetLinkHoldPlayback ResolveLinkHoldForStep(AnimationSetEntry entry, bool isTerminal)
		{
			if (isTerminal || entry == null || entry.link == null || !entry.link.useLinkHold)
			{
				return AnimationSetLinkHoldPlayback.Inactive;
			}

			ValidateLinkHoldConfig(entry.link, entry.id);
			return new AnimationSetLinkHoldPlayback
			{
				IsActive = true,
				HoldStartNormalizedTime = entry.link.holdStartNormalizedTime,
				HoldDurationSeconds = entry.link.holdDurationSeconds,
				HoldMode = entry.link.holdMode
			};
		}

		public static void ValidateLinkHoldConfig(AnimationSetLink link, string entryId)
		{
			if (link == null || !link.useLinkHold)
			{
				return;
			}

			if (!link.HasNext)
			{
				throw new InvalidOperationException(
					$"AnimationSet entry '{entryId}': useLinkHold requires link.nextEntryId.");
			}

			if (link.holdDurationSeconds <= 0f)
			{
				throw new InvalidOperationException(
					$"AnimationSet entry '{entryId}': useLinkHold requires holdDurationSeconds > 0.");
			}

			if (link.holdStartNormalizedTime < 0f || link.holdStartNormalizedTime > 1f)
			{
				throw new InvalidOperationException(
					$"AnimationSet entry '{entryId}': holdStartNormalizedTime must be in [0, 1].");
			}
		}

		private static AnimationSetEntry FindEntryById(AnimationSet set, string entryId)
		{
			if (set == null) return null;
			return set.FindEntry(entryId);
		}
	}
}
