#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FoundationPlatform.FrameworkInspector.Editor
{
    /// <summary>Draws FrameworkInspector feature settings under Project Settings ▸ FrameworkInspector.</summary>
    public static class InspectorXSettingsProvider
    {
        private static SerializedObject serialized;

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new SettingsProvider("Project/FrameworkInspector", SettingsScope.Project)
            {
                label = "FrameworkInspector",
                guiHandler = OnGUI,
                keywords = new HashSet<string>
                {
                    "inspector", "object", "field", "pencil", "drag", "selector",
                    "missing", "script", "fixer", "play", "save", "event"
                }
            };
        }

        private static void Ensure()
        {
            var settings = InspectorXSettings.instance;
            if (serialized == null || serialized.targetObject != settings)
            {
                settings.hideFlags &= ~HideFlags.NotEditable;
                serialized = new SerializedObject(settings);
            }
        }

        private static void OnGUI(string searchContext)
        {
            Ensure();
            serialized.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Object Fields", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("objectFieldPencil"), new GUIContent("Pencil (Open Property Editor)"));
            EditorGUILayout.PropertyField(serialized.FindProperty("objectFieldDragOut"), new GUIContent("Drag From Field"));
            EditorGUILayout.PropertyField(serialized.FindProperty("objectFieldSelector"), new GUIContent("Right-Click Selector"));
            EditorGUILayout.HelpBox("Object-field enhancements apply where the FrameworkInspector engine draws the field. Inspectors using their own concrete custom editors or property drawers are unaffected.", MessageType.None);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Components", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("missingScriptFixer"), new GUIContent("Missing Script Fixer"));
            EditorGUILayout.PropertyField(serialized.FindProperty("saveComponentValuesInPlayMode"), new GUIContent("Play-Mode Value Saver"));
            EditorGUILayout.PropertyField(serialized.FindProperty("unityEventDrop"), new GUIContent("UnityEvent Drop Target"));

            if (EditorGUI.EndChangeCheck())
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                InspectorXSettings.instance.SaveNow();
            }

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Export...", GUILayout.Width(90f)))
                {
                    var path = EditorUtility.SaveFilePanel("Export FrameworkInspector Settings", "", "FrameworkInspectorXSettings", "json");
                    if (!string.IsNullOrEmpty(path))
                        InspectorXSettings.instance.ExportToJson(path);
                }
                if (GUILayout.Button("Import...", GUILayout.Width(90f)))
                {
                    var path = EditorUtility.OpenFilePanel("Import FrameworkInspector Settings", "", "json");
                    if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                    {
                        InspectorXSettings.instance.ImportFromJson(path);
                        serialized = null;
                        GUIUtility.ExitGUI();
                    }
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset to Defaults", GUILayout.Width(140f)))
                {
                    InspectorXSettings.instance.ResetToDefaults();
                    serialized = null;
                    GUIUtility.ExitGUI();
                }
            }
        }
    }
}
#endif
