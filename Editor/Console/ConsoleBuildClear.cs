using FoundationPlatform.DebugX;
using DebugXLogging.ConsoleView;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace DebugXLogging.ConsoleView.Editor
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
