using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace HierarchyX {

    /// <summary>
    /// Docks a collapsible, self-sizing setup panel to the bottom of Unity's built-in Hierarchy
    /// window. Sections are contributed by plugins via <see cref="IHierarchyPanelSection"/> and
    /// rendered accordion-style (at most one expanded at a time) by
    /// <see cref="HierarchyPanelWidgets.DrawSections"/>; the footer's height auto-fits whichever
    /// section is currently open, clamped to <see cref="MinHeight"/>/<see cref="MaxHeight"/> (beyond
    /// which the section scrolls instead of growing further).
    ///
    /// Mechanism: reflect the internal <c>UnityEditor.SceneHierarchyWindow</c> type, then append a
    /// single <see cref="IMGUIContainer"/> footer to its public <c>rootVisualElement</c>. This is the
    /// ONE place in HierarchyX that touches internal editor structure — deliberately quarantined so
    /// the rest of the package stays on public APIs. If the type can't be resolved (unsupported
    /// Unity), the docked footer silently no-ops and the same sections remain reachable through the
    /// fallback <see cref="HierarchyPanelWindow"/> (menu + always available).
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyPanelHost {

        internal const string FooterName = "HierarchyXPanelFooter";
        internal const float MinHeight = 80f;
        internal const float MaxHeight = 600f;
        internal const float CollapsedHeight = 20f;
        private const double PollInterval = 0.4;

        private static readonly Type SceneHierarchyWindowType =
            typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");

        private static double nextPoll;
        private static bool warnedUnsupported;
        private static Vector2 scroll;

        /// <summary>
        /// Last-applied expanded footer height — the auto-fit result, not a user preference. Seeded
        /// with <see cref="MinHeight"/> until the first Repaint measures real content.
        /// </summary>
        private static float measuredHeight = MinHeight;

        static HierarchyPanelHost() {
            EditorApplication.update += Poll;
            EditorApplication.hierarchyChanged += RepaintAll;
            Selection.selectionChanged += RepaintAll;
            EditorApplication.playModeStateChanged += _ => RepaintAll();
            HierarchyXPanelRegistry.Changed += RepaintAll;
        }

        /// <summary>True when the docked footer can be injected on this Unity version.</summary>
        public static bool DockingSupported => SceneHierarchyWindowType != null;

        /// <summary>
        /// Reveal a panel section by id: enable the panel, un-collapse it, expand the target section,
        /// bring the hosting surface forward, and request a scroll so the section is in view. This is the
        /// hand-off entry point for out-of-context tools (e.g. the Central Authoring Window) that want to
        /// send the user to the canonical in-context home of a feature instead of duplicating it.
        /// When the docked footer is unsupported, falls back to the companion window.
        /// </summary>
        public static void RevealSection(string sectionId) {
            if (string.IsNullOrEmpty(sectionId))
                return;

            var settings = HierarchyXSettings.Instance;
            var changed = false;
            if (!settings.panelEnabled) { settings.panelEnabled = true; changed = true; }
            if (settings.panelCollapsed) { settings.panelCollapsed = false; changed = true; }
            if (settings.panelCollapsedSections.Remove(sectionId)) changed = true;
            if (changed) settings.Save();

            HierarchyPanelWidgets.PendingRevealId = sectionId;

            if (DockingSupported) {
                var windows = Resources.FindObjectsOfTypeAll(SceneHierarchyWindowType);
                var target = windows.Length > 0 ? windows[0] as EditorWindow : null;
                if (target != null) {
                    target.Focus();
                    Sync(target); // re-apply footer height now the panel is enabled/expanded
                }
                RepaintAll();
            } else {
                HierarchyPanelWindow.Open();
            }
        }

        private static void Poll() {
            if (EditorApplication.timeSinceStartup < nextPoll)
                return;
            nextPoll = EditorApplication.timeSinceStartup + PollInterval;

            if (SceneHierarchyWindowType == null) {
                if (!warnedUnsupported) {
                    warnedUnsupported = true;
                    Debug.LogWarning(
                        "HierarchyX: could not resolve SceneHierarchyWindow; the docked setup panel is unavailable on this Unity version. " +
                        "Use " + HierarchyPanelWindow.MenuPath + " for the companion window instead.");
                }
                return;
            }

            var windows = Resources.FindObjectsOfTypeAll(SceneHierarchyWindowType);
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
            var settings = HierarchyXSettings.Instance;

            if (!settings.panelEnabled) {
                if (footer != null) {
                    ClearReservation(root, footer);
                    footer.RemoveFromHierarchy();
                }
                return;
            }

            if (footer == null) {
                // The hierarchy's content child fills the window (absolute), so a flex sibling won't
                // reserve space — pin the footer absolute to the bottom and carve room out of the
                // content child (ReserveSpace) instead. A solid background stops the tree bleeding
                // through where the footer's own IMGUI doesn't paint.
                footer = new IMGUIContainer { name = FooterName };
                footer.style.position = Position.Absolute;
                footer.style.left = 0f;
                footer.style.right = 0f;
                footer.style.bottom = 0f;
                footer.style.borderTopWidth = 1f;
                footer.style.borderTopColor = new Color(0f, 0f, 0f, 0.5f);
                footer.style.backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.22f, 0.22f, 0.22f)
                    : new Color(0.78f, 0.78f, 0.78f);
                var captured = footer;
                footer.onGUIHandler = () => DrawFooter(captured);
                root.Add(footer);
            }

            var h = settings.panelCollapsed ? CollapsedHeight : measuredHeight;
            footer.style.height = h;
            ReserveSpace(root, footer, h);
        }

        /// <summary>
        /// The Hierarchy's main content element (the biggest non-footer child). We shrink it to leave
        /// room for the docked footer: absolute-positioned content moves via <c>bottom</c>, flow
        /// content via <c>marginBottom</c>. Re-applied every poll so it self-heals after re-layout.
        /// </summary>
        private static VisualElement FindContent(VisualElement root, VisualElement footer) {
            VisualElement content = null;
            var best = -1f;
            foreach (var child in root.Children()) {
                if (child == footer)
                    continue;
                var ch = child.layout.height;
                if (ch > best) {
                    best = ch;
                    content = child;
                }
            }
            return content;
        }

        private static void ReserveSpace(VisualElement root, VisualElement footer, float h) {
            var content = FindContent(root, footer);
            if (content == null)
                return;
            if (content.resolvedStyle.position == Position.Absolute)
                content.style.bottom = h;
            else
                content.style.marginBottom = h;
        }

        private static void ClearReservation(VisualElement root, VisualElement footer) {
            var content = FindContent(root, footer);
            if (content == null)
                return;
            content.style.bottom = StyleKeyword.Null;
            content.style.marginBottom = StyleKeyword.Null;
        }

        private const float HeaderHeight = 20f;
        private const float StatusBarHeight = 20f;

        private static void DrawFooter(IMGUIContainer footer) {
            var settings = HierarchyXSettings.Instance;
            var w = footer.contentRect.width;
            var collapsed = settings.panelCollapsed;

            if (collapsed) {
                // Single glanceable strip: expand arrow + chips.
                GUILayout.BeginArea(new Rect(0f, 0f, w, CollapsedHeight));
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                if (GUILayout.Button("▸", EditorStyles.toolbarButton, GUILayout.Width(22f)))
                    SetCollapsed(footer, settings, false);
                if (settings.panelStatusChips)
                    HierarchyPanelWidgets.DrawStatusChips();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
                return;
            }

            // No fixed-height BeginArea here: the footer's height is derived FROM this content
            // (measured below), not the other way around, so everything below stacks naturally.
            EditorGUILayout.BeginVertical();
            DrawHeaderBar(settings, footer);

            var statusBar = settings.panelStatusChips ? StatusBarHeight : 0f;
            var sectionsBudget = MaxHeight - HeaderHeight - statusBar;
            HierarchyPanelWidgets.DrawSections(ref scroll, sectionsBudget);

            if (settings.panelStatusChips)
                HierarchyPanelWidgets.DrawStatusBar();
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint) {
                var desired = Mathf.Clamp(GUILayoutUtility.GetLastRect().height, MinHeight, MaxHeight);
                if (!Mathf.Approximately(desired, measuredHeight)) {
                    measuredHeight = desired;
                    footer.style.height = desired;
                    if (footer.parent != null)
                        ReserveSpace(footer.parent, footer, desired);
                    footer.MarkDirtyRepaint();
                }
            }
        }

        private static void SetCollapsed(IMGUIContainer footer, HierarchyXSettings settings, bool collapsed) {
            settings.panelCollapsed = collapsed;
            settings.Save();
            var h = collapsed ? CollapsedHeight : measuredHeight;
            footer.style.height = h;
            if (footer.parent != null)
                ReserveSpace(footer.parent, footer, h);
            footer.MarkDirtyRepaint();
        }

        private static void DrawHeaderBar(HierarchyXSettings settings, IMGUIContainer footer) {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(new GUIContent("▾", "Collapse"), EditorStyles.toolbarButton, GUILayout.Width(22f)))
                SetCollapsed(footer, settings, true);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("⛶", "Open as window"), EditorStyles.toolbarButton, GUILayout.Width(22f)))
                HierarchyPanelWindow.Open();
            if (GUILayout.Button(new GUIContent("⚙", "Settings"), EditorStyles.toolbarButton, GUILayout.Width(22f)))
                SettingsService.OpenProjectSettings("Project/HierarchyX");

            GUILayout.EndHorizontal();
        }

        private static void RepaintAll() {
            if (SceneHierarchyWindowType == null)
                return;
            var windows = Resources.FindObjectsOfTypeAll(SceneHierarchyWindowType);
            for (var i = 0; i < windows.Length; i++) {
                var window = windows[i] as EditorWindow;
                var footer = window != null ? window.rootVisualElement?.Q<IMGUIContainer>(FooterName) : null;
                footer?.MarkDirtyRepaint();
            }
        }
    }
}
