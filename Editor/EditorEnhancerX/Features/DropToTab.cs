using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorEnhancerX {
    /// <summary>
    /// Drop an object onto an editor window's tab strip to open it in a floating
    /// Property Editor. Fed by <see cref="GlobalKeyCapture"/> (requires the Tier-2
    /// hook), gated by the dropToTabEnabled setting, off by default.
    /// </summary>
    internal static class DropToTab {

        private const float TabStripHeight = 28f;

        internal static void Handle(Event e) {
            var window = EditorWindow.mouseOverWindow;
            if (window == null)
                return;

            // Event coordinates are local to the view under the cursor; convert to
            // screen space and test against the window's tab strip band.
            var screenMouse = GUIUtility.GUIToScreenPoint(e.mousePosition);
            var winPos = window.position;
            if (screenMouse.x < winPos.x || screenMouse.x > winPos.xMax)
                return;
            var relativeY = screenMouse.y - (winPos.y - TabStripHeight);
            if (relativeY < 0f || relativeY > TabStripHeight)
                return;

            var refs = DragAndDrop.objectReferences;
            if (refs == null || refs.Length != 1 || refs[0] == null)
                return;

            if (e.type == EventType.DragUpdated) {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                e.Use();
                return;
            }

            DragAndDrop.AcceptDrag();
            EditorUtility.OpenPropertyEditor(refs[0]);
            e.Use();
        }
    }
}
