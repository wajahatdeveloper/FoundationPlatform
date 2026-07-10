using UnityEditor;
using UnityEngine;

namespace HierarchyX {
    /// <summary>
    /// Replaces the generic GameObject icon with the icon of the row's most
    /// distinctive component (cached in <see cref="HierarchyXRowCache"/>). Paints an
    /// opaque row-matched backing over the icon slot first so Unity's default icon is
    /// erased rather than showing through transparent/letterboxed areas of the custom icon.
    /// </summary>
    internal static class HierarchyXBestIcon {

        internal static void Draw(Rect rect, GameObject go, HierarchyXSettings s) {
            if (!s.bestIcons || Event.current.type != EventType.Repaint)
                return;

            var icon = HierarchyXRowCache.Get(go).icon;
            if (icon == null)
                return;

            var size = Mathf.Min(16f, rect.height);
            var iconRect = new Rect(rect.x, rect.yMin + (rect.height - size) * 0.5f, size, size);

            var bg = HierarchyX.ComposeRowBackground(go, rect, s); // opaque; erases Unity's default icon
            EditorGUI.DrawRect(iconRect, bg);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
        }
    }
}
