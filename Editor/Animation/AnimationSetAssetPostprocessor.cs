#if UNITY_EDITOR
using FoundationPlatform.Animation;
using UnityEditor;

namespace FoundationPlatform.Editor.Animation
{
	internal sealed class AnimationSetAssetPostprocessor : AssetPostprocessor
	{
		private static bool _scheduled;

		private static void OnPostprocessAllAssets(
			string[] importedAssets,
			string[] deletedAssets,
			string[] movedAssets,
			string[] movedFromAssetPaths)
		{
			var needsRebuild = false;
			for (var i = 0; i < importedAssets.Length; i++)
			{
				if (IsAnimationSetOrBlendProfilePath(importedAssets[i]))
				{
					needsRebuild = true;
					break;
				}
			}

			if (!needsRebuild)
				return;

			ScheduleRebuild();
		}

		private static bool IsAnimationSetOrBlendProfilePath(string assetPath)
		{
			if (string.IsNullOrEmpty(assetPath))
				return false;

			return assetPath.EndsWith(".asset")
			       && (AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(AnimationSet)
			           || AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(LocomotionBlendProfile));
		}

		private static void ScheduleRebuild()
		{
			if (_scheduled)
				return;

			_scheduled = true;
			EditorApplication.delayCall += RunScheduledRebuild;
		}

		private static void RunScheduledRebuild()
		{
			_scheduled = false;
			if (EditorApplication.isCompiling || EditorApplication.isUpdating)
			{
				ScheduleRebuild();
				return;
			}

			AnimationSetCodeGenerator.RebuildAllAnimationSets();
		}
	}
}
#endif
