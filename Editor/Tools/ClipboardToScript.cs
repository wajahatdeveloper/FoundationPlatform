#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Reflection;

public static class ClipboardToScript
{
    [MenuItem("Assets/Create/From Clipboard/C# Script")]
    static void CreateScript()
    {
        Show(FileType.CSharp);
    }

    [MenuItem("Assets/Create/From Clipboard/Shader")]
    static void CreateShader()
    {
        Show(FileType.Shader);
    }

    [MenuItem("Assets/Create/From Clipboard/Text File")]
    static void CreateTextFile()
    {
        Show(FileType.TXT);
    }

    [MenuItem("Assets/Create/From Clipboard/C# Script", true)]
    [MenuItem("Assets/Create/From Clipboard/Shader", true)]
    [MenuItem("Assets/Create/From Clipboard/Text File", true)]
    static bool ValidateCreate()
    {
        return true;
    }

    static void Show(FileType type)
    {
        var window = EditorWindow.GetWindow<CreateNewFileWithName>(true, "File Name");
        window.minSize = new Vector2(220f, 70f);
        window.maxSize = window.minSize;
        window.fileType = type;
        window.ShowModalUtility();
    }
}

public enum FileType
{
    CSharp,
    Shader,
    TXT
}

public class CreateNewFileWithName : EditorWindow
{
    string m_FileName = string.Empty;
    bool didFocus = false;

    public FileType fileType { get; set; }

    private void OnEnable()
    {
        titleContent = new GUIContent("File Name");
    }

    private void OnGUI()
    {
        GUILayout.Space(6f);
        fileType = (FileType)EditorGUILayout.EnumPopup(fileType);

        Event current = Event.current;
        bool submit = (current.type == EventType.KeyDown) &&
                      ((current.keyCode == KeyCode.Return) || (current.keyCode == KeyCode.KeypadEnter));
        bool cancel = (current.type == EventType.KeyDown) && current.keyCode == KeyCode.Escape;

        GUI.SetNextControlName("m_PreferencesName");
        var prevLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 70f;
        m_FileName = EditorGUILayout.TextField("Filename", m_FileName, new GUILayoutOption[0]);
        EditorGUIUtility.labelWidth = prevLabelWidth;
        if (!didFocus)
        {
            didFocus = true;
            EditorGUI.FocusTextInControl("m_PreferencesName");
        }

        if (cancel)
        {
            Close();
            GUIUtility.ExitGUI();
        }

        EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(m_FileName));
        if (GUILayout.Button("Save", new GUILayoutOption[0]) || submit)
        {
            if (TryCreate())
            {
                Close();
                GUIUtility.ExitGUI();
            }
        }
        EditorGUI.EndDisabledGroup();
    }

    bool TryCreate()
    {
        string basePath = GetTargetFolderPath();
        if (string.IsNullOrEmpty(basePath))
            basePath = "Assets";

        string safeName = SanitizeFileName(m_FileName);
        if (string.IsNullOrEmpty(safeName))
        {
            EditorUtility.DisplayDialog("Invalid name", "Please enter a valid file name.", "OK");
            return false;
        }

        string extension;
        switch (fileType)
        {
            case FileType.CSharp:
                extension = ".cs";
                break;
            case FileType.Shader:
                extension = ".shader";
                break;
            case FileType.TXT:
            default:
                extension = ".txt";
                break;
        }

        string desiredPath = basePath.Replace("\\", "/") + "/" + safeName + extension;
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(desiredPath);

        string content = EditorGUIUtility.systemCopyBuffer ?? string.Empty;
        ProjectWindowUtil.CreateAssetWithContent(uniquePath, content);
        return true;
    }

    static string GetTargetFolderPath()
    {
        // Try call to ProjectWindowUtil.GetActiveFolderPath via reflection for cross-version safety
        var method = typeof(ProjectWindowUtil).GetMethod("GetActiveFolderPath", BindingFlags.Public | BindingFlags.Static);
        if (method != null)
        {
            var value = method.Invoke(null, null) as string;
            if (!string.IsNullOrEmpty(value)) return value.Replace("\\", "/");
        }
        var obj = Selection.activeObject;
        var path = obj != null ? AssetDatabase.GetAssetPath(obj) : null;
        if (string.IsNullOrEmpty(path)) return "Assets";
        if (Directory.Exists(path)) return path.Replace("\\", "/");
        var dir = Path.GetDirectoryName(path);
        return string.IsNullOrEmpty(dir) ? "Assets" : dir.Replace("\\", "/");
    }

    static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Trim().Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return cleaned;
    }
}
#endif