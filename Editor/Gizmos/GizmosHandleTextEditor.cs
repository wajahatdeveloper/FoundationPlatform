using UnityEngine;
using UnityEditor;

namespace FoundationPlatform.Gizmos
{
    [CustomEditor(typeof(GizmosHandleText))]
    [CanEditMultipleObjects]
    public class GizmosHandleTextEditor : UnityEditor.Editor
    {
        SerializedProperty enable;
        SerializedProperty text;

        void OnEnable()
        {
            enable = serializedObject.FindProperty("enable");
            text = serializedObject.FindProperty("text");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(enable);
            EditorGUILayout.PropertyField(text);

            if (!enable.hasMultipleDifferentValues && !enable.boolValue)
                EditorGUILayout.HelpBox("Label is hidden in the Scene view while disabled.", MessageType.Info, true);

            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                var handle = targets[i] as GizmosHandleText;
                if (handle == null || !handle.enable)
                    continue;

                Handles.Label(handle.transform.position, handle.text);
            }
        }
    }
}
