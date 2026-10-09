using System;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Project-wide EditorEnhancerX settings, stored in ProjectSettings/EditorEnhancerXSettings.asset
    /// (version-controlled, shared by the team). Edited via Project Settings ▸ EditorEnhancerX.
    /// Every feature is toggleable; key bindings live in Unity's Shortcut Manager (Edit ▸ Shortcuts,
    /// "EditorEnhancerX/..." entries).
    /// </summary>
    [FilePath("ProjectSettings/EditorEnhancerXSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class EditorEnhancerXSettings : ScriptableSingleton<EditorEnhancerXSettings> {

        /// <summary>Master switch every shortcut handler checks.</summary>
        internal static bool Active => instance.masterEnabled;

        public bool masterEnabled = true;

        [Serializable]
        public sealed class AutosaveOptions {
            public bool enabled;
            public bool saveOnPlay = true;          // save dirty scenes when entering play mode
            public bool intervalEnabled;            // off = "only on play"
            public int intervalMinutes = 10;
            public bool saveAssets = true;          // also AssetDatabase.SaveAssets()
        }
        public AutosaveOptions autosave = new AutosaveOptions();

        [Serializable]
        public sealed class TimescaleOptions {
            public bool enabled = true;             // main-toolbar timescale slider + stepper
            public float sliderMax = 2f;
            public int stepperFramesPerSecond = 10; // step rate while stepper held
        }
        public TimescaleOptions timescale = new TimescaleOptions();

        [Serializable]
        public sealed class GroupOptions {
            public enum ParentPlacement { SelectionCenter, FirstObjectPivot, WorldOrigin }
            public ParentPlacement parentPlacement = ParentPlacement.SelectionCenter;
            public bool askForName = true;
            public string defaultName = "Group";
        }
        public GroupOptions group = new GroupOptions();

        [Serializable]
        public sealed class DropToFloorOptions {
            public bool fallbackToZeroPlane = true; // no collider hit → drop to y=0
        }
        public DropToFloorOptions dropToFloor = new DropToFloorOptions();

        [Serializable]
        public sealed class WailaOptions {
            public bool enabled;                    // hover tooltip in SceneView
            public bool requireModifier = true;
            public EventModifiers modifiers = EventModifiers.Control;
        }
        public WailaOptions waila = new WailaOptions();

        [Serializable]
        public sealed class ViewSwitcherOptions {
            public bool switchToGameViewOnPlay;
        }
        public ViewSwitcherOptions viewSwitcher = new ViewSwitcherOptions();

        // ---- Feature toggles (non-shortcut) ----
        public bool selectionBoundsEnabled;
        public bool toolValuesEnabled;
        public bool duplicateToolEnabled = true;
        public bool pivotToolsEnabled = true;

#if AETHERNEXUS_UIWIDGETS
        // ---- UI Nudge (UIWidgets present) ----
        public float nudgeStep = 1f;
        public float nudgeStepCoarse = 10f;
#endif

        public void SaveNow() {
            Save(true);
        }

        public void ResetToDefaults() {
            var flags = hideFlags;
            var fresh = CreateInstance<EditorEnhancerXSettings>();
            EditorUtility.CopySerialized(fresh, this);
            DestroyImmediate(fresh);
            hideFlags = flags;
            Save(true);
        }
    }
}
