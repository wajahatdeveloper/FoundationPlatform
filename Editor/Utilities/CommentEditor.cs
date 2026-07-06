#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Comment))]
public class CommentEditor : Editor
{
    private SerializedProperty messageProperty;
    private SerializedProperty typeProperty;
    private SerializedProperty showInSceneViewProperty;
    private SerializedProperty gizmoColorProperty;
    private SerializedProperty gizmoSizeProperty;
    
    private bool isEditing = false;

    private void OnEnable()
    {
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
        // Compact header with type indicator and edit button
        EditorGUILayout.BeginHorizontal();
        
        // Type indicator with color
        Color originalColor = GUI.color;
        GUI.color = GetTypeColor(comment.Type);
        GUIStyle typeStyle = new GUIStyle(EditorStyles.boldLabel);
        typeStyle.fontSize = 12;
        EditorGUILayout.LabelField($"[{comment.Type}]", typeStyle, GUILayout.Width(60));
        GUI.color = originalColor;
        
        // Comment message (truncated if too long)
        string displayMessage = string.IsNullOrEmpty(comment.Message) ? "No message" : comment.Message;
        if (displayMessage.Length > 50)
        {
            displayMessage = displayMessage.Substring(0, 47) + "...";
        }
        
        EditorGUILayout.LabelField(displayMessage, EditorStyles.wordWrappedLabel);
        
        // Edit button
        if (GUILayout.Button("Edit", GUILayout.Width(40)))
        {
            isEditing = true;
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Scene view indicator
        if (comment.ShowInSceneView)
        {
            EditorGUILayout.LabelField("Visible in Scene View", EditorStyles.miniLabel);
        }
    }

    private void DrawFullEditor(Comment comment)
    {
        // Header with Done button
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Comment Editor", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Done", GUILayout.Width(50)))
        {
            isEditing = false;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        // Message field with better styling
        EditorGUILayout.LabelField("Message", EditorStyles.boldLabel);
        messageProperty.stringValue = EditorGUILayout.TextArea(messageProperty.stringValue, GUILayout.Height(60));
        EditorGUILayout.Space();

        // Comment type with color coding
        EditorGUILayout.LabelField("Type", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(typeProperty, GUIContent.none);
        
        // Show type-specific color preview
        Color originalColor = GUI.color;
        GUI.color = GetTypeColor(comment.Type);
        EditorGUILayout.LabelField($"Preview: {comment.Type}", EditorStyles.helpBox);
        GUI.color = originalColor;
        EditorGUILayout.Space();

        // Scene view options
        EditorGUILayout.LabelField("Scene View", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(showInSceneViewProperty, new GUIContent("Show in Scene View"));
        
        if (showInSceneViewProperty.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(gizmoColorProperty, new GUIContent("Custom Color"));
            EditorGUILayout.PropertyField(gizmoSizeProperty, new GUIContent("Gizmo Size"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();

        // Utility buttons
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Clear Message"))
        {
            messageProperty.stringValue = "";
        }
        
        if (GUILayout.Button("Reset to Default"))
        {
            messageProperty.stringValue = "Enter your comment here...";
            typeProperty.enumValueIndex = 0;
            showInSceneViewProperty.boolValue = true;
            gizmoColorProperty.colorValue = Color.white;
            gizmoSizeProperty.floatValue = 1f;
        }
        
        EditorGUILayout.EndHorizontal();

        // Info box
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This component is editor-only and will be disabled during play mode.", MessageType.Info);
    }

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
#endif