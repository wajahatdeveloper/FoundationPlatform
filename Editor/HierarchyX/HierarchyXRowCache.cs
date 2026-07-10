using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HierarchyX {
    /// <summary>
    /// Per-row derived data shared by the icon and missing-script passes.
    /// Filled once per GameObject, invalidated on hierarchy/undo/object changes —
    /// keeps GetComponents out of the per-repaint path.
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyXRowCache {

        internal struct RowInfo {
            public Texture icon;            // best component icon, null = keep Unity's default
            public bool hasMissingScript;
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
            if (cache.TryGetValue(id, out var info))
                return info;

            info = Build(go);
            cache[id] = info;
            return info;
        }

        private static RowInfo Build(GameObject go) {
            var info = new RowInfo();

            go.GetComponents(componentBuffer);
            for (var i = 0; i < componentBuffer.Count; i++) {
                if (componentBuffer[i] == null) {
                    info.hasMissingScript = true;
                    break;
                }
            }

            info.icon = ComputeBestIcon(go);
            return info;
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
