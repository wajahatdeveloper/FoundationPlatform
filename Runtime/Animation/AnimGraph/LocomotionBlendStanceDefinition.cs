using System;
using AetherNexus.FoundationPlatform.AetherInspector;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Animation
{
	[Serializable]
	public class LocomotionTurnDefinition
	{
		[HorizontalGroup("Turn", LabelWidth = 100)]
		[Tooltip("Minimum absolute angle threshold to trigger this turn (e.g. 45, 90, 135, 180).")]
		public float angleThreshold = 100f;

		[HorizontalGroup("Turn", LabelWidth = 50)]
		[Tooltip("Entry ID for left turn at this angle.")]
		public string turnLeftId;

		[HorizontalGroup("Turn", LabelWidth = 50)]
		[Tooltip("Entry ID for right turn at this angle.")]
		public string turnRightId;
	}

	[Serializable]
	public class LocomotionBlendStanceDefinition
	{
		public const int CardinalDirectionSlotCount = 5;
		public const int DiagonalDirectionSlotCount = 4;
		public const int MaxDirectionSlotCount = CardinalDirectionSlotCount + DiagonalDirectionSlotCount;

		[TitleGroup("Stance Setup")]
		[Tooltip("Stable stance id used by abilities and SetLocomotionStance (e.g. walk, run, crouch).")]
		public string stanceId;

		[TitleGroup("Directional Entries")]
		[Tooltip("Cardinal entry ids: idle (0), forward (1), back (2), left (3), right (4).")]
		[ListDrawerSettings(IsReadOnly = true, CustomAddFunction = "InitDirectionArray")]
		public string[] directionEntryIds = new string[CardinalDirectionSlotCount];

		[TitleGroup("Directional Entries")]
		[Tooltip("When enabled, adds diagonal entries (FwdLeft, FwdRight, BackLeft, BackRight) for 8-way blending.")]
		public bool enableDiagonalDirections;

		[ShowIf("enableDiagonalDirections")]
		[TitleGroup("Directional Entries")]
		[Tooltip("Diagonal entry IDs: FwdLeft (5), FwdRight (6), BackLeft (7), BackRight (8).")]
		[ListDrawerSettings(IsReadOnly = true)]
		public string[] diagonalDirectionEntryIds = new string[DiagonalDirectionSlotCount];

		[TitleGroup("Stride Matching")]
		[Tooltip("Ground speed (m/s) each direction clip covers at playback 1 (entry Speed included), in direction slot order: idle (0, unused), forward, back, left, right, then the diagonals when enabled. Moving clips play at body speed / the blend-weighted speed of the directions in use, so feet don't slip when strafing or backing off either. Use the profile's Measure Natural Speeds button; enter by hand for in-place clips.")]
		[ListDrawerSettings(ShowIndexLabels = true)]
		public float[] directionNaturalSpeeds = new float[CardinalDirectionSlotCount];

		[TitleGroup("Stride Matching")]
		[Min(0.01f)]
		[Tooltip("Slowest playback rate stride matching may use. Below it the feet slide rather than the cycle crawling.")]
		public float minPlaybackRate = 0.6f;

		[TitleGroup("Stride Matching")]
		[Min(0.01f)]
		[Tooltip("Fastest playback rate stride matching may use.")]
		public float maxPlaybackRate = 1.6f;

		[TitleGroup("Turn In Place")]
		[Tooltip("Enable turn-in-place for this stance.")]
		public bool enableTurnInPlace = true;

		[TitleGroup("Turn In Place")]
		[ShowIf("enableTurnInPlace")]
		[Tooltip("Default turn angle threshold for 180° turns. Only used if customTurns is empty.")]
		public float turnAngleThreshold180 = 100f;

		[TitleGroup("Turn In Place")]
		[ShowIf("enableTurnInPlace")]
		[Tooltip("Cooldown between turn-in-place plays.")]
		public float turnCooldown = 0.35f;

		[TitleGroup("Turn In Place")]
		[ShowIf("enableTurnInPlace")]
		[Tooltip("180° turn one-shot entry ids for this stance.")]
		public string turnLeft180Id;

		[TitleGroup("Turn In Place")]
		[ShowIf("enableTurnInPlace")]
		public string turnRight180Id;

		[TitleGroup("Turn In Place")]
		[ShowIf("enableTurnInPlace")]
		[Tooltip("Optional multi-angle turn configurations. Ordered by angle threshold ascending.")]
		public LocomotionTurnDefinition[] customTurns = Array.Empty<LocomotionTurnDefinition>();

		[TitleGroup("Jumping")]
		[Tooltip("Jump clip id when leaving ground in this stance.")]
		public string jumpClipId;

		[TitleGroup("Jumping")]
		[Tooltip("Jump apex clip id after jump start in this stance.")]
		public string jumpApexClipId;

		[TitleGroup("Blend Parameter Overrides")]
		[Tooltip("When enabled, blendParams below override the profile defaults for this stance.")]
		public bool overrideBlendParams;

		[TitleGroup("Blend Parameter Overrides")]
		[ShowIf("overrideBlendParams")]
		public LocomotionBlendParams blendParams = LocomotionBlendParams.Default;

		private void InitDirectionArray()
		{
			directionEntryIds = new string[CardinalDirectionSlotCount];
			diagonalDirectionEntryIds = new string[DiagonalDirectionSlotCount];
		}

		public int GetDirectionSlotCount()
		{
			return enableDiagonalDirections ? MaxDirectionSlotCount : CardinalDirectionSlotCount;
		}

		public bool HasExplicitDirectionEntryIds()
		{
			if (directionEntryIds == null || directionEntryIds.Length != CardinalDirectionSlotCount)
				return false;

			for (var i = 0; i < CardinalDirectionSlotCount; i++)
			{
				if (string.IsNullOrWhiteSpace(directionEntryIds[i]))
					return false;
			}

			if (!enableDiagonalDirections)
				return true;

			if (diagonalDirectionEntryIds == null || diagonalDirectionEntryIds.Length != DiagonalDirectionSlotCount)
				return false;

			for (var i = 0; i < DiagonalDirectionSlotCount; i++)
			{
				if (string.IsNullOrWhiteSpace(diagonalDirectionEntryIds[i]))
					return false;
			}

			return true;
		}

		public bool HasExplicitTurnClipIds()
		{
			return !string.IsNullOrWhiteSpace(turnLeft180Id)
			       && !string.IsNullOrWhiteSpace(turnRight180Id);
		}

		public void ValidateOrThrow(string profileName)
		{
			if (string.IsNullOrWhiteSpace(stanceId))
				throw new InvalidOperationException(
					$"Locomotion blend profile '{profileName}': stance has empty stanceId.");

			if (directionEntryIds == null || directionEntryIds.Length != CardinalDirectionSlotCount)
				throw new InvalidOperationException(
					$"Locomotion blend profile '{profileName}' stance '{stanceId}': directionEntryIds must have {CardinalDirectionSlotCount} cardinal elements.");

			if (enableDiagonalDirections
			    && (diagonalDirectionEntryIds == null || diagonalDirectionEntryIds.Length != DiagonalDirectionSlotCount))
				throw new InvalidOperationException(
					$"Locomotion blend profile '{profileName}' stance '{stanceId}': diagonalDirectionEntryIds must have {DiagonalDirectionSlotCount} elements when diagonals are enabled.");

			var slotCount = GetDirectionSlotCount();
			if (directionNaturalSpeeds == null || directionNaturalSpeeds.Length != slotCount)
				throw new InvalidOperationException(
					$"Locomotion blend profile '{profileName}' stance '{stanceId}': directionNaturalSpeeds must have {slotCount} elements, one per direction slot.");

			for (var i = 1; i < slotCount; i++)
			{
				if (directionNaturalSpeeds[i] <= 0f)
					throw new InvalidOperationException(
						$"Locomotion blend profile '{profileName}' stance '{stanceId}': directionNaturalSpeeds[{i}] must be above 0 (m/s that direction clip covers at playback 1). Use Measure Natural Speeds or enter it by hand.");
			}

			if (minPlaybackRate > maxPlaybackRate)
				throw new InvalidOperationException(
					$"Locomotion blend profile '{profileName}' stance '{stanceId}': minPlaybackRate ({minPlaybackRate}) is above maxPlaybackRate ({maxPlaybackRate}).");
		}
	}
}
