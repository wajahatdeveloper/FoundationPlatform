using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Draws EditorEnhancerX settings under Project Settings ▸ EditorEnhancerX.
    /// Project-scoped (stored in ProjectSettings/), with JSON export/import.
    /// </summary>
    public static class EditorEnhancerXSettingsProvider {

        private static SerializedObject serialized;

        [SettingsProvider]
        public static SettingsProvider Create() {
            return new SettingsProvider("Project/EditorEnhancerX", SettingsScope.Project) {
                label = "EditorEnhancerX",
                guiHandler = OnGUI,
                keywords = new HashSet<string> {
                    "shortcut", "autosave", "group", "ungroup", "rename", "rotate", "zoom",
                    "frame", "bounds", "pivot", "duplicate", "drop", "floor", "waila",
                    "maximize", "timescale", "stepper", "selection", "tool",
                    "nudge", "ui"
                }
            };
        }

        private static void Ensure() {
            var settings = EditorEnhancerXSettings.instance;
            if (serialized == null || serialized.targetObject != settings) {
                settings.hideFlags &= ~HideFlags.NotEditable;
                serialized = new SerializedObject(settings);
            }
        }

        private static void OnGUI(string searchContext) {
            Ensure();
            serialized.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("masterEnabled"), new GUIContent("Enabled"));

            Space();
            EditorGUILayout.LabelField("Autosave", EditorStyles.boldLabel);
            var autosave = serialized.FindProperty("autosave");
            var autosaveEnabled = autosave.FindPropertyRelative("enabled");
            EditorGUILayout.PropertyField(autosaveEnabled, new GUIContent("Enable Autosave"));
            using (new EditorGUI.DisabledScope(!autosaveEnabled.boolValue)) {
                EditorGUILayout.PropertyField(autosave.FindPropertyRelative("saveOnPlay"), new GUIContent("Save On Play"));
                var interval = autosave.FindPropertyRelative("intervalEnabled");
                EditorGUILayout.PropertyField(interval, new GUIContent("Interval Save"));
                using (new EditorGUI.DisabledScope(!interval.boolValue))
                    EditorGUILayout.PropertyField(autosave.FindPropertyRelative("intervalMinutes"), new GUIContent("Interval (Minutes)"));
                EditorGUILayout.PropertyField(autosave.FindPropertyRelative("saveAssets"), new GUIContent("Also Save Assets"));
            }
            if (autosaveEnabled.boolValue && autosave.FindPropertyRelative("saveOnPlay").boolValue
                && !autosave.FindPropertyRelative("intervalEnabled").boolValue)
                EditorGUILayout.HelpBox("Autosave fires only when entering Play Mode.", MessageType.Info);

            Space();
            EditorGUILayout.LabelField("Toolbar", EditorStyles.boldLabel);
            var timescale = serialized.FindProperty("timescale");
            EditorGUILayout.PropertyField(timescale.FindPropertyRelative("enabled"), new GUIContent("Timescale + Stepper"));
            using (new EditorGUI.DisabledScope(!timescale.FindPropertyRelative("enabled").boolValue)) {
                EditorGUILayout.PropertyField(timescale.FindPropertyRelative("sliderMax"), new GUIContent("Slider Max"));
                EditorGUILayout.PropertyField(timescale.FindPropertyRelative("stepperFramesPerSecond"), new GUIContent("Stepper FPS"));
            }

            Space();
            EditorGUILayout.LabelField("GameObject Tools", EditorStyles.boldLabel);
            Shortcut("groupKey", "Group Selection");
            Shortcut("ungroupKey", "Ungroup");
            var group = serialized.FindProperty("group");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(group.FindPropertyRelative("parentPlacement"), new GUIContent("Group Parent At"));
            EditorGUILayout.PropertyField(group.FindPropertyRelative("askForName"), new GUIContent("Ask For Name"));
            EditorGUILayout.PropertyField(group.FindPropertyRelative("defaultName"), new GUIContent("Default Name"));
            EditorGUI.indentLevel--;
            Shortcut("renameKey", "Rename / Mass Rename");
            Shortcut("addComponentKey", "Add Component");
            Shortcut("dropToFloorKey", "Drop To Floor");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serialized.FindProperty("dropToFloor").FindPropertyRelative("fallbackToZeroPlane"),
                new GUIContent("Fallback To Y=0 Plane"));
            EditorGUI.indentLevel--;
            Shortcut("rotateLeftKey", "Rotate -90° (Yaw)");
            Shortcut("rotateRightKey", "Rotate +90° (Yaw)");
            Shortcut("rotateUpKey", "Rotate -90° (Pitch)");
            Shortcut("rotateDownKey", "Rotate +90° (Pitch)");
            EditorGUILayout.PropertyField(serialized.FindProperty("pivotToolsEnabled"), new GUIContent("Pivot Tools (Tool Rail)"));
            EditorGUILayout.PropertyField(serialized.FindProperty("duplicateToolEnabled"), new GUIContent("Duplicate Tool (Tool Rail)"));

            Space();
            EditorGUILayout.LabelField("Scene View", EditorStyles.boldLabel);
            Shortcut("zoomInKey", "Fast Zoom In");
            Shortcut("zoomOutKey", "Fast Zoom Out");
            Shortcut("frameBoundsKey", "Frame Selected Bounds");
            EditorGUILayout.PropertyField(serialized.FindProperty("selectionBoundsEnabled"), new GUIContent("Selection Bounds Display"));
            EditorGUILayout.PropertyField(serialized.FindProperty("toolValuesEnabled"), new GUIContent("Tool Values Readout"));
            var waila = serialized.FindProperty("waila");
            var wailaEnabled = waila.FindPropertyRelative("enabled");
            EditorGUILayout.PropertyField(wailaEnabled, new GUIContent("Waila (Hover Tooltip)"));
            using (new EditorGUI.DisabledScope(!wailaEnabled.boolValue)) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(waila.FindPropertyRelative("requireModifier"), new GUIContent("Require Modifier"));
                EditorGUILayout.PropertyField(waila.FindPropertyRelative("modifiers"), new GUIContent("Modifier"));
                EditorGUI.indentLevel--;
            }
            Shortcut("smartSelectKey", "Smart Selection Cycle");

