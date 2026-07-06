#if UNITY_EDITOR
using FoundationPlatform.Behaviours;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InspectorSeparator))]
public class InspectorSeparatorEditor : Editor
{
    private SerializedProperty _labelProperty;
    private bool _showLabelField;

    private void OnEnable()
    {
        _labelProperty = serializedObject.FindProperty("_label");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.BeginHorizontal();
        if (_showLabelField)
        {
            EditorGUILayout.PropertyField(_labelProperty, GUIContent.none, GUILayout.ExpandWidth(true));
            if (GUILayout.Button(new GUIContent("−", "Collapse"), EditorStyles.miniButton, GUILayout.Width(22)))
                _showLabelField = false;
        }
        else
        {
            string labelValue = _labelProperty.stringValue;
            if (!string.IsNullOrEmpty(labelValue))
            {
                var labelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
                EditorGUILayout.LabelField($"——— {labelValue} ———", labelStyle, GUILayout.ExpandWidth(true));
            }
            else
            {
                Rect lineRect = GUILayoutUtility.GetRect(1, 2, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 1));
            }
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_editicon.sml"), EditorStyles.miniButton, GUILayout.Width(22)))
                _showLabelField = true;
        }
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
