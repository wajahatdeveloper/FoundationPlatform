using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ProjectWindowX {
    /// <summary>
    /// Draws ProjectWindowX settings under Project Settings ▸ ProjectWindowX.
    /// Project-scoped (stored in ProjectSettings/), with JSON export/import.
    /// </summary>
    public static class ProjectWindowXSettingsProvider {

        private static SerializedObject serialized;
        private static ReorderableList folderRulesList;

        [SettingsProvider]
        public static SettingsProvider Create() {
            return new SettingsProvider("Project/ProjectWindowX", SettingsScope.Project) {
                label = "ProjectWindowX",
                guiHandler = OnGUI,
                keywords = new HashSet<string> {
                    "project", "folder", "icon", "extension", "zebra", "row",
                    "create", "script", "material", "shader", "template",
                    "authoring", "drift", "badge", "context", "menu"
                }
            };
        }

        private static void Ensure() {
            var settings = ProjectWindowXSettings.instance;
            if (serialized == null || serialized.targetObject != settings) {
                settings.hideFlags &= ~HideFlags.NotEditable;
                serialized = new SerializedObject(settings);
                BuildFolderRulesList();
            }
        }

        private static void OnGUI(string searchContext) {
            Ensure();
            serialized.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("enabled"), new GUIContent("Enabled"));

            Space();
            EditorGUILayout.LabelField("Rows", EditorStyles.boldLabel);
            var zebra = serialized.FindProperty("zebraRows");
            EditorGUILayout.PropertyField(zebra, new GUIContent("Zebra Rows"));
            using (new EditorGUI.DisabledScope(!zebra.boolValue))
                EditorGUILayout.PropertyField(serialized.FindProperty("oddRowColor"), new GUIContent("Odd Row Tint"));
            EditorGUILayout.PropertyField(serialized.FindProperty("extensionLabels"), new GUIContent("File Extensions"));

            Space();
            EditorGUILayout.LabelField("Folder Icons", EditorStyles.boldLabel);
            var folderIcons = serialized.FindProperty("folderIcons");
            EditorGUILayout.PropertyField(folderIcons, new GUIContent("Enable Folder Icons"));
            using (new EditorGUI.DisabledScope(!folderIcons.boolValue))
                folderRulesList.DoLayoutList();

            Space();
            EditorGUILayout.LabelField("Create Actions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("contextActions"), new GUIContent("Hover \"+\" Button"));
            EditorGUILayout.HelpBox("Hovering a row shows a \"+\" button with create actions (script, material, shader, folder, animator, custom editor...).", MessageType.None);

            Space();
            EditorGUILayout.LabelField("Authoring", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("authoringContextMenus"), new GUIContent("Authoring Context Menus"));
            var drift = serialized.FindProperty("driftBadges");
            EditorGUILayout.PropertyField(drift, new GUIContent("Out-of-Sync Badges"));
            using (new EditorGUI.DisabledScope(!drift.boolValue)) {
                EditorGUILayout.PropertyField(serialized.FindProperty("driftBadgeColor"), new GUIContent("Out-of-Sync Badge Color"));
                EditorGUILayout.PropertyField(serialized.FindProperty("driftBadgeTooltip"), new GUIContent("Out-of-Sync Badge Tooltip"));
                EditorGUILayout.PropertyField(serialized.FindProperty("driftBadgeIcon"), new GUIContent("Out-of-Sync Badge Icon"));
            }

            if (EditorGUI.EndChangeCheck()) {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                ProjectWindowXSettings.instance.SaveNow();
                EditorApplication.RepaintProjectWindow();
            }

            Space();
            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Export...", GUILayout.Width(90f))) {
                    var path = EditorUtility.SaveFilePanel("Export ProjectWindowX Settings", "", "ProjectWindowXSettings", "json");
                    if (!string.IsNullOrEmpty(path))
                        ProjectWindowXSettings.instance.ExportToJson(path);
                }
                if (GUILayout.Button("Import...", GUILayout.Width(90f))) {
                    var path = EditorUtility.OpenFilePanel("Import ProjectWindowX Settings", "", "json");
                    if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path)) {
                        ProjectWindowXSettings.instance.ImportFromJson(path);
                        serialized = null;
                        GUIUtility.ExitGUI();
                    }
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset to Defaults", GUILayout.Width(140f))) {
                    ProjectWindowXSettings.instance.ResetToDefaults();
                    serialized = null;
                    GUIUtility.ExitGUI();
                }
            }
        }

        private static void BuildFolderRulesList() {
            var prop = serialized.FindProperty("folderIconRules");
            folderRulesList = new ReorderableList(serialized, prop, true, true, true, true) {
                elementHeight = EditorGUIUtility.singleLineHeight * 2f + 8f
            };

            folderRulesList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "Folder Icon Rules (path → icon)");

            folderRulesList.drawElementCallback = (rect, index, active, focused) => {
                var element = prop.GetArrayElementAtIndex(index);
                var line = EditorGUIUtility.singleLineHeight;
                rect.y += 2f;

                var pathRect = new Rect(rect.x, rect.y, rect.width - 110f, line);
                var childrenRect = new Rect(rect.x + rect.width - 106f, rect.y, 106f, line);
                var iconNameRect = new Rect(rect.x, rect.y + line + 4f, rect.width * 0.5f - 4f, line);
                var iconTexRect = new Rect(rect.x + rect.width * 0.5f, rect.y + line + 4f, rect.width * 0.5f, line);

                EditorGUI.PropertyField(pathRect, element.FindPropertyRelative("folderPath"), GUIContent.none);
                var children = element.FindPropertyRelative("applyToChildren");
                children.boolValue = EditorGUI.ToggleLeft(childrenRect, "Children", children.boolValue);
                EditorGUI.PropertyField(iconNameRect, element.FindPropertyRelative("builtinIconName"), GUIContent.none);
                EditorGUI.PropertyField(iconTexRect, element.FindPropertyRelative("customIcon"), GUIContent.none);
            };
        }

        private static void Space() {
            EditorGUILayout.Space(8);
        }
    }
}
