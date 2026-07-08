using UnityEditor;
using UnityEngine;

namespace HierarchyX {

    /// <summary>
    /// Companion / fallback host for the same <see cref="IHierarchyPanelSection"/>s the docked footer
    /// renders. Always available from the menu; the automatic path is the docked footer
    /// (<see cref="HierarchyPanelHost"/>), and this window is what you dock manually next to the
    /// Hierarchy if the footer can't be injected on your Unity version.
    /// </summary>
    public sealed class HierarchyPanelWindow : EditorWindow {

        public const string MenuPath = "Window/HierarchyX/Setup Panel";

        private Vector2 scroll;

        [MenuItem(MenuPath, false, 2200)]
        public static void Open() {
            var window = GetWindow<HierarchyPanelWindow>();
            window.titleContent = new GUIContent("Setup Panel");
            window.minSize = new Vector2(240f, 120f);
            window.Show();
        }

        private void OnEnable() {
            HierarchyXPanelRegistry.Changed += Repaint;
            EditorApplication.hierarchyChanged += Repaint;
            Selection.selectionChanged += Repaint;
        }

        private void OnDisable() {
            HierarchyXPanelRegistry.Changed -= Repaint;
            EditorApplication.hierarchyChanged -= Repaint;
            Selection.selectionChanged -= Repaint;
        }

        private const float HeaderHeight = 20f;
        private const float StatusBarHeight = 20f;

        private void OnGUI() {
            var w = position.width;
            var h = position.height;

            GUILayout.BeginArea(new Rect(0f, 0f, w, HeaderHeight));
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            HierarchyPanelWidgets.DrawToolbarActions();
            if (GUILayout.Button(new GUIContent("⚙", "Settings"), EditorStyles.toolbarButton, GUILayout.Width(22f)))
                SettingsService.OpenProjectSettings("Project/HierarchyX");
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            var bodyHeight = h - HeaderHeight - StatusBarHeight;
            if (bodyHeight > 1f) {
                GUILayout.BeginArea(new Rect(0f, HeaderHeight, w, bodyHeight));
                HierarchyPanelWidgets.DrawSections(ref scroll);
                GUILayout.EndArea();
            }

            GUILayout.BeginArea(new Rect(0f, h - StatusBarHeight, w, StatusBarHeight));
            HierarchyPanelWidgets.DrawStatusBar();
            GUILayout.EndArea();
        }
    }
}
