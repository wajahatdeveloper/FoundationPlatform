#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.Animation;
using AetherNexus.FoundationPlatform.Editor.AssetImport;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.Editor.Animation
{
	[InitializeOnLoad]
	internal static class AnimationSetImportPluginRegistration
	{
		static AnimationSetImportPluginRegistration()
		{
			AssetImportPluginRegistry.RegisterBatch(new AnimationSetImportPlugin());
		}
	}

	internal sealed class AnimationSetImportPlugin : IAssetImportBatchPlugin
	{
		private const string SchedulerKey = "animation-set-rebuild";

		public int Order => AssetImportPluginOrders.AnimationSet;

		public bool ShouldRun(
			string[] importedAssets,
			string[] deletedAssets,
			string[] movedAssets,
			string[] movedFromAssetPaths)
		{
			if (importedAssets == null || importedAssets.Length == 0)
				return false;

			for (int i = 0; i < importedAssets.Length; i++)
			{
				if (IsAnimationSetOrBlendProfilePath(importedAssets[i]))
					return true;
			}

			return false;
		}

		public void Run(
			string[] importedAssets,
			string[] deletedAssets,
			string[] movedAssets,
			string[] movedFromAssetPaths)
		{
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
			DeferredEditorScheduler.ScheduleOnce(SchedulerKey, RunScheduledRebuild);
		}

		private static void RunScheduledRebuild()
		{
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
