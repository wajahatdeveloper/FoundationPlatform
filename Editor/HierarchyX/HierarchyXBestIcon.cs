using UnityEditor;
using UnityEngine;
using ProjectWindowX;

namespace HierarchyX {
    /// <summary>
    /// Fills Unity's Hierarchy row icon slot with at most one replacement texture:
    /// folder rule (<c>applyToHierarchy</c>) wins over best-component icon; otherwise leave Unity's stock icon.
    /// Paints an opaque row-matched backing before drawing so transparent icon edges do not bleed.
    /// </summary>
    internal static class HierarchyXBestIcon {

        internal static void Draw(Rect rect, GameObject go, HierarchyXSettings s) {
            if (Event.current.type != EventType.Repaint)
                return;
            if (!s.bestIcons && !s.folderIcons)
                return;

            var info = HierarchyXRowCache.Get(go);
            Texture icon = null;
            string tooltip = null;

            if (s.folderIcons && info.folderIcon != null) {
                icon = info.folderIcon;
                if (!string.IsNullOrEmpty(info.matchedFolderPath))
                    tooltip = "Folder icon: " + info.matchedFolderPath;
            } else if (s.bestIcons && info.icon != null) {
                icon = info.icon;
            }

            if (icon == null)
                return;

            var size = Mathf.Min(Mathf.Max(8f, s.rowIconSize), rect.height);
            var iconRect = new Rect(
                rect.x + s.rowIconOffsetX,
                rect.yMin + (rect.height - size) * 0.5f,
                size,
                size);

            var bg = HierarchyX.ComposeRowBackground(go, rect, s);
            EditorGUI.DrawRect(iconRect, bg);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            if (!string.IsNullOrEmpty(tooltip))
                GUI.Label(iconRect, new GUIContent(string.Empty, tooltip));
        }
    }
}
