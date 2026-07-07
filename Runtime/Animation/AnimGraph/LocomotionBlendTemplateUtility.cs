#if UNITY_EDITOR

namespace FoundationPlatform.Animation
{
	internal static class LocomotionBlendTemplateUtility
	{
		internal static readonly string[] DefaultDirectionSuffixes =
		{
			"Idle",
			"Fwd",
			"Back",
			"Left",
			"Right",
		};

		internal static LocomotionClipCategoryValidation[] CreateDefaultRequiredCategories()
		{
			return new[]
			{
				Category("Jump",          "Jump_Start",     expectLooping: false),
				Category("JumpApex",      "Jump_Apex",      expectLooping: false),
				Category("Land",          "Land",           expectLooping: false),
				Category("InAir",         "InAir",          expectLooping: true),
				Category("StandToCrouch", "Stand_To_Crouch", expectLooping: false),
				Category("CrouchToStand", "Crouch_To_Stand", expectLooping: false),
				Category("RunJump",       "Run_Jump",       expectLooping: false),
			};
		}

		internal static LocomotionBlendStanceDefinition CreateWalkStance()
		{
			return CreateStance(
				"walk",
				new[] { "Idle", "Walk_Fwd", "Walk_Back", "Walk_Left", "Walk_Right" },
				"Walk_Turn_Left_180",
				"Walk_Turn_Right_180",
				string.Empty);
		}

		internal static LocomotionBlendStanceDefinition CreateRunStance()
		{
			return CreateStance(
				"run",
				new[] { "Run_Idle", "Run_Fwd", "Run_Back", "Run_Left", "Run_Right" },
				"Turn_Left_180",
				"Turn_Right_180",
				"Run_Jump",
				"Run_Jump_Apex");
		}

		internal static LocomotionBlendStanceDefinition CreateCrouchStance()
		{
			return CreateStance(
				"crouch",
				new[] { "Crouch_Idle", "Crouch_Move_Fwd", "Crouch_Move_Back", "Crouch_Move_Left", "Crouch_Move_Right" },
				"Crouch_Turn_Left_180",
				"Crouch_Turn_Right_180",
				string.Empty);
		}

		internal static LocomotionBlendStanceDefinition CreateStance(
			string stanceId,
			string[] directionEntryIds,
			string turnLeft180Id,
			string turnRight180Id,
			string jumpClipId)
		{
			return CreateStance(stanceId, directionEntryIds, turnLeft180Id, turnRight180Id, jumpClipId, string.Empty);
		}

		internal static LocomotionBlendStanceDefinition CreateStance(
			string stanceId,
			string[] directionEntryIds,
			string turnLeft180Id,
			string turnRight180Id,
			string jumpClipId,
			string jumpApexClipId)
		{
			return new LocomotionBlendStanceDefinition
			{
				stanceId          = stanceId,
				jumpClipId        = jumpClipId,
				jumpApexClipId    = jumpApexClipId,
				turnLeft180Id     = turnLeft180Id,
				turnRight180Id    = turnRight180Id,
				directionEntryIds = (string[])directionEntryIds.Clone(),
			};
		}

		internal static LocomotionBlendStanceDefinition CreateStanceFromId(string stanceId)
		{
			var directionEntryIds = new string[LocomotionBlendStanceDefinition.CardinalDirectionSlotCount];
			for (var i = 0; i < DefaultDirectionSuffixes.Length; i++)
				directionEntryIds[i] = $"{stanceId}_{DefaultDirectionSuffixes[i]}";

			return CreateStance(
				stanceId,
				directionEntryIds,
				$"{stanceId}_Turn_Left_180",
				$"{stanceId}_Turn_Right_180",
				string.Empty);
		}

		private static LocomotionClipCategoryValidation Category(string category, string entryId, bool expectLooping)
		{
			return new LocomotionClipCategoryValidation
			{
				category      = category,
				entryId       = entryId,
				expectLooping = expectLooping,
			};
		}
	}
}
#endif
