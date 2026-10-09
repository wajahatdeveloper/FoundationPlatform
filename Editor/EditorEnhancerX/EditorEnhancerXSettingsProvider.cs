using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Draws EditorEnhancerX settings under Project Settings ▸ EditorEnhancerX.
    /// Project-scoped (stored in ProjectSettings/). Key bindings are not here: they are
    /// "EditorEnhancerX/..." entries in Unity's Shortcut Manager.
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
                    "timescale", "stepper", "selection", "tool", "nudge", "ui"
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
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField("Key bindings live in Edit ▸ Shortcuts (EditorEnhancerX).", EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("Open Shortcut Manager", GUILayout.Width(170f)))
                    EditorApplication.ExecuteMenuItem("Edit/Shortcuts...");
            }

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
            var group = serialized.FindProperty("group");
            EditorGUILayout.PropertyField(group.FindPropertyRelative("parentPlacement"), new GUIContent("Group Parent At"));
            EditorGUILayout.PropertyField(group.FindPropertyRelative("askForName"), new GUIContent("Group Asks For Name"));
            EditorGUILayout.PropertyField(group.FindPropertyRelative("defaultName"), new GUIContent("Group Default Name"));
            EditorGUILayout.PropertyField(serialized.FindProperty("dropToFloor").FindPropertyRelative("fallbackToZeroPlane"),
                new GUIContent("Drop To Floor: Fallback To Y=0 Plane"));
            EditorGUILayout.PropertyField(serialized.FindProperty("pivotToolsEnabled"), new GUIContent("Pivot Tools (Tool Rail)"));
            EditorGUILayout.PropertyField(serialized.FindProperty("duplicateToolEnabled"), new GUIContent("Duplicate Tool (Tool Rail)"));

            Space();
            EditorGUILayout.LabelField("Scene View", EditorStyles.boldLabel);
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

#if AETHERNEXUS_UIWIDGETS
            Space();
            EditorGUILayout.LabelField("UI Nudge", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("nudgeStep"), new GUIContent("Step (px)"));
            EditorGUILayout.PropertyField(serialized.FindProperty("nudgeStepCoarse"), new GUIContent("Coarse Step (px)"));
#endif

            Space();
            EditorGUILayout.LabelField("Windows", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty("viewSwitcher").FindPropertyRelative("switchToGameViewOnPlay"),
                new GUIContent("Game View On Play"));

            if (EditorGUI.EndChangeCheck()) {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorEnhancerXSettings.instance.SaveNow();
                TimescaleToolbar.Sync();
            }

            Space();
            using (new EditorGUILayout.HorizontalScope()) {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset to Defaults", GUILayout.Width(140f))) {
                    EditorEnhancerXSettings.instance.ResetToDefaults();
                    serialized = null;
                    GUIUtility.ExitGUI();
                }
            }
        }

        private static void Space() {
            EditorGUILayout.Space(8);
        }
    }
}
