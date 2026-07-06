#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(IdentityFieldAttribute))]
public sealed class IdentityFieldDrawer : PropertyDrawer
{
    private const float ButtonWidth = 22f;
    private const float ButtonSpacing = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text,
                "IdentityField attribute supports string fields only.");
            return;
        }

        EditorGUI.BeginProperty(position, label, property);

        // Measure the label so it only takes the width it needs.
        var idLabel = EditorGUIUtility.TrTextContent("ID");
        float labelWidth = GUI.skin.label.CalcSize(idLabel).x + 4f;

        // Right-aligned buttons
        var clearRect = new Rect(
            position.xMax - ButtonWidth,
            position.y,
            ButtonWidth,
            position.height);

        var copyRect = new Rect(
            clearRect.x - ButtonSpacing - ButtonWidth,
            position.y,
            ButtonWidth,
            position.height);

        var generateRect = new Rect(
            copyRect.x - ButtonSpacing - ButtonWidth,
            position.y,
            ButtonWidth,
            position.height);

        // Label
        var labelRect = new Rect(
            position.x,
            position.y,
            labelWidth,
            position.height);

        EditorGUI.LabelField(labelRect, idLabel);

        // Remaining space for ID
        var fieldRect = new Rect(
            labelRect.xMax,
            position.y,
            generateRect.x - labelRect.xMax - 4f,
            position.height);

        EditorGUI.BeginDisabledGroup(true);
        EditorGUI.TextField(
            fieldRect,
            string.IsNullOrEmpty(property.stringValue)
                ? "(not set)"
                : property.stringValue);
        EditorGUI.EndDisabledGroup();

        // Icons
        GUIContent generateIcon =
            EditorGUIUtility.IconContent("Refresh");

        GUIContent copyIcon =
            EditorGUIUtility.IconContent("Clipboard");

        GUIContent clearIcon =
            EditorGUIUtility.IconContent("TreeEditor.Trash");

        generateIcon.tooltip = "Generate ID";
        copyIcon.tooltip = "Copy ID";
        clearIcon.tooltip = "Clear ID";

        if (GUI.Button(generateRect, generateIcon))
        {
            foreach (var target in property.serializedObject.targetObjects)
            {
                using (var so = new SerializedObject(target))
                {
                    var prop = so.FindProperty(property.propertyPath);
                    if (prop != null)
                    {
                        prop.stringValue = $"e:{System.Guid.NewGuid():N}";
                        so.ApplyModifiedProperties();
                    }
                }
            }
        }

        if (GUI.Button(copyRect, copyIcon))
        {
            EditorGUIUtility.systemCopyBuffer = property.stringValue;
        }

        if (GUI.Button(clearRect, clearIcon))
        {
            property.stringValue = string.Empty;
            property.serializedObject.ApplyModifiedProperties();
        }

        EditorGUI.EndProperty();
    }
}
#endif