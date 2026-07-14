#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AetherNexus.FoundationPlatform.FrameworkInspector.Editor
{
    /// <summary>
    /// The engine's enhanced object-reference field: optional pencil button (opens a floating
    /// Property Editor), drag-out (drag the referenced object from the field), and a
    /// right-click selector popup listing compatible scene objects and assets.
    /// Pure layering over EditorGUI.ObjectField — no editor internals.
    /// </summary>
    internal static class ObjectFieldX
    {
        private const float PencilWidth = 18f;
        private const float PickerDotWidth = 20f;   // ObjectField's built-in picker button zone
        private const float DragThreshold = 6f;

        private static Vector2 mouseDownPosition;
        private static bool mouseDownInField;

        internal static Object Draw(Rect rect, GUIContent label, Object value, Type type, bool allowScene, SerializedProperty prop)
        {
            var s = InspectorXSettings.instance;
            var fieldRect = rect;

            if (s.objectFieldPencil && value != null)
            {
                fieldRect.xMax -= PencilWidth + 2f;
                var pencilRect = new Rect(fieldRect.xMax + 2f, rect.y, PencilWidth, EditorGUIUtility.singleLineHeight);
                var icon = EditorGUIUtility.IconContent("editicon.sml");
                icon.tooltip = "Open in Property Editor";
                if (GUI.Button(pencilRect, icon, EditorStyles.iconButton))
                    EditorUtility.OpenPropertyEditor(value);
            }

            // The interactive content zone: field body minus label and minus the picker dot.
            var contentRect = fieldRect;
            contentRect.xMin += EditorGUIUtility.labelWidth;
            contentRect.xMax -= PickerDotWidth;

            if (s.objectFieldDragOut && value != null)
                HandleDragOut(contentRect, value);

            if (s.objectFieldSelector && prop != null)
                HandleSelector(contentRect, type, allowScene, prop);

            return EditorGUI.ObjectField(fieldRect, label, value, type, allowScene);
        }

        private static void HandleDragOut(Rect contentRect, Object value)
        {
            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    // Record only — never consume, so click-to-ping and double-click-to-open keep working.
                    if (e.button == 0 && contentRect.Contains(e.mousePosition))
                    {
                        mouseDownPosition = e.mousePosition;
                        mouseDownInField = true;
                    }
                    else
                    {
                        mouseDownInField = false;
                    }
                    break;

                case EventType.MouseDrag:
                    if (mouseDownInField && (e.mousePosition - mouseDownPosition).magnitude > DragThreshold)
                    {
                        mouseDownInField = false;
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new[] { value };
                        DragAndDrop.StartDrag(value.name);
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                case EventType.DragExited:
                    mouseDownInField = false;
                    break;
            }
        }

        private static void HandleSelector(Rect contentRect, Type type, bool allowScene, SerializedProperty prop)
        {
            var e = Event.current;
            if (e.type != EventType.ContextClick || !contentRect.Contains(e.mousePosition))
                return;

            ObjectSelectorPopupX.Open(new Rect(e.mousePosition, Vector2.zero), type, allowScene, prop);
            e.Use();
        }
    }
}
#endif
