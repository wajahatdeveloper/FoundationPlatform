#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.Identity;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Identity
{
[CustomPropertyDrawer(typeof(IdentityFieldAttribute))]
public sealed class IdentityFieldDrawer : PropertyDrawer
{
    private const float ButtonSpacing = 2f;

    // Text labels, not icons — "Copy" vs "Paste" share Unity's built-in Clipboard icon and are
    // visually indistinguishable at a glance, which is exactly how a probe field ends up filled
    // with an unrelated freshly-generated id instead of a pasted one.
    private static readonly GUIContent GenerateContent = new GUIContent("New", "Generate a brand-new random ID for THIS field. Do not use this to fill a lookup field that must match another entity's id.");
    private static readonly GUIContent PasteContent = new GUIContent("Paste", "Paste the ID from the clipboard (use after clicking Copy on the entity you want to match).");
    private static readonly GUIContent CopyContent = new GUIContent("Copy", "Copy this ID to the clipboard.");
    private static readonly GUIContent ClearContent = new GUIContent("Clear", "Clear this ID.");

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

        float clearWidth = GUI.skin.button.CalcSize(ClearContent).x;
        float copyWidth = GUI.skin.button.CalcSize(CopyContent).x;
        float pasteWidth = GUI.skin.button.CalcSize(PasteContent).x;
        float generateWidth = GUI.skin.button.CalcSize(GenerateContent).x;

        // Right-aligned buttons
        var clearRect = new Rect(
            position.xMax - clearWidth,
            position.y,
            clearWidth,
            position.height);

        var copyRect = new Rect(
            clearRect.x - ButtonSpacing - copyWidth,
            position.y,
            copyWidth,
            position.height);

        var pasteRect = new Rect(
            copyRect.x - ButtonSpacing - pasteWidth,
            position.y,
            pasteWidth,
            position.height);

        var generateRect = new Rect(
            pasteRect.x - ButtonSpacing - generateWidth,
            position.y,
            generateWidth,
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

        if (GUI.Button(generateRect, GenerateContent))
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

        if (GUI.Button(pasteRect, PasteContent))
        {
            string clipboard = EditorGUIUtility.systemCopyBuffer;
            foreach (var target in property.serializedObject.targetObjects)
            {
                using (var so = new SerializedObject(target))
                {
                    var prop = so.FindProperty(property.propertyPath);
                    if (prop != null)
                    {
                        prop.stringValue = clipboard;
                        so.ApplyModifiedProperties();
                    }
                }
            }
        }

        if (GUI.Button(copyRect, CopyContent))
        {
            EditorGUIUtility.systemCopyBuffer = property.stringValue;
        }

        if (GUI.Button(clearRect, ClearContent))
        {
            property.stringValue = string.Empty;
            property.serializedObject.ApplyModifiedProperties();
        }

        EditorGUI.EndProperty();
    }
}
}
#endif