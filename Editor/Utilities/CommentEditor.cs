#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.AetherInspector;
using AetherNexus.FoundationPlatform.AetherInspector.Editor;
using AetherNexus.FoundationPlatform.Extensions;
using UnityEngine;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.Editor.Utilities
{
[CustomEditor(typeof(Comment))]
[CanEditMultipleObjects]
public class CommentEditor : AetherInspectorEditor
{
    private SerializedProperty messageProperty;
    private SerializedProperty typeProperty;
    private SerializedProperty showInSceneViewProperty;
    private SerializedProperty gizmoColorProperty;
    private SerializedProperty gizmoSizeProperty;
    
    private bool isEditing = false;

    protected override void OnEnable()
    {
        base.OnEnable();
        messageProperty = serializedObject.FindProperty("message");
        typeProperty = serializedObject.FindProperty("type");
        showInSceneViewProperty = serializedObject.FindProperty("showInSceneView");
        gizmoColorProperty = serializedObject.FindProperty("gizmoColor");
        gizmoSizeProperty = serializedObject.FindProperty("gizmoSize");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        Comment comment = (Comment)target;

        if (!isEditing)
        {
            DrawCompactView(comment);
        }
        else
        {
            DrawFullEditor(comment);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCompactView(Comment comment)
    {
        string displayMessage = string.IsNullOrEmpty(comment.Message) ? "No message" : comment.Message;
        GuiKit.InfoBox(displayMessage, ToMessageType(comment.Type));

        using (GuiKit.ActionRow())
        {
            if (comment.ShowInSceneView)
                EditorGUILayout.LabelField("Visible in Scene View", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GuiKit.ActionButton("Edit", 60f))
                isEditing = true;
        }
    }

    private void DrawFullEditor(Comment comment)
    {
        using (GuiKit.ActionRow())
        {
            GuiKit.Title("Comment Editor");
            GUILayout.FlexibleSpace();
            if (GuiKit.ActionButton("Done", 60f))
                isEditing = false;
        }

        GuiKit.Title("Message");
        messageProperty.stringValue = EditorGUILayout.TextArea(messageProperty.stringValue, GUILayout.Height(60));

        GuiKit.Title("Type");
        EditorGUILayout.PropertyField(typeProperty, GUIContent.none);
        GuiKit.InfoBox($"Preview: {comment.Type}", ToMessageType(comment.Type));

        GuiKit.Title("Scene View");
        EditorGUILayout.PropertyField(showInSceneViewProperty, new GUIContent("Show in Scene View"));

        if (showInSceneViewProperty.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(gizmoColorProperty, new GUIContent("Custom Color"));
            EditorGUILayout.PropertyField(gizmoSizeProperty, new GUIContent("Gizmo Size"));
            EditorGUI.indentLevel--;
        }

        using (GuiKit.ActionRow())
        {
            if (GuiKit.ActionButton("Clear Message"))
            {
                messageProperty.stringValue = "";
            }

            if (GuiKit.ActionButton("Reset to Default"))
            {
                messageProperty.stringValue = "Enter your comment here...";
                typeProperty.enumValueIndex = 0;
                showInSceneViewProperty.boolValue = true;
                gizmoColorProperty.colorValue = Color.white;
                gizmoSizeProperty.floatValue = 1f;
            }
        }

        GuiKit.InfoBox("This component is editor-only and will be disabled during play mode.", InfoMessageType.Info);
    }

    private static InfoMessageType ToMessageType(Comment.CommentType type) => type switch
    {
        Comment.CommentType.Warning => InfoMessageType.Warning,
        Comment.CommentType.Error => InfoMessageType.Error,
        _ => InfoMessageType.Info
    };

    private Color GetTypeColor(Comment.CommentType type)
    {
        switch (type)
        {
            case Comment.CommentType.Info:
                return Color.cyan;
            case Comment.CommentType.Warning:
                return Color.yellow;
            case Comment.CommentType.Error:
                return Color.red;
            case Comment.CommentType.Question:
                return Color.green;
            default:
                return Color.white;
        }
    }

    private void OnSceneGUI()
    {
        Comment comment = (Comment)target;
        
        if (!comment.ShowInSceneView || string.IsNullOrEmpty(comment.Message)) return;

        // Draw a label in the scene view
        Handles.BeginGUI();
        
        Vector3 worldPosition = comment.transform.position;
        Vector2 screenPosition = HandleUtility.WorldToGUIPoint(worldPosition);
        
        // Offset the label slightly above the gizmo
        screenPosition.y -= 20;
        
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = GetTypeColor(comment.Type);
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        
        // Draw background
        Vector2 labelSize = labelStyle.CalcSize(new GUIContent(comment.Message));
        Rect backgroundRect = new Rect(screenPosition.x - labelSize.x / 2, screenPosition.y - 5, labelSize.x + 10, labelSize.y + 10);
        
        Color originalColor = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(backgroundRect, Texture2D.whiteTexture);
        GUI.color = originalColor;
        
        // Draw text
        GUI.Label(new Rect(screenPosition.x - labelSize.x / 2, screenPosition.y, labelSize.x, labelSize.y), comment.Message, labelStyle);
        
        Handles.EndGUI();
    }
}
}
#endif