using AetherNexus.FoundationPlatform.Logging;
using AetherNexus.FoundationPlatform.Logging.ConsoleView;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace AetherNexus.FoundationPlatform.Logging.ConsoleView.Editor
{
    /// <summary>Clears the console when a player build starts, if the per-project setting is enabled.</summary>
    internal sealed class ConsoleBuildClear : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (DebugXConsoleSettings.Instance.clearOnBuild)
                ConsoleLogStore.Clear();
        }
    }
}
