using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace EditorEnhancerX {
    /// <summary>
    /// Scene View overlay with a Time.timeScale slider + presets and a frame stepper
    /// (pause + EditorApplication.Step at a configurable rate while held).
    /// Implemented as an Overlay (public API) rather than main-toolbar injection.
    /// </summary>
    [Overlay(typeof(SceneView), "Timescale", false)]
    internal sealed class TimescaleOverlay : IMGUIOverlay {

        private static bool stepping;
        private static double lastStepTime;

        public override void OnGUI() {
            var s = EditorEnhancerXSettings.instance;
            if (!s.masterEnabled || !s.timescale.enabled) {
                EditorGUILayout.LabelField("Disabled in Project Settings ▸ EditorEnhancerX", EditorStyles.miniLabel);
                return;
            }

            using (new EditorGUILayout.HorizontalScope(GUILayout.MinWidth(260f))) {
                EditorGUILayout.LabelField("Time", GUILayout.Width(32f));
                var newScale = EditorGUILayout.Slider(Time.timeScale, 0f, Mathf.Max(0.01f, s.timescale.sliderMax));
                if (!Mathf.Approximately(newScale, Time.timeScale))
                    Time.timeScale = newScale;
            }

            using (new EditorGUILayout.HorizontalScope()) {
                DrawPreset(0f);
                DrawPreset(0.25f);
                DrawPreset(0.5f);
                DrawPreset(1f);
                DrawPreset(2f);

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying)) {
                    var pauseIcon = EditorGUIUtility.IconContent(EditorApplication.isPaused ? "PlayButton" : "PauseButton");
                    if (GUILayout.Button(pauseIcon, EditorStyles.miniButton, GUILayout.Width(28f)))
                        EditorApplication.isPaused = !EditorApplication.isPaused;

                    // Hold to step frames at the configured rate.
                    var stepIcon = EditorGUIUtility.IconContent("StepButton");
                    var pressed = GUILayout.RepeatButton(stepIcon, EditorStyles.miniButton, GUILayout.Width(28f));
                    HandleStepper(pressed, s);
                }
            }
        }

        private static void DrawPreset(float value) {
            var label = value == 0f ? "0" : value.ToString("0.##") + "x";
            if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.Width(34f)))
                Time.timeScale = value;
        }

        private static void HandleStepper(bool pressed, EditorEnhancerXSettings s) {
            if (pressed && !stepping) {
                stepping = true;
                lastStepTime = 0d;
                EditorApplication.isPaused = true;
                EditorApplication.update += StepUpdate;
            } else if (!pressed && stepping && Event.current.type == EventType.Repaint) {
                stepping = false;
                EditorApplication.update -= StepUpdate;
            }
        }

        private static void StepUpdate() {
            var s = EditorEnhancerXSettings.instance;
            var interval = 1.0 / Mathf.Max(1, s.timescale.stepperFramesPerSecond);
            if (EditorApplication.timeSinceStartup - lastStepTime < interval)
                return;
            lastStepTime = EditorApplication.timeSinceStartup;
            EditorApplication.Step();
        }
    }
}
