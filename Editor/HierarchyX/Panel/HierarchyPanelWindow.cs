using UnityEditor;
using UnityEngine;

namespace HierarchyX {

    /// <summary>
    /// Companion / fallback host for the same <see cref="IHierarchyPanelSection"/>s the docked footer
    /// renders. Always available from the menu; the automatic path is the docked footer
    /// (<see cref="HierarchyPanelHost"/>), and this window is what you dock manually next to the
    /// Hierarchy if the footer can't be injected on your Unity version.
    ///
    /// Height auto-fits the currently expanded accordion section (see
    /// <see cref="HierarchyPanelWidgets.DrawSections"/>), clamped to the same
    /// <see cref="HierarchyPanelHost.MinHeight"/>/<see cref="HierarchyPanelHost.MaxHeight"/> bounds as
    /// the docked footer — beyond that the section scrolls instead of growing the window further.
    /// </summary>
    public sealed class HierarchyPanelWindow : EditorWindow {

        public const string MenuPath = "Window/HierarchyX/Setup Panel";

        private Vector2 scroll;

        [MenuItem(MenuPath, false, 2200)]
        public static void Open() {
            var window = GetWindow<HierarchyPanelWindow>();
            window.titleContent = new GUIContent("Setup Panel");
            window.minSize = new Vector2(240f, HierarchyPanelHost.MinHeight);
            window.maxSize = new Vector2(4096f, HierarchyPanelHost.MaxHeight);
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
            var settings = HierarchyXSettings.Instance;

            EditorGUILayout.BeginVertical();
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("⚙", "Settings"), EditorStyles.toolbarButton, GUILayout.Width(22f)))
                SettingsService.OpenProjectSettings("Project/HierarchyX");
            GUILayout.EndHorizontal();

            var statusBar = settings.panelStatusChips ? StatusBarHeight : 0f;
            var sectionsBudget = HierarchyPanelHost.MaxHeight - HeaderHeight - statusBar;
            HierarchyPanelWidgets.DrawSections(ref scroll, sectionsBudget);

            if (settings.panelStatusChips)
                HierarchyPanelWidgets.DrawStatusBar();
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint) {
                var desired = Mathf.Clamp(GUILayoutUtility.GetLastRect().height,
                    HierarchyPanelHost.MinHeight, HierarchyPanelHost.MaxHeight);
                if (!Mathf.Approximately(desired, position.height)) {
                    var p = position;
                    p.height = desired;
                    position = p;
                }
            }
        }
    }
}
