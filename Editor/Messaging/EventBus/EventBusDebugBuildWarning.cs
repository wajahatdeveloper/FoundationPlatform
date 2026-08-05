using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Messaging
{
	/// <summary>
	/// Warns before a Development Build that includes EventBus reflection debug metadata.
	/// Never silent when the Project Setting is enabled.
	/// </summary>
	internal sealed class EventBusDebugBuildWarning : IPreprocessBuildWithReport
	{
		public int callbackOrder => 0;

		public void OnPreprocessBuild(BuildReport report)
		{
			var isDevelopment = (report.summary.options & BuildOptions.Development) != 0;
			if (!isDevelopment)
				return;

			if (!EventBusDebugSettings.Instance.includeReflectionInDevelopmentBuilds)
				return;

			var continueBuild = EditorUtility.DisplayDialog(
				"EventBus reflection in Development Build",
				"This Development Build will include EventBus reflection-based debug metadata " +
				$"(scripting define {EventBusDebugSettings.DefineSymbol}).\n\n" +
				"That path is opt-in via Project Settings → EventBus Debug. " +
				"Disable the setting if you do not want reflection in this player.",
				"Continue",
				"Cancel");

			if (!continueBuild)
			{
				throw new BuildFailedException(
					"Development Build cancelled: EventBus reflection debug metadata is enabled " +
					$"(Project Settings → EventBus Debug / {EventBusDebugSettings.DefineSymbol}).");
			}
		}
	}
}
