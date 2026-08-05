#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using AetherNexus.FoundationPlatform.Utilities.Menus;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Messaging
{
    public class EventChannelGenerator : EditorWindow
    {
        private string _eventName = "MyEvent";
        private string _targetFolder = "Assets/Scripts";
        private bool _includeParameterlessConstructor = true;
        
        private Vector2 _scrollPosition;
        
		[MenuItem(MenuPaths.WindowUtilities.CreateEventChannel, priority = MenuPriorities.WindowUtilities + 5)]
        public static void ShowWindow()
        {
            var window = GetWindow<EventChannelGenerator>("Event Channel Generator");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Event Channel Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool writes a BaseGameEvent-derived C# class to disk (not a ScriptableObject).\n\n" +
                "Example: Enter 'PlayerDied' and this will create:\n" +
                "• PlayerDiedEvent : BaseGameEvent\n\n" +
                "Use EventBus.Publish/Subscribe directly for raising and subscribing.",
                MessageType.Info
            );
            
            EditorGUILayout.Space(10);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // Input fields
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            
            _eventName = EditorGUILayout.TextField("Event Name (without 'Event')", _eventName);
            
            EditorGUILayout.BeginHorizontal();
            _targetFolder = EditorGUILayout.TextField("Target Folder", _targetFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    _targetFolder = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            _includeParameterlessConstructor = EditorGUILayout.Toggle("Parameterless Constructor", _includeParameterlessConstructor);
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
            
            // Preview
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            var preview = GenerateCode();
            EditorGUILayout.TextArea(preview, GUILayout.Height(200));
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
            
            // Generate button
            if (GUILayout.Button("Generate Event Channel", GUILayout.Height(40)))
            {
                GenerateEventChannel();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private string GenerateCode()
        {
            var eventClassName = _eventName.EndsWith("Event") ? _eventName : _eventName + "Event";
            
            var code = $@"using UnityEngine;

// Event definition
public class {eventClassName} : BaseGameEvent
{{
    // Add your event properties here
    // Example:
    // public int Value {{ get; set; }}
    // public string Message {{ get; set; }}
    
    {((_includeParameterlessConstructor) ? $@"// Parameterless constructor
    public {eventClassName}()
    {{
    }}" : "")}
    
    // Add constructors with parameters as needed
    // Example:
    // public {eventClassName}(int value, string message)
    // {{
    //     Value = value;
    //     Message = message;
    // }}
}}
";
            
            return code;
        }
        
        private void GenerateEventChannel()
        {
            if (string.IsNullOrWhiteSpace(_eventName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter an event name.", "OK");
                return;
            }
            
            if (!AssetDatabase.IsValidFolder(_targetFolder))
            {
                EditorUtility.DisplayDialog("Error", $"Target folder '{_targetFolder}' does not exist.", "OK");
                return;
            }
            
            var eventClassName = _eventName.EndsWith("Event") ? _eventName : _eventName + "Event";
            var fileName = $"{eventClassName}.cs";
            var filePath = Path.Combine(_targetFolder, fileName);
            
            if (File.Exists(filePath))
            {
                if (!EditorUtility.DisplayDialog("File Exists", 
                    $"File '{fileName}' already exists. Overwrite?", "Yes", "Cancel"))
                {
                    return;
                }
            }
            
            var code = GenerateCode();
            File.WriteAllText(filePath, code);
            
            AssetDatabase.Refresh();
            
            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(filePath);
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            }
            
            EditorUtility.DisplayDialog("Success", 
                $"Event class written.\n\n" +
                $"File: {fileName}\n" +
                $"Location: {_targetFolder}\n\n" +
                "After Unity compiles, raise/subscribe with EventBus.Publish / EventBus.Subscribe.", 
                "OK");
        }
    }
}
#endif
