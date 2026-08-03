using UnityEditor;
using UnityEngine;

namespace ProjectWindowX {

    public sealed class ProjectPanelWindow : EditorWindow {

        public const string MenuPath = "Window/ProjectWindowX/Context Panel";

        private Vector2 scroll;

        [MenuItem(MenuPath, false, 2200)]
        public static void Open() {
            var window = GetWindow<ProjectPanelWindow>();
            window.titleContent = new GUIContent("Project Context");
            window.minSize = new Vector2(240f, ProjectPanelHost.MinHeight);
            window.maxSize = new Vector2(4096f, ProjectPanelHost.MaxHeight);
            window.Show();
        }

        private void OnEnable() {
            ProjectWindowXPanelRegistry.Changed += Repaint;
            EditorApplication.projectChanged += Repaint;
            Selection.selectionChanged += Repaint;
        }

        private void OnDisable() {
            ProjectWindowXPanelRegistry.Changed -= Repaint;
            EditorApplication.projectChanged -= Repaint;
            Selection.selectionChanged -= Repaint;
        }

        private const float HeaderHeight = 18f;
        private const float StatusBarHeight = 16f;

        private void OnGUI() {
            var settings = ProjectWindowXSettings.instance;

            EditorGUILayout.BeginVertical();
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("⚙", "Settings"), EditorStyles.toolbarButton, GUILayout.Width(22f)))
                SettingsService.OpenProjectSettings("Project/ProjectWindowX");
            GUILayout.EndHorizontal();

            var statusBar = settings.panelStatusChips ? StatusBarHeight : 0f;
            var sectionsBudget = ProjectPanelHost.MaxHeight - HeaderHeight - statusBar;
            ProjectPanelWidgets.DrawSections(ref scroll, sectionsBudget);

            if (settings.panelStatusChips)
                ProjectPanelWidgets.DrawStatusBar();
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint) {
                var desired = Mathf.Clamp(GUILayoutUtility.GetLastRect().height,
                    ProjectPanelHost.MinHeight, ProjectPanelHost.MaxHeight);
                if (!Mathf.Approximately(desired, position.height)) {
                    var p = position;
                    p.height = desired;
                    position = p;
                }
            }
        }
    }
}
