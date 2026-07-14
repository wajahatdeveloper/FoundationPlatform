using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
    #if UNITY_EDITOR
    using UnityEditor;
    #endif
    
    using Gizmos = UnityEngine.Gizmos;
    
[AddComponentMenu("Toolkit/Comment")]
public class Comment : MonoBehaviour
{
    [TextArea(3, 10)]
    [SerializeField] private string message = "Enter your comment here...";
    
    [SerializeField] private CommentType type = CommentType.Info;
    [SerializeField] private bool showInSceneView = true;
    [SerializeField] private Color gizmoColor = Color.white;
    [SerializeField] private float gizmoSize = 1f;

    public string Message => message;
    public CommentType Type => type;
    public bool ShowInSceneView => showInSceneView;
    public Color GizmoColor => gizmoColor;
    public float GizmoSize => gizmoSize;

    private void Awake()
    {
        // Disable the component in play mode since it's editor-only
        enabled = false;
    }

    private void OnValidate()
    {
        // Ensure the component is always disabled in play mode
        if (Application.isPlaying)
        {
            enabled = false;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showInSceneView || string.IsNullOrEmpty(message)) return;

        // Set gizmo color based on comment type
        Gizmos.color = GetGizmoColor();
        
        // Draw a simple icon or shape to represent the comment
        Vector3 position = transform.position;
        
        // Draw different shapes based on comment type
        switch (type)
        {
            case CommentType.Info:
                Gizmos.DrawWireSphere(position, gizmoSize * 0.5f);
                break;
            case CommentType.Warning:
                Gizmos.DrawWireCube(position, Vector3.one * gizmoSize);
                break;
            case CommentType.Error:
                Gizmos.DrawWireCube(position, Vector3.one * gizmoSize);
                Gizmos.DrawLine(position + Vector3.left * gizmoSize, position + Vector3.right * gizmoSize);
                Gizmos.DrawLine(position + Vector3.up * gizmoSize, position + Vector3.down * gizmoSize);
                break;
            case CommentType.Question:
                Gizmos.DrawWireSphere(position, gizmoSize * 0.5f);
                Gizmos.DrawLine(position + Vector3.up * gizmoSize * 0.5f, position + Vector3.up * gizmoSize);
                break;
        }
    }

    private Color GetGizmoColor()
    {
        switch (type)
        {
            case CommentType.Info:
                return Color.cyan;
            case CommentType.Warning:
                return Color.yellow;
            case CommentType.Error:
                return Color.red;
            case CommentType.Question:
                return Color.green;
            default:
                return gizmoColor;
        }
    }
#endif

    public enum CommentType
    {
        Info,
        Warning,
        Error,
        Question
    }
}}
