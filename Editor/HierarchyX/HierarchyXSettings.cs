using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HierarchyX {

    /// <summary>How a per-layer row tint is painted across the row.</summary>
    public enum TintMode {
        Flat = 0,
        GradientRightToLeft = 1,
        GradientLeftToRight = 2,
    }

    /// <summary>The small text badges that can be drawn on the right of a row.</summary>
    public enum MiniLabelType {
        Tag = 0,
        Layer = 1,
        SortingLayer = 2,
    }

    /// <summary>Per-layer row color entry.</summary>
    [Serializable]
    public struct LayerColor {
        public int layer;
        public Color color;
        public TintMode mode;

        public LayerColor(int layer, Color color, TintMode mode = TintMode.GradientRightToLeft) {
            this.layer = layer;
            this.color = color;
            this.mode = mode;
        }
    }

    /// <summary>
    /// Project-scoped settings for HierarchyX. Persisted to ProjectSettings/HierarchyXSettings.asset
    /// (per-project, versionable) instead of user EditorPrefs.
    /// </summary>
    public sealed class HierarchyXSettings : ScriptableObject {

        public const string AssetPath = "ProjectSettings/HierarchyXSettings.asset";

        public const int MaxLineThickness = 6;

        public bool enabled = true;
        [Tooltip("Margin reserved on the right side, useful when other hierarchy extensions also draw there.")]
        public int rightMargin = 0;

        [Tooltip("Select GameObjects by dragging over their rows with the right mouse button.")]
        public bool enhancedSelection = true;

        public bool drawTree = true;
        [Range(0f, 1f)] public float treeOpacity = 0.8f;
        [Range(0f, 1f)] [Tooltip("Length of the extra stem drawn for leaf objects (no children).")]
        public float stemProportion = 0.5f;
        [Tooltip("Clicking a tree line selects the parent it connects to. May cost some performance.")]
        public bool selectOnTree = true;

        [Tooltip("Separator line thickness in pixels. 0 disables the separator.")]
        [Range(0, MaxLineThickness)] public int lineThickness = 1;
        public Color lineColor = new Color(0f, 0f, 0f, 0.2f);

        [Tooltip("Master switch for odd/even and per-layer row tints. Turn off to skip that draw pass entirely.")]
        public bool rowColors = true;
        public Color oddRowColor = new Color(0f, 0f, 0f, 0.1f);
        public Color evenRowColor = new Color(0f, 0f, 0f, 0f);
        public List<LayerColor> perLayerColors = new List<LayerColor>();

        [Tooltip("Let registered IHierarchyRowDecorators paint per-row tints and left-edge accent spines (e.g. character-rig membership). Turn off to skip that pass entirely.")]
        public bool rowDecorators = true;

        [Tooltip("Dock a collapsible setup/status panel to the bottom of the Hierarchy window. Sections are contributed by IHierarchyPanelSection plugins (e.g. Scene Setup).")]
        public bool panelEnabled = true;
        [Tooltip("Height of the docked panel in pixels when expanded.")]
        public float panelHeight = 200f;
        [Tooltip("Whether the docked panel is currently collapsed to its header bar.")]
        public bool panelCollapsed = false;
        [Tooltip("Section ids that are currently collapsed within the panel (state persistence).")]
        public List<string> panelCollapsedSections = new List<string>();

        public List<MiniLabelType> miniLabels = new List<MiniLabelType> { MiniLabelType.Layer, MiniLabelType.Tag };
        [Tooltip("Use an 8px font instead of 9px for narrow hierarchies.")]
        public bool smallerFont = true;
        [Tooltip("Hide the \"Untagged\" tag on the mini label.")]
        public bool hideDefaultTag = true;
        [Tooltip("Hide the \"Default\" layer on the mini label.")]
        public bool hideDefaultLayer = true;
        [Tooltip("Center a single mini label vertically when only tag OR only layer is shown.")]
        public bool centralizeWhenPossible = true;

        private static HierarchyXSettings instance;

        public static HierarchyXSettings Instance {
            get {
                if (instance)
                    return instance;
                return instance = LoadOrCreate();
            }
        }

        private static HierarchyXSettings LoadOrCreate() {
            HierarchyXSettings settings = null;

            try {
                var loaded = InternalEditorUtility.LoadSerializedFileAndForget(AssetPath);
                if (loaded != null && loaded.Length > 0)
                    settings = loaded[0] as HierarchyXSettings;
            } catch (Exception e) {
                Debug.LogWarning("HierarchyX: failed to load settings, using defaults.\n" + e);
            }

            if (!settings) {
                settings = CreateInstance<HierarchyXSettings>();
                settings.ApplySkinDefaults();
                settings.Save();
            }

            settings.hideFlags = HideFlags.HideAndDontSave & ~HideFlags.NotEditable;
            return settings;
        }

        /// <summary>Row-color / line defaults depend on the editor skin, applied on first creation.</summary>
        public void ApplySkinDefaults() {
            var pro = EditorGUIUtility.isProSkin;
            oddRowColor = pro ? new Color(0f, 0f, 0f, 0.1f) : new Color(1f, 1f, 1f, 0.2f);
            evenRowColor = new Color(0f, 0f, 0f, 0f);
            lineColor = new Color(0f, 0f, 0f, 0.2f);
            if (perLayerColors == null || perLayerColors.Count == 0)
                perLayerColors = new List<LayerColor> {
                    new LayerColor(5, new Color(0f, 0f, 1f, 0.3f)) // UI layer, subtle blue
                };
        }

        public void Save() {
            try {
                InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[] { this }, AssetPath, true);
            } catch (Exception e) {
                Debug.LogWarning("HierarchyX: failed to save settings.\n" + e);
            }
        }

        private void OnValidate() {
            lineThickness = Mathf.Clamp(lineThickness, 0, MaxLineThickness);
            if (rightMargin < 0) rightMargin = 0;
        }

        /// <summary>Serialize the current settings to a standalone JSON file.</summary>
        public bool ExportToJson(string path) {
            try {
                System.IO.File.WriteAllText(path, JsonUtility.ToJson(this, true));
                return true;
            } catch (Exception e) {
                Debug.LogError("HierarchyX: export failed.\n" + e);
                return false;
            }
        }

        /// <summary>Overwrite the current settings from a JSON file produced by ExportToJson.</summary>
        public bool ImportFromJson(string path) {
            try {
                var flags = hideFlags;
                JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(path), this);
                hideFlags = flags;
                OnValidate();
                Save();
                return true;
            } catch (Exception e) {
                Debug.LogError("HierarchyX: import failed.\n" + e);
                return false;
            }
        }
    }
}
