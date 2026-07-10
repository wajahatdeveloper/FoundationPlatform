using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EditorEnhancerX {
    /// <summary>
    /// Tier-2 key capture: prepends a handler to the internal
    /// EditorApplication.globalEventHandler so shortcuts (Maximize, Switch View)
    /// fire regardless of which editor window has focus. Opt-in via
    /// "Global Key Capture" in settings; self-disables when the internal field
    /// disappears on a Unity upgrade. Also feeds the drop-to-tab feature.
    /// </summary>
    [InitializeOnLoad]
    internal static class GlobalKeyCapture {

        internal static bool Available { get; private set; }

        static GlobalKeyCapture() {
            try {
                var field = typeof(EditorApplication).GetField("globalEventHandler",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (field == null || field.FieldType != typeof(EditorApplication.CallbackFunction))
                    return;

                var existing = (EditorApplication.CallbackFunction)field.GetValue(null);
                EditorApplication.CallbackFunction handler = OnGlobalEvent;
                field.SetValue(null, handler + existing);
                Available = true;
            } catch (Exception e) {
                Available = false;
                Debug.LogWarning("[EditorEnhancerX] Global key capture unavailable on this Unity version.\n" + e.Message);
            }
        }

        private static void OnGlobalEvent() {
            var s = EditorEnhancerXSettings.instance;
            if (!s.masterEnabled)
                return;

            var e = Event.current;
            if (e == null)
                return;

            if (s.globalCaptureEnabled && e.type == EventType.KeyDown)
                KeyRouter.Dispatch(e, KeyScope.Global);

            if (s.dropToTabEnabled && (e.type == EventType.DragUpdated || e.type == EventType.DragPerform))
                DropToTab.Handle(e);
        }
    }
}
