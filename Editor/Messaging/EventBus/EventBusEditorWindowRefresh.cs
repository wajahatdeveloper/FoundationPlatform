#if UNITY_EDITOR
using UnityEditor;

namespace FoundationPlatform.Editor.Utilities.Messaging
{
    internal static class EventBusEditorWindowRefresh
    {
        public static bool ShouldPoll(EditorWindow window, bool autoRefreshEnabled)
        {
            if (!autoRefreshEnabled || window == null)
                return false;
            return EditorWindow.focusedWindow == window;
        }
    }
}
#endif
