#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AetherNexus.FoundationPlatform.FrameworkInspector.Editor
{
    /// <summary>
    /// Drop a GameObject or Component onto a UnityEvent field to append a persistent
    /// listener targeting it (method left as "No Function" for Unity's own dropdown).
    /// Everything goes through the SerializedProperty structure — no internals.
    /// </summary>
    internal static class UnityEventDropTarget
    {
        internal static void Handle(Rect rect, SerializedProperty eventProp)
        {
            if (!InspectorXSettings.instance.unityEventDrop)
                return;

            var e = Event.current;
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform)
                return;
            if (!rect.Contains(e.mousePosition))
                return;

            var target = SingleDraggedTarget();
            if (target == null)
                return;

            if (e.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                e.Use();
                return;
            }

            DragAndDrop.AcceptDrag();
            AppendListener(eventProp, target);
            e.Use();
        }

        private static Object SingleDraggedTarget()
        {
            var refs = DragAndDrop.objectReferences;
            if (refs == null || refs.Length != 1)
                return null;
            var obj = refs[0];
            return (obj is GameObject || obj is Component) ? obj : null;
        }

        private static void AppendListener(SerializedProperty eventProp, Object target)
        {
            var calls = eventProp.FindPropertyRelative("m_PersistentCalls.m_Calls");
            if (calls == null || !calls.isArray)
                return;

            calls.arraySize++;
            var call = calls.GetArrayElementAtIndex(calls.arraySize - 1);
            call.FindPropertyRelative("m_Target").objectReferenceValue = target;
            call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = target.GetType().AssemblyQualifiedName;
            call.FindPropertyRelative("m_MethodName").stringValue = string.Empty;
            call.FindPropertyRelative("m_Mode").enumValueIndex = 1;      // PersistentListenerMode.Void
            call.FindPropertyRelative("m_CallState").enumValueIndex = 2; // UnityEventCallState.RuntimeOnly

            eventProp.serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