#if AETHERNEXUS_UIWIDGETS
            Space();
            EditorGUILayout.LabelField("UI Nudge", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("nudgeStep"), new GUIContent("Step (px)"));
            EditorGUILayout.PropertyField(serialized.FindProperty("nudgeStepCoarse"), new GUIContent("Coarse Step (px)"));
            Shortcut("nudgeLeftKey", "Nudge Left");
            Shortcut("nudgeRightKey", "Nudge Right");
            Shortcut("nudgeUpKey", "Nudge Up");
            Shortcut("nudgeDownKey", "Nudge Down");
            Shortcut("nudgeLeftCoarseKey", "Nudge Left (Coarse)");
            Shortcut("nudgeRightCoarseKey", "Nudge Right (Coarse)");
            Shortcut("nudgeUpCoarseKey", "Nudge Up (Coarse)");
            Shortcut("nudgeDownCoarseKey", "Nudge Down (Coarse)");
#endif

            Space();
            EditorGUILayout.LabelField("Windows", EditorStyles.boldLabel);
            Shortcut("maximizeKey", "Maximize Active Window");
            Shortcut("switchViewKey", "Switch Scene ↔ Game View");
            EditorGUILayout.PropertyField(serialized.FindProperty("viewSwitcher").FindPropertyRelative("switchToGameViewOnPlay"),
                new GUIContent("Game View On Play"));

            Space();
            EditorGUILayout.LabelField("Advanced (internal editor APIs)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("globalCaptureEnabled"), new GUIContent("Global Key Capture"));
            EditorGUILayout.PropertyField(serialized.FindProperty("dropToTabEnabled"), new GUIContent("Drag && Drop To Tab"));
            EditorGUILayout.HelpBox("These use internal editor APIs and self-disable when unavailable on a Unity upgrade.", MessageType.None);

            WarnOnDuplicateBindings();

            if (EditorGUI.EndChangeCheck()) {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorEnhancerXSettings.instance.SaveNow();
                TimescaleToolbar.Sync();
            }

            Space();
            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Export...", GUILayout.Width(90f))) {
                    var path = EditorUtility.SaveFilePanel("Export EditorEnhancerX Settings", "", "EditorEnhancerXSettings", "json");
                    if (!string.IsNullOrEmpty(path))
                        EditorEnhancerXSettings.instance.ExportToJson(path);
                }
                if (GUILayout.Button("Import...", GUILayout.Width(90f))) {
                    var path = EditorUtility.OpenFilePanel("Import EditorEnhancerX Settings", "", "json");
                    if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path)) {
                        EditorEnhancerXSettings.instance.ImportFromJson(path);
                        serialized = null;
                        GUIUtility.ExitGUI();
                    }
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset to Defaults", GUILayout.Width(140f))) {
                    EditorEnhancerXSettings.instance.ResetToDefaults();
                    serialized = null;
                    GUIUtility.ExitGUI();
                }
            }
        }

        private static void Shortcut(string propertyName, string label) {
            ShortcutBindingUI.Field(serialized.FindProperty(propertyName), new GUIContent(label));
        }

        private static void WarnOnDuplicateBindings() {
            var duplicates = KeyRouter.Registered()
                .Where(r => r.binding.enabled && r.binding.key != KeyCode.None)
                .GroupBy(r => (r.binding.key, r.binding.modifiers))
                .Where(g => g.Count() > 1)
                .ToList();
            foreach (var g in duplicates)
                EditorGUILayout.HelpBox(
                    $"Shortcut conflict: {string.Join(", ", g.Select(x => x.id))} all bound to {g.First().binding}.",
                    MessageType.Warning);
        }

        private static void Space() {
            EditorGUILayout.Space(8);
        }
    }
}
