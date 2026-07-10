using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HierarchyX {
    /// <summary>
    /// Ctrl-dragging a single Component onto a hierarchy row pastes a copy of that
    /// component onto the target GameObject (public ComponentUtility copy/paste).
    /// Without Ctrl, Unity's default drag behavior is untouched.
    /// </summary>
    internal static class HierarchyXDropCopy {

        internal static void Handle(Rect rect, GameObject go, HierarchyXSettings s) {
            if (!s.dropCopyComponent)
                return;

            var e = Event.current;
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform)
                return;
            if (!e.control && !e.command)
                return;

            var full = rect;
            full.xMin = 0f;
            full.xMax = EditorGUIUtility.currentViewWidth;
            if (!full.Contains(e.mousePosition))
                return;

            var refs = DragAndDrop.objectReferences;
            if (refs == null || refs.Length != 1 || !(refs[0] is Component component) || component is Transform)
                return;

            if (e.type == EventType.DragUpdated) {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                e.Use();
                return;
            }

            DragAndDrop.AcceptDrag();
            if (ComponentUtility.CopyComponent(component))
                ComponentUtility.PasteComponentAsNew(go);
            e.Use();
        }
    }
}
