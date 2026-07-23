#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.AetherInspector.Editor
{
    /// <summary>
    /// Project-wide toggles for the AetherInspector convenience features
    /// (object-field pencil/drag-out/selector, missing-script fixer, play-mode value saver,
    /// UnityEvent drop target). Stored in ProjectSettings/AetherInspectorXSettings.asset.
    /// </summary>
    [FilePath("ProjectSettings/AetherInspectorXSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class InspectorXSettings : ScriptableSingleton<InspectorXSettings>
    {
        [Tooltip("Pencil button next to populated object-reference fields opening a floating Property Editor.")]
        public bool objectFieldPencil = true;

        [Tooltip("Start a drag from a populated object-reference field (6px threshold keeps click-to-ping working).")]
        public bool objectFieldDragOut = true;

        [Tooltip("Right-click an object-reference field to pick from compatible scene objects and assets.")]
        public bool objectFieldSelector = true;

        [Tooltip("Replace the broken inspector of a missing script with a fixer that ranks candidate scripts by serialized-field match.")]
        public bool missingScriptFixer = true;

        [Tooltip("Enable the 'Save Values When Exiting Play Mode' component context-menu item.")]
        public bool saveComponentValuesInPlayMode = true;

        [Tooltip("Drop a GameObject or Component onto a UnityEvent field to add a persistent listener targeting it.")]
        public bool unityEventDrop = true;

        [Tooltip("Maximum nested depth for recursive [ShowInInspector] and [InlineProperty] drawers. Prevents stack overflow on circular references.")]
        public int maxNestedDepth = 10;

        public void SaveNow() => Save(true);

        public void ExportToJson(string path)
            => System.IO.File.WriteAllText(path, JsonUtility.ToJson(this, true));

        public void ImportFromJson(string path)
        {
            JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(path), this);
            Save(true);
        }

        public void ResetToDefaults()
        {
            var flags = hideFlags;
            var fresh = CreateInstance<InspectorXSettings>();
            EditorUtility.CopySerialized(fresh, this);
            DestroyImmediate(fresh);
            hideFlags = flags;
            Save(true);
        }
    }
}
#endif
