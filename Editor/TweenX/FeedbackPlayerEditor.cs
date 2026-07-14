#if UNITY_EDITOR
using System;
using AetherNexus.FoundationPlatform.TweenX.Feedbacks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.TweenX.EditorTools
{
    /// <summary>
    /// Inspector for <see cref="FeedbackPlayer"/>: a reorderable, polymorphic feedback list plus an
    /// Add-Feedback dropdown auto-populated from every concrete <see cref="Feedback"/> subclass
    /// (TypeCache) — no central registry to edit when a new feedback type is added. Play/Stop buttons
    /// appear in Play mode.
    /// </summary>
    [CustomEditor(typeof(FeedbackPlayer))]
    public sealed class FeedbackPlayerEditor : UnityEditor.Editor
    {
        private SerializedProperty _playOnEnable;
        private SerializedProperty _clock;
        private SerializedProperty _feedbacks;
        private ReorderableList _list;

        private void OnEnable()
        {
            _playOnEnable = serializedObject.FindProperty("PlayOnEnable");
            _clock = serializedObject.FindProperty("Clock");
            _feedbacks = serializedObject.FindProperty("Feedbacks");

            _list = new ReorderableList(serializedObject, _feedbacks, true, true, false, true)
            {
                drawHeaderCallback = r => EditorGUI.LabelField(r, "Feedbacks"),
                elementHeightCallback = i =>
                    EditorGUI.GetPropertyHeight(_feedbacks.GetArrayElementAtIndex(i), true) + 4f,
                drawElementCallback = DrawElement,
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_playOnEnable);
            EditorGUILayout.PropertyField(_clock);
            EditorGUILayout.Space(4f);
            _list.DoLayoutList();

            var addRect = EditorGUILayout.GetControlRect(false, 22f);
            if (GUI.Button(addRect, "Add Feedback  ▾")) ShowAddMenu();

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(6f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Play")) ((FeedbackPlayer)target).Play();
                    if (GUILayout.Button("Stop")) ((FeedbackPlayer)target).Stop();
                }
            }
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _feedbacks.GetArrayElementAtIndex(index);
            rect.y += 2f;
            rect.height = EditorGUI.GetPropertyHeight(element, true);
            EditorGUI.PropertyField(rect, element, new GUIContent(LabelFor(element)), true);
        }

        private static string LabelFor(SerializedProperty element)
        {
            string full = element.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(full)) return "(empty)";

            // Format is "<assembly> <namespace.Type>" — take the type name after the last dot.
            int space = full.LastIndexOf(' ');
            string typePart = space >= 0 ? full.Substring(space + 1) : full;
            int dot = typePart.LastIndexOf('.');
            string typeName = dot >= 0 ? typePart.Substring(dot + 1) : typePart;
            string pretty = typeName.StartsWith("Feedback") ? typeName.Substring("Feedback".Length) : typeName;

            var labelProp = element.FindPropertyRelative("Label");
            string custom = labelProp != null ? labelProp.stringValue : null;
            return string.IsNullOrEmpty(custom) ? pretty : $"{pretty} — {custom}";
        }

        private void ShowAddMenu()
        {
            var menu = new GenericMenu();
            foreach (var type in TypeCache.GetTypesDerivedFrom<Feedback>())
            {
                if (type.IsAbstract || type.IsGenericTypeDefinition || type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                string name = type.Name.StartsWith("Feedback") ? type.Name.Substring("Feedback".Length) : type.Name;
                var captured = type;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(name)), false, () => AddFeedback(captured));
            }
            menu.ShowAsContext();
        }

        private void AddFeedback(Type type)
        {
            serializedObject.Update();
            int idx = _feedbacks.arraySize;
            _feedbacks.arraySize++;
            _feedbacks.GetArrayElementAtIndex(idx).managedReferenceValue = Activator.CreateInstance(type);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
