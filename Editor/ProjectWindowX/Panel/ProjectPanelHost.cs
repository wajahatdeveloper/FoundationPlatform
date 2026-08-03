using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectWindowX {

    /// <summary>
    /// Docks a collapsible context panel to the bottom of Unity's Project browser, above the
    /// built-in Project status bar. Full window width (one- and two-column layouts). Sections via
    /// <see cref="IProjectPanelSection"/>.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectPanelHost {

        internal const string FooterName = "ProjectWindowXPanelFooter";
        internal const float MinHeight = 38f;
        internal const float MaxHeight = 320f;
        internal const float CollapsedHeight = 18f;
        private const double PollInterval = 0.4;
        private const float FallbackUnityStatusBarHeight = 18f;

        private static readonly Type ProjectBrowserType =
            typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");

        private static double nextPoll;
        private static bool warnedUnsupported;
        private static Vector2 scroll;
        private static float measuredHeight = MinHeight;

        private const float HeaderHeight = 18f;
        private const float StatusBarHeight = 16f;

        static ProjectPanelHost() {
            EditorApplication.update += Poll;
            EditorApplication.projectChanged += RepaintAll;
            Selection.selectionChanged += RepaintAll;
            EditorApplication.playModeStateChanged += _ => RepaintAll();
            ProjectWindowXPanelRegistry.Changed += RepaintAll;
        }

        public static bool DockingSupported => ProjectBrowserType != null;

        public static void RevealSection(string sectionId) {
            if (string.IsNullOrEmpty(sectionId))
                return;

            var settings = ProjectWindowXSettings.instance;
            var changed = false;
            if (!settings.panelEnabled) { settings.panelEnabled = true; changed = true; }
            if (settings.panelCollapsed) { settings.panelCollapsed = false; changed = true; }
            if (!string.Equals(settings.panelExpandedSectionId, sectionId, System.StringComparison.Ordinal)) {
                settings.panelExpandedSectionId = sectionId;
                changed = true;
            }
            if (changed) settings.SaveNow();

            ProjectPanelWidgets.PendingRevealId = sectionId;

            if (DockingSupported) {
                var windows = Resources.FindObjectsOfTypeAll(ProjectBrowserType);
                var target = windows.Length > 0 ? windows[0] as EditorWindow : null;
                if (target != null) {
                    target.Focus();
                    Sync(target);
                }
                RepaintAll();
            } else {
                ProjectPanelWindow.Open();
            }
        }

        private static void Poll() {
            if (EditorApplication.timeSinceStartup < nextPoll)
                return;
            nextPoll = EditorApplication.timeSinceStartup + PollInterval;

            if (ProjectBrowserType == null) {
                if (!warnedUnsupported) {
                    warnedUnsupported = true;
                    Debug.LogWarning(
                        "ProjectWindowX: could not resolve ProjectBrowser; the docked panel is unavailable on this Unity version. " +
                        "Use " + ProjectPanelWindow.MenuPath + " for the companion window instead.");
                }
                return;
            }

            var windows = Resources.FindObjectsOfTypeAll(ProjectBrowserType);
            for (var i = 0; i < windows.Length; i++)
                Sync(windows[i] as EditorWindow);
        }

        private static void Sync(EditorWindow window) {
            if (window == null)
                return;
            var root = window.rootVisualElement;
            if (root == null)
                return;

            var footer = root.Q<IMGUIContainer>(FooterName);
            var settings = ProjectWindowXSettings.instance;

            if (!settings.panelEnabled) {
                if (footer != null) {
                    ClearReservation(root, footer);
                    footer.RemoveFromHierarchy();
                }
                return;
            }

            var unityStatusHeight = MeasureUnityStatusBarHeight(root, footer);

            if (footer == null) {
                footer = new IMGUIContainer { name = FooterName };
                footer.style.position = Position.Absolute;
                footer.style.left = 0f;
                footer.style.right = 0f;
                footer.style.borderTopWidth = 1f;
                footer.style.borderTopColor = new Color(0f, 0f, 0f, 0.5f);
                footer.style.backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.22f, 0.22f, 0.22f)
                    : new Color(0.78f, 0.78f, 0.78f);
                var captured = footer;
                footer.onGUIHandler = () => DrawFooter(captured);
                root.Add(footer);
            }

            footer.style.bottom = unityStatusHeight;
            var h = settings.panelCollapsed ? CollapsedHeight : measuredHeight;
            footer.style.height = h;
            ReserveSpace(root, footer, h + unityStatusHeight);
        }

        /// <summary>
        /// Keep Unity's Project status strip visible: sit our footer above the bottom-most small
        /// bar (or a conservative fallback), never covering it.
        /// </summary>
        private static float MeasureUnityStatusBarHeight(VisualElement root, VisualElement ourFooter) {
            float best = 0f;
            foreach (var child in root.Children()) {
                if (child == ourFooter)
                    continue;
                var name = child.name ?? string.Empty;
                if (name.IndexOf("status", StringComparison.OrdinalIgnoreCase) >= 0) {
                    var h = child.resolvedStyle.height;
                    if (h > 0f && h < 40f)
                        return h;
                }
                var layoutH = child.layout.height;
                if (layoutH > 0f && layoutH <= 24f) {
                    var bottom = child.layout.yMax;
                    var nearBottom = Mathf.Abs(bottom - root.layout.height) < 2f;
                    var absoluteBottom = child.resolvedStyle.position == Position.Absolute
                                        && child.resolvedStyle.bottom < 1f;
                    if (nearBottom || absoluteBottom) {
                        best = Mathf.Max(best, layoutH);
                    }
                }
            }
            return best > 0f ? best : FallbackUnityStatusBarHeight;
        }

        private static VisualElement FindContent(VisualElement root, VisualElement footer) {
            VisualElement content = null;
            var best = -1f;
            foreach (var child in root.Children()) {
                if (child == footer)
                    continue;
                var name = child.name ?? string.Empty;
                if (name.IndexOf("status", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                var ch = child.layout.height;
                if (ch > best) {
                    best = ch;
                    content = child;
                }
            }
            return content;
        }

        private static void ReserveSpace(VisualElement root, VisualElement footer, float totalBottom) {
            var content = FindContent(root, footer);
            if (content == null)
                return;
            if (content.resolvedStyle.position == Position.Absolute)
                content.style.bottom = totalBottom;
            else
                content.style.marginBottom = totalBottom;
        }

        private static void ClearReservation(VisualElement root, VisualElement footer) {
            var content = FindContent(root, footer);
            if (content == null)
                return;
            content.style.bottom = StyleKeyword.Null;
            content.style.marginBottom = StyleKeyword.Null;
        }

        private static void DrawFooter(IMGUIContainer footer) {
            var settings = ProjectWindowXSettings.instance;
            var w = footer.contentRect.width;
            var collapsed = settings.panelCollapsed;

            if (collapsed) {
                GUILayout.BeginArea(new Rect(0f, 0f, w, CollapsedHeight));
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                if (GUILayout.Button("▸", EditorStyles.toolbarButton, GUILayout.Width(22f)))
                    SetCollapsed(footer, settings, false);
                if (settings.panelStatusChips)
                    ProjectPanelWidgets.DrawStatusChips();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
                return;
            }

            EditorGUILayout.BeginVertical();
            DrawHeaderBar(settings, footer);

            var statusBar = settings.panelStatusChips ? StatusBarHeight : 0f;
            var sectionsBudget = MaxHeight - HeaderHeight - statusBar;
            ProjectPanelWidgets.DrawSections(ref scroll, sectionsBudget);

            if (settings.panelStatusChips)
                ProjectPanelWidgets.DrawStatusBar();
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint) {
                var desired = Mathf.Clamp(GUILayoutUtility.GetLastRect().height, MinHeight, MaxHeight);
                if (!Mathf.Approximately(desired, measuredHeight)) {
                    measuredHeight = desired;
                    footer.style.height = desired;
                    if (footer.parent != null) {
                        var unityStatus = MeasureUnityStatusBarHeight(footer.parent, footer);
                        footer.style.bottom = unityStatus;
                        ReserveSpace(footer.parent, footer, desired + unityStatus);
                    }
                    footer.MarkDirtyRepaint();
                }
            }
        }

        private static void SetCollapsed(IMGUIContainer footer, ProjectWindowXSettings settings, bool collapsed) {
            settings.panelCollapsed = collapsed;
            settings.SaveNow();
            var h = collapsed ? CollapsedHeight : measuredHeight;
            footer.style.height = h;
            if (footer.parent != null) {
                var unityStatus = MeasureUnityStatusBarHeight(footer.parent, footer);
                footer.style.bottom = unityStatus;
                ReserveSpace(footer.parent, footer, h + unityStatus);
            }
            footer.MarkDirtyRepaint();
        }

        private static void DrawHeaderBar(ProjectWindowXSettings settings, IMGUIContainer footer) {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(new GUIContent("▾", "Collapse"), EditorStyles.toolbarButton, GUILayout.Width(22f)))
                SetCollapsed(footer, settings, true);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("⛶", "Open as window"), EditorStyles.toolbarButton, GUILayout.Width(22f)))
                ProjectPanelWindow.Open();
            if (GUILayout.Button(new GUIContent("⚙", "Settings"), EditorStyles.toolbarButton, GUILayout.Width(22f)))
                SettingsService.OpenProjectSettings("Project/ProjectWindowX");

            GUILayout.EndHorizontal();
        }

        private static void RepaintAll() {
            if (ProjectBrowserType == null)
                return;
            var windows = Resources.FindObjectsOfTypeAll(ProjectBrowserType);
            for (var i = 0; i < windows.Length; i++) {
                var window = windows[i] as EditorWindow;
                var footer = window != null ? window.rootVisualElement?.Q<IMGUIContainer>(FooterName) : null;
                footer?.MarkDirtyRepaint();
            }
        }
    }
}
