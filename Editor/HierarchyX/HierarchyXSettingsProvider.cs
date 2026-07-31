using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HierarchyX {
    /// <summary>
    /// Draw hooks for packages that extend Project Settings ▸ HierarchyX without HierarchyX
    /// referencing them (avoids circular asmdefs). Call <see cref="Register"/> from
    /// <c>[InitializeOnLoadMethod]</c> in the contributing assembly.
    /// </summary>
    public static class HierarchyXSettingsExtras {
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
    /// Draws HierarchyX settings under Project Settings ▸ HierarchyX.
    /// Project-scoped so config is per-project (stored in ProjectSettings/).
    /// </summary>
    public static class HierarchyXSettingsProvider {

        private static SerializedObject serialized;
        private static ReorderableList layerColorsList;

        [SettingsProvider]
        public static SettingsProvider Create() {
            var keywords = new HashSet<string> {
                "hierarchy", "tree", "row", "tag", "layer", "sorting", "separator", "selection",
                "badge", "chip", "decorator", "domain", "placement",
                "focus", "double-click", "2d", "frame", "recttransform",
                "middle-click", "active", "toggle",
                "stale", "component", "guard", "orphan"
            };
            HierarchyXSettingsExtras.CollectKeywords(keywords);
            return new SettingsProvider("Project/HierarchyX", SettingsScope.Project) {
                label = "HierarchyX",
                guiHandler = OnGUI,
                keywords = keywords
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
            EditorGUILayout.PropertyField(serialized.FindProperty("middleClickToggleActive"), new GUIContent("Middle-Click Toggle Active"));

            Space();
            EditorGUILayout.LabelField("Double-Click Focus", EditorStyles.boldLabel);
            var focusOnDoubleClick = serialized.FindProperty("focusOnDoubleClick");
            EditorGUILayout.PropertyField(focusOnDoubleClick, new GUIContent("Focus On Double-Click"));
            using (new EditorGUI.DisabledScope(!focusOnDoubleClick.boolValue)) {
                EditorGUILayout.PropertyField(serialized.FindProperty("autoToggle2DMode"), new GUIContent("Auto 2D Mode"));
            }
            if (focusOnDoubleClick.boolValue)
                EditorGUILayout.HelpBox(
                    serialized.FindProperty("autoToggle2DMode").boolValue
                        ? "Double-clicking a UI (RectTransform) object frames it with Scene View 2D mode on; a normal Transform frames with 2D mode off."
                        : "Double-clicking frames the object in the Scene View without changing 2D mode.",
                    MessageType.Info);

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
                EditorGUILayout.PropertyField(serialized.FindProperty("panelStatusChips"), new GUIContent("Show Status Chips"));
            }
            if (!HierarchyPanelHost.DockingSupported)
                EditorGUILayout.HelpBox(
                    "The docked footer is unavailable on this Unity version. Use " + HierarchyPanelWindow.MenuPath + " for the companion window.",
                    MessageType.Warning);

            HierarchyXSettingsExtras.DrawAll();

            Space();
            EditorGUILayout.LabelField("Rows", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("bestIcons"), new GUIContent("Best Component Icons"));
            EditorGUILayout.PropertyField(serialized.FindProperty("missingScriptIndicator"), new GUIContent("Missing Script Badge"));
            EditorGUILayout.PropertyField(serialized.FindProperty("rowActiveToggle"), new GUIContent("Active Toggle (Hover)"));
            EditorGUILayout.PropertyField(serialized.FindProperty("soloButtons"), new GUIContent("Vis/Pick Toggles (Hover)"));
            EditorGUILayout.PropertyField(serialized.FindProperty("dropCopyComponent"), new GUIContent("Ctrl-Drop Copies Component"));

            Space();
            EditorGUILayout.LabelField("Headers", EditorStyles.boldLabel);
            var headers = serialized.FindProperty("headersEnabled");
            EditorGUILayout.PropertyField(headers, new GUIContent("Enable Headers"));
            using (new EditorGUI.DisabledScope(!headers.boolValue)) {
                EditorGUILayout.PropertyField(serialized.FindProperty("headerPrefix"), new GUIContent("Name Prefix"));
                EditorGUILayout.PropertyField(serialized.FindProperty("headerColor"), new GUIContent("Bar Color"));
            }
            if (headers.boolValue)
                EditorGUILayout.HelpBox("Create one via GameObject ▸ Header. Header objects are tagged EditorOnly and stripped from builds.", MessageType.None);

            Space();
            EditorGUILayout.LabelField("Scene View", EditorStyles.boldLabel);
            var hover = serialized.FindProperty("hoverHighlight");
            EditorGUILayout.PropertyField(hover, new GUIContent("Highlight Hovered Row"));
            using (new EditorGUI.DisabledScope(!hover.boolValue))
                EditorGUILayout.PropertyField(serialized.FindProperty("hoverHighlightColor"), new GUIContent("Outline Color"));

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
                    UnityEngine.Object.DestroyImmediate(fresh);
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
