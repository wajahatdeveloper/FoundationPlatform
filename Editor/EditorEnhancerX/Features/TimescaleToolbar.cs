using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Main-toolbar Time.timeScale slider + a stepper dropdown (pause / step frames /
    /// scale presets), matching the project's existing [MainToolbarElement] buttons.
    /// Elements are gated by the timescale setting via <see cref="MainToolbarElement.displayed"/>.
    /// </summary>
    public static class TimescaleToolbar {

        private const string SliderPath = "EditorEnhancerX/Timescale";
        private const string StepperPath = "EditorEnhancerX/TimescaleStepper";

        private static MainToolbarElement sliderElement;
        private static MainToolbarElement stepperElement;

        [MainToolbarElement(SliderPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement TimescaleSlider() {
            var s = EditorEnhancerXSettings.instance;
            var content = new MainToolbarContent("Time", "Time.timeScale");
            var max = Mathf.Max(0.01f, s.timescale.sliderMax);
            sliderElement = new MainToolbarSlider(content, Time.timeScale, 0f, max, value => Time.timeScale = value) {
                displayed = s.masterEnabled && s.timescale.enabled
            };
            return sliderElement;
        }

        [MainToolbarElement(StepperPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement TimescaleStepper() {
            var s = EditorEnhancerXSettings.instance;
            var icon = EditorGUIUtility.IconContent("StepButton").image as Texture2D;
            var content = new MainToolbarContent(icon, "Timescale presets & frame stepper");
            stepperElement = new MainToolbarDropdown(content, OpenPopup) {
                displayed = s.masterEnabled && s.timescale.enabled
            };
            return stepperElement;
        }

        /// <summary>Push the current enabled/max settings onto the live toolbar elements.</summary>
        public static void Sync() {
            var s = EditorEnhancerXSettings.instance;
            var show = s.masterEnabled && s.timescale.enabled;
            if (sliderElement != null) sliderElement.displayed = show;
            if (stepperElement != null) stepperElement.displayed = show;
            MainToolbar.Refresh(SliderPath);
            MainToolbar.Refresh(StepperPath);
        }

        private static void OpenPopup(Rect activatorRect) {
            PopupWindow.Show(activatorRect, new StepperPopup());
        }

        private sealed class StepperPopup : PopupWindowContent {
            public override Vector2 GetWindowSize() => new Vector2(200f, 118f);

            public override void OnGUI(Rect rect) {
                var s = EditorEnhancerXSettings.instance;

                EditorGUILayout.LabelField("Time Scale", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope()) {
                    Preset(0f); Preset(0.25f); Preset(0.5f); Preset(1f); Preset(2f);
                }

                EditorGUILayout.Space(6f);
                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying)) {
                    using (new EditorGUILayout.HorizontalScope()) {
                        if (GUILayout.Button(EditorApplication.isPaused ? "Resume" : "Pause"))
                            EditorApplication.isPaused = !EditorApplication.isPaused;
                        if (GUILayout.Button("Step Frame")) {
                            EditorApplication.isPaused = true;
                            EditorApplication.Step();
                        }
                    }
                }

                EditorGUILayout.Space(4f);
                EditorGUI.BeginChangeCheck();
                var fps = EditorGUILayout.IntSlider("Held Step FPS", s.timescale.stepperFramesPerSecond, 1, 60);
                if (EditorGUI.EndChangeCheck()) {
                    s.timescale.stepperFramesPerSecond = fps;
                    s.SaveNow();
                }
            }

            private static void Preset(float value) {
                var label = value == 0f ? "0" : value.ToString("0.##") + "x";
                if (GUILayout.Button(label, EditorStyles.miniButton))
                    Time.timeScale = value;
            }
        }
    }
}
