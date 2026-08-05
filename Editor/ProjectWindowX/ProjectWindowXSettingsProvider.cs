using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ProjectWindowX {
    /// <summary>
    /// Draw hooks for packages that extend Project Settings ▸ ProjectWindowX without ProjectWindowX
    /// referencing them (avoids circular asmdefs). Call <see cref="Register"/> from
    /// <c>[InitializeOnLoadMethod]</c> in the contributing assembly.
    /// </summary>
    public static class ProjectWindowXSettingsExtras {
        private static readonly List<(string Title, Action Drawer)> drawers = new List<(string, Action)>();

        public static void Register(string title, Action drawer) {
            if (drawer == null)
                throw new ArgumentNullException(nameof(drawer));
            if (string.IsNullOrEmpty(title))
                throw new ArgumentException("Title is required.", nameof(title));
            for (var i = 0; i < drawers.Count; i++) {
                if (drawers[i].Title == title) {
                    drawers[i] = (title, drawer);
                    return;
                }
            }
            drawers.Add((title, drawer));
        }

        internal static void DrawAll() {
            for (var i = 0; i < drawers.Count; i++) {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField(drawers[i].Title, EditorStyles.boldLabel);
                drawers[i].Drawer();
            }
        }

        internal static void CollectKeywords(HashSet<string> keywords) {
            for (var i = 0; i < drawers.Count; i++) {
                var title = drawers[i].Title;
                if (!string.IsNullOrEmpty(title))
                    keywords.Add(title.ToLowerInvariant());
            }
        }
    }

    /// <summary>
    /// Draws ProjectWindowX settings under Project Settings ▸ ProjectWindowX.
    /// Project-scoped (stored in ProjectSettings/), with JSON export/import.
    /// </summary>
    public static class ProjectWindowXSettingsProvider {

        private static SerializedObject serialized;
        private static ReorderableList folderRulesList;

        [SettingsProvider]
        public static SettingsProvider Create() {
            var keywords = new HashSet<string> {
                "project", "folder", "icon", "extension", "zebra", "row",
                "create", "script", "material", "shader", "template",
                "authoring", "drift", "sync", "out of sync", "badge", "context", "menu"
            };
            ProjectWindowXSettingsExtras.CollectKeywords(keywords);
            return new SettingsProvider("Project/ProjectWindowX", SettingsScope.Project) {
                label = "ProjectWindowX",
                guiHandler = OnGUI,
                keywords = keywords
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
            EditorGUILayout.LabelField("Context Panel", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("panelEnabled"), new GUIContent("Docked Panel"));
            EditorGUILayout.PropertyField(serialized.FindProperty("panelCollapsed"), new GUIContent("Collapsed"));
            EditorGUILayout.PropertyField(serialized.FindProperty("panelStatusChips"), new GUIContent("Status Chips"));
            EditorGUILayout.HelpBox("Docks above Unity's Project status bar; spans one- and two-column layouts.", MessageType.None);

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
                FolderIcons.NotifyRulesChanged();
                EditorApplication.RepaintProjectWindow();
            }

            ProjectWindowXSettingsExtras.DrawAll();

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
                elementHeight = EditorGUIUtility.singleLineHeight * 4f + 20f
            };

            folderRulesList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "Folder Icon Rules (path → icon)");

            folderRulesList.drawElementCallback = (rect, index, active, focused) => {
                var element = prop.GetArrayElementAtIndex(index);
                float line = EditorGUIUtility.singleLineHeight;
                float indent = 14f;
                rect.y += 3f;
                rect.x += indent;
                float usableWidth = rect.width - indent;

                // Row 1 — folder path (60%) + apply-to-children toggle (20%) + apply-to-hierarchy toggle (20%)
                var pathRect  = new Rect(rect.x,                          rect.y, usableWidth * 0.6f - 4f, line);
                var childRect = new Rect(rect.x + usableWidth * 0.6f,     rect.y, usableWidth * 0.2f,     line);
                var hierRect  = new Rect(rect.x + usableWidth * 0.8f + 2f, rect.y, usableWidth * 0.2f,  line);
                EditorGUI.PropertyField(pathRect,  element.FindPropertyRelative("folderPath"), GUIContent.none);
                EditorGUI.PropertyField(childRect, element.FindPropertyRelative("applyToChildren"), new GUIContent("Apply To Children"));
                EditorGUI.PropertyField(hierRect,  element.FindPropertyRelative("applyToHierarchy"),
                    new GUIContent("Apply To Hierarchy", "Also render this icon in the Hierarchy window for assets from this folder."));

                // Row 2 — builtin icon name (full width, labelled)
                var iconNameRect = new Rect(rect.x, rect.y + line + 4f, usableWidth, line);
                EditorGUI.PropertyField(iconNameRect, element.FindPropertyRelative("builtinIconName"),
                    new GUIContent("Built-in Icon"));

                // Row 3 — custom texture (full width, labelled)
                var texRect = new Rect(rect.x, rect.y + (line + 4f) * 2f, usableWidth, line);
                EditorGUI.PropertyField(texRect, element.FindPropertyRelative("customIcon"),
                    new GUIContent("Custom Texture"));
            };
        }

        private static void Space() {
            EditorGUILayout.Space(8);
        }
    }
}
