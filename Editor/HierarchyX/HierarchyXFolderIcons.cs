using UnityEditor;
using UnityEngine;
using ProjectWindowX;

namespace HierarchyX {
    /// <summary>
    /// Draws the folder icon assigned via <see cref="ProjectWindowXSettings.FolderIconRule"/>
    /// on hierarchy rows whose asset path (or nearest prefab ancestor path) matches a rule
    /// with <c>applyToHierarchy = true</c>. Gated by <see cref="HierarchyXSettings.folderIcons"/>.
    /// </summary>
    internal static class HierarchyXFolderIcons {

        internal static void Draw(Rect rect, GameObject go, HierarchyXSettings s) {
            if (!s.folderIcons || Event.current.type != EventType.Repaint)
                return;

            var path = ResolveAssetPath(go);
            if (string.IsNullOrEmpty(path))
                return;

            if (!FolderIcons.TryResolve(path, ProjectWindowXSettings.instance, out var icon, out var matchedFolder))
                return;

            var size = Mathf.Min(16f, rect.height);
            var iconRect = new Rect(rect.x, rect.yMin + (rect.height - size) * 0.5f, size, size);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            if (!string.IsNullOrEmpty(matchedFolder))
                GUI.Label(iconRect, new GUIContent(string.Empty, "Folder icon: " + matchedFolder));
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
    }
}
