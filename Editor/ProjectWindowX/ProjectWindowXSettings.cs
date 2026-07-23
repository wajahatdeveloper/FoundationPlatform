using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectWindowX {
    /// <summary>
    /// Project-wide ProjectWindowX settings, stored in ProjectSettings/ProjectWindowXSettings.asset
    /// (version-controlled). Edited via Project Settings ▸ ProjectWindowX.
    /// </summary>
    [FilePath("ProjectSettings/ProjectWindowXSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class ProjectWindowXSettings : ScriptableSingleton<ProjectWindowXSettings> {

        public bool enabled = true;

        // Zebra rows (list mode)
        public bool zebraRows;
        public Color oddRowColor = new Color(0f, 0f, 0f, 0.06f);

        // File extension labels (list mode)
        public bool extensionLabels = true;

        // Custom folder icons
        public bool folderIcons = true;

        [Serializable]
        public sealed class FolderIconRule {
            public string folderPath = "Assets/";   // project-relative folder path
            public bool applyToChildren;
            public string builtinIconName = "";     // EditorGUIUtility.IconContent name; wins when set
            public Texture2D customIcon;            // used when builtinIconName empty
        }
        public List<FolderIconRule> folderIconRules = new List<FolderIconRule>();

        // Hover "+" create-actions button
        public bool contextActions = true;

        // Authoring (GEC / Central Authoring consumers)
        public bool authoringContextMenus = true;
        public bool driftBadges = true;
        public Color driftBadgeColor = new Color(1f, 0.75f, 0.15f, 1f); // amber / warning
        public string driftBadgeTooltip = "Asset is outside its declared folder pattern";
        public Texture2D driftBadgeIcon;

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
            var fresh = CreateInstance<ProjectWindowXSettings>();
            EditorUtility.CopySerialized(fresh, this);
            DestroyImmediate(fresh);
            hideFlags = flags;
            Save(true);
        }
    }
}
