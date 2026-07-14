#if UNITY_EDITOR
using UnityEditor;

namespace AetherNexus.FoundationPlatform.StaleComponentGuard.Editor
{
    /// <summary>
    /// Single on/off switch for the whole feature, persisted in <see cref="EditorPrefs"/> (per user,
    /// per machine). Read by the decorator, inspector badge, and panel section so one toggle silences
    /// every surface at once. Default on.
    /// </summary>
    public static class StaleComponentGuardSettings
    {
        private const string EnabledKey = "FoundationPlatform.StaleComponentGuard.Enabled";

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, true);
            set
            {
                if (value == Enabled)
                    return;
                EditorPrefs.SetBool(EnabledKey, value);
                StaleComponentCache.Invalidate();
                EditorApplication.RepaintHierarchyWindow();
            }
        }
    }
}
#endif
