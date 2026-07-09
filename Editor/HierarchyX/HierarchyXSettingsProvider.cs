using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HierarchyX {
    /// <summary>
    /// Draws HierarchyX settings under Project Settings ▸ HierarchyX.
    /// Project-scoped so config is per-project (stored in ProjectSettings/).
    /// </summary>
    public static class HierarchyXSettingsProvider {

        private static SerializedObject serialized;
        private static ReorderableList layerColorsList;

        [SettingsProvider]
        public static SettingsProvider Create() {
            return new SettingsProvider("Project/HierarchyX", SettingsScope.Project) {
                label = "HierarchyX",
                guiHandler = OnGUI,
                keywords = new HashSet<string> {
                    "hierarchy", "tree", "row", "tag", "layer", "sorting", "separator", "selection",
                    "badge", "chip", "decorator", "domain", "placement"
                }
            };
        }

        private static void Ensure() {
            var settings = HierarchyXSettings.Instance;
            if (serialized == null || serialized.targetObject != settings) {
                serialized = new SerializedObject(settings);
                BuildLayerColorsList();
            }
        }

        private static void OnGUI(string searchContext) {
            Ensure();
            serialized.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("enabled"));
            EditorGUILayout.PropertyField(serialized.FindProperty("rightMargin"), new GUIContent("Right Margin"));

            Space();
            EditorGUILayout.LabelField("Enhanced Selection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("enhancedSelection"), new GUIContent("Right-drag Select"));

            Space();
            EditorGUILayout.LabelField("Tree Lines", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("drawTree"), new GUIContent("Draw Tree"));
            using (new EditorGUI.DisabledScope(!serialized.FindProperty("drawTree").boolValue)) {
                EditorGUILayout.PropertyField(serialized.FindProperty("treeOpacity"), new GUIContent("Opacity"));
                EditorGUILayout.PropertyField(serialized.FindProperty("stemProportion"), new GUIContent("Stem Length"));
                EditorGUILayout.PropertyField(serialized.FindProperty("selectOnTree"), new GUIContent("Select On Click"));
            }

            Space();
            EditorGUILayout.LabelField("Row Separator", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("lineThickness"), new GUIContent("Line Thickness"));
            EditorGUILayout.PropertyField(serialized.FindProperty("lineColor"), new GUIContent("Line Color"));

            Space();
            EditorGUILayout.LabelField("Row Colors", EditorStyles.boldLabel);
            var rowColors = serialized.FindProperty("rowColors");
            EditorGUILayout.PropertyField(rowColors, new GUIContent("Enable Row Colors"));
            using (new EditorGUI.DisabledScope(!rowColors.boolValue)) {
                EditorGUILayout.PropertyField(serialized.FindProperty("oddRowColor"), new GUIContent("Odd Row Tint"));
                EditorGUILayout.PropertyField(serialized.FindProperty("evenRowColor"), new GUIContent("Even Row Tint"));
                EditorGUILayout.Space(2);
                layerColorsList.DoLayoutList();
            }
            EditorGUILayout.PropertyField(serialized.FindProperty("rowDecorators"), new GUIContent("Row Decorators"));

            Space();
            EditorGUILayout.LabelField("Row Badges", EditorStyles.boldLabel);
            var rowBadges = serialized.FindProperty("rowBadges");
            EditorGUILayout.PropertyField(rowBadges, new GUIContent("Enable Chips"));
            using (new EditorGUI.DisabledScope(!rowBadges.boolValue)) {
                EditorGUILayout.PropertyField(serialized.FindProperty("badgePlacement"), new GUIContent("Placement"));
                EditorGUILayout.PropertyField(serialized.FindProperty("badgePadding"), new GUIContent("Padding"));
                EditorGUILayout.PropertyField(serialized.FindProperty("badgeSpacing"), new GUIContent("Spacing"));
                EditorGUILayout.PropertyField(serialized.FindProperty("badgeBackgroundOpacity"), new GUIContent("Background Opacity"));
            }
            if (rowBadges.boolValue && !serialized.FindProperty("rowDecorators").boolValue)
                EditorGUILayout.HelpBox("Chips are supplied by row decorators — enable Row Decorators above for chips to appear.", MessageType.Info);

            Space();
            EditorGUILayout.LabelField("Docked Setup Panel", EditorStyles.boldLabel);
            var panelEnabled = serialized.FindProperty("panelEnabled");
            EditorGUILayout.PropertyField(panelEnabled, new GUIContent("Enable Panel"));
            using (new EditorGUI.DisabledScope(!panelEnabled.boolValue)) {
                EditorGUILayout.PropertyField(serialized.FindProperty("panelCollapsed"), new GUIContent("Start Collapsed"));
                var height = serialized.FindProperty("panelHeight");
                height.floatValue = EditorGUILayout.Slider("Expanded Height", height.floatValue, 80f, 600f);
            }
            if (!HierarchyPanelHost.DockingSupported)
                EditorGUILayout.HelpBox(
                    "The docked footer is unavailable on this Unity version. Use " + HierarchyPanelWindow.MenuPath + " for the companion window.",
                    MessageType.Warning);

            Space();
            EditorGUILayout.LabelField("Mini Labels", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("miniLabels"), new GUIContent("Labels"), true);
            EditorGUILayout.PropertyField(serialized.FindProperty("smallerFont"), new GUIContent("Smaller Font"));
            EditorGUILayout.PropertyField(serialized.FindProperty("hideDefaultTag"), new GUIContent("Hide \"Untagged\""));
            EditorGUILayout.PropertyField(serialized.FindProperty("hideDefaultLayer"), new GUIContent("Hide \"Default\" Layer"));
            EditorGUILayout.PropertyField(serialized.FindProperty("centralizeWhenPossible"), new GUIContent("Center Single Label"));

            if (EditorGUI.EndChangeCheck()) {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var settings = (HierarchyXSettings)serialized.targetObject;
                settings.Save();
                EditorApplication.RepaintHierarchyWindow();
            }

            Space();
            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Export...", GUILayout.Width(90f))) {
                    var path = EditorUtility.SaveFilePanel("Export HierarchyX Settings", "", "HierarchyXSettings", "json");
                    if (!string.IsNullOrEmpty(path))
                        ((HierarchyXSettings)serialized.targetObject).ExportToJson(path);
                }

                if (GUILayout.Button("Import...", GUILayout.Width(90f))) {
                    var path = EditorUtility.OpenFilePanel("Import HierarchyX Settings", "", "json");
                    if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path)) {
                        ((HierarchyXSettings)serialized.targetObject).ImportFromJson(path);
                        serialized = null;
                        GUIUtility.ExitGUI();
                    }
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Reset to Defaults", GUILayout.Width(140f))) {
                    var settings = (HierarchyXSettings)serialized.targetObject;
                    var flags = settings.hideFlags;
                    var fresh = ScriptableObject.CreateInstance<HierarchyXSettings>();
                    EditorUtility.CopySerialized(fresh, settings);
                    Object.DestroyImmediate(fresh);
                    settings.hideFlags = flags;
                    settings.ApplySkinDefaults();
                    settings.Save();
                    serialized = null;
                    EditorApplication.RepaintHierarchyWindow();
                }
            }
        }

        private static void Space() {
            EditorGUILayout.Space(8);
        }

        private static void BuildLayerColorsList() {
            var prop = serialized.FindProperty("perLayerColors");
            layerColorsList = new ReorderableList(serialized, prop, true, true, true, true);

            layerColorsList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "Per-Layer Row Colors");

            layerColorsList.drawElementCallback = (rect, index, active, focused) => {
                var element = prop.GetArrayElementAtIndex(index);
                var layer = element.FindPropertyRelative("layer");
                var color = element.FindPropertyRelative("color");
                var mode = element.FindPropertyRelative("mode");

                rect.y += 2f;
                rect.height = EditorGUIUtility.singleLineHeight;

                var third = rect.width / 3f;
                var layerRect = new Rect(rect.x, rect.y, third - 4f, rect.height);
                var colorRect = new Rect(rect.x + third, rect.y, third - 4f, rect.height);
                var modeRect = new Rect(rect.x + third * 2f, rect.y, third, rect.height);

                layer.intValue = EditorGUI.LayerField(layerRect, layer.intValue);
                EditorGUI.PropertyField(colorRect, color, GUIContent.none);
                EditorGUI.PropertyField(modeRect, mode, GUIContent.none);
            };

            layerColorsList.onAddCallback = list => {
                var i = prop.arraySize;
                prop.InsertArrayElementAtIndex(i);
                var element = prop.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("layer").intValue = 0;
                element.FindPropertyRelative("color").colorValue = new Color(1f, 1f, 1f, 0.15f);
                element.FindPropertyRelative("mode").enumValueIndex = (int)TintMode.GradientRightToLeft;
            };
        }
    }
}
