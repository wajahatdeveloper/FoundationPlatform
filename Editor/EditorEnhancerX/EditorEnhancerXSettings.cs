using System;
using UnityEditor;
using UnityEngine;

namespace EditorEnhancerX {
    /// <summary>
    /// Project-wide EditorEnhancerX settings, stored in ProjectSettings/EditorEnhancerXSettings.asset
    /// (version-controlled, shared by the team). Edited via Project Settings ▸ EditorEnhancerX.
    /// Every feature is toggleable; shortcut features carry a rebindable <see cref="ShortcutBinding"/>.
    /// </summary>
    [FilePath("ProjectSettings/EditorEnhancerXSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class EditorEnhancerXSettings : ScriptableSingleton<EditorEnhancerXSettings> {

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
        public bool dropToTabEnabled;               // fragile (internal DockArea) — off by default
        public bool globalCaptureEnabled;           // Tier-2 key capture (internal API) — off by default

        // ---- Shortcuts ----
        public ShortcutBinding addComponentKey = new ShortcutBinding(false, KeyCode.A, EventModifiers.Control | EventModifiers.Shift);
        public ShortcutBinding groupKey = new ShortcutBinding(true, KeyCode.G, EventModifiers.Control);
        public ShortcutBinding ungroupKey = new ShortcutBinding(true, KeyCode.G, EventModifiers.Control | EventModifiers.Shift);
        public ShortcutBinding renameKey = new ShortcutBinding(true, KeyCode.F2, EventModifiers.None);
        public ShortcutBinding dropToFloorKey = new ShortcutBinding(true, KeyCode.End, EventModifiers.None);
        public ShortcutBinding rotateLeftKey = new ShortcutBinding(true, KeyCode.LeftArrow, EventModifiers.Control | EventModifiers.Shift);
        public ShortcutBinding rotateRightKey = new ShortcutBinding(true, KeyCode.RightArrow, EventModifiers.Control | EventModifiers.Shift);
        public ShortcutBinding rotateUpKey = new ShortcutBinding(true, KeyCode.UpArrow, EventModifiers.Control | EventModifiers.Shift);
        public ShortcutBinding rotateDownKey = new ShortcutBinding(true, KeyCode.DownArrow, EventModifiers.Control | EventModifiers.Shift);
        public ShortcutBinding zoomInKey = new ShortcutBinding(false, KeyCode.Equals, EventModifiers.Shift);
        public ShortcutBinding zoomOutKey = new ShortcutBinding(false, KeyCode.Minus, EventModifiers.Shift);
        public ShortcutBinding frameBoundsKey = new ShortcutBinding(false, KeyCode.F, EventModifiers.Shift);
        public ShortcutBinding smartSelectKey = new ShortcutBinding(false, KeyCode.Space, EventModifiers.Control);
        public ShortcutBinding maximizeKey = new ShortcutBinding(true, KeyCode.Space, EventModifiers.Shift);
        public ShortcutBinding switchViewKey = new ShortcutBinding(false, KeyCode.Tab, EventModifiers.Control);

        public void SaveNow() {
            Save(true);
        }

        public void ExportToJson(string path) {
            System.IO.File.WriteAllText(path, JsonUtility.ToJson(this, true));
        }

        public void ImportFromJson(string path) {
            JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(path), this);
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
