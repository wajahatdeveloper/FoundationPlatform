using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectWindowX;

namespace HierarchyX {
    /// <summary>
    /// Per-row derived data shared by the icon and missing-script passes.
    /// Filled once per GameObject, invalidated on hierarchy/undo/object changes and when
    /// ProjectWindowX folder-icon rules change — keeps GetComponents / AssetDatabase out of the per-repaint path.
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyXRowCache {

        internal struct RowInfo {
            public Texture icon;            // best component icon, null = keep Unity's default
            public Texture folderIcon;      // ProjectWindowX rule with applyToHierarchy, null if none
            public string matchedFolderPath;
            public string assetPath;
            public bool hasMissingScript;
            public int folderRulesVersion;
        }

        private static readonly Dictionary<int, RowInfo> cache = new Dictionary<int, RowInfo>();
        private static readonly List<Component> componentBuffer = new List<Component>(16);

        static HierarchyXRowCache() {
            EditorApplication.hierarchyChanged += Clear;
            Undo.undoRedoPerformed += Clear;
            ObjectChangeEvents.changesPublished += OnChangesPublished;
        }

        private static void OnChangesPublished(ref ObjectChangeEventStream stream) {
            Clear();
        }

        internal static void Clear() {
            cache.Clear();
        }

        internal static RowInfo Get(GameObject go) {
            var id = go.GetInstanceID();
            var rulesVersion = FolderIcons.RulesVersion;
            if (cache.TryGetValue(id, out var info) && info.folderRulesVersion == rulesVersion)
                return info;

            info = Build(go, rulesVersion);
            cache[id] = info;
            return info;
        }

        private static RowInfo Build(GameObject go, int rulesVersion) {
            var info = new RowInfo { folderRulesVersion = rulesVersion };

            go.GetComponents(componentBuffer);
            for (var i = 0; i < componentBuffer.Count; i++) {
                if (componentBuffer[i] == null) {
                    info.hasMissingScript = true;
                    break;
                }
            }

            info.icon = ComputeBestIcon(go);
            info.assetPath = ResolveAssetPath(go);
            if (!string.IsNullOrEmpty(info.assetPath)
                && FolderIcons.TryResolveForHierarchy(info.assetPath, ProjectWindowXSettings.instance, out var folderIcon, out var matched)) {
                info.folderIcon = folderIcon;
                info.matchedFolderPath = matched;
            }

            return info;
        }

        private static string ResolveAssetPath(GameObject go) {
            if (go == null)
                return null;

            var path = AssetDatabase.GetAssetPath(go);
            if (!string.IsNullOrEmpty(path))
                return path;

#if UNITY_2019_3_OR_NEWER
            path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            if (!string.IsNullOrEmpty(path))
                return path;
#endif

            var parent = go.transform.parent;
            while (parent != null) {
                var parentPath = AssetDatabase.GetAssetPath(parent.gameObject);
                if (!string.IsNullOrEmpty(parentPath))
                    return parentPath;
                parent = parent.parent;
            }

            return null;
        }

        // "Best" icon: the first component with a distinctive icon. Prefab roots keep their
        // prefab icon; generic script icons don't beat the default GameObject icon.
        private static Texture ComputeBestIcon(GameObject go) {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
                return null;

            for (var i = 0; i < componentBuffer.Count; i++) {
                var component = componentBuffer[i];
                if (component == null || component is Transform || component is CanvasRenderer)
                    continue;

                var icon = AssetPreview.GetMiniThumbnail(component);
                if (icon == null || IsGenericScriptIcon(icon))
                    continue;
                return icon;
            }
            return null;
        }

        private static bool IsGenericScriptIcon(Texture icon) {
            var name = icon.name;
            return name == "cs Script Icon" || name == "d_cs Script Icon";
        }
    }
}
