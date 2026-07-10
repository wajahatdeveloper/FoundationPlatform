using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HierarchyX {

    /// <summary>
    /// IMGUI primitives for the Hierarchy docked setup panel (status pills) plus the shared body
    /// renderer used by both the docked footer (<see cref="HierarchyPanelHost"/>) and the fallback
    /// companion window. <see cref="HierarchyX"/>'s asmdef has no game dependencies, so the widget
    /// kit is intentionally small and self-contained (AuthoringUxShared lives in another assembly
    /// and has no chip primitive).
    /// </summary>
    public static class HierarchyPanelWidgets {

        private static GUIStyle chipStyle;
        private static GUIStyle titleStyle;
        private static GUIStyle emptyStyle;

        /// <summary>
        /// Section id an out-of-context tool asked to reveal (see
        /// <see cref="HierarchyPanelHost.RevealSection"/>). Consumed once by <see cref="DrawSections"/>:
        /// on the next Repaint it scrolls that section into view and clears the request. Shared by the
        /// docked footer and the fallback window so whichever surface repaints first honors it.
        /// </summary>
        public static string PendingRevealId;

        public static Color StatusColor(PanelChipStatus status) {
            switch (status) {
                case PanelChipStatus.Ok:      return new Color(0.35f, 0.72f, 0.40f);
                case PanelChipStatus.Warning: return new Color(0.90f, 0.70f, 0.22f);
                case PanelChipStatus.Error:   return new Color(0.85f, 0.34f, 0.34f);
                default:                      return new Color(0.55f, 0.55f, 0.55f);
            }
        }

        private static string StatusGlyph(PanelChipStatus status) {
            switch (status) {
                case PanelChipStatus.Ok:      return "✓"; // ✓
                case PanelChipStatus.Warning: return "!";
                case PanelChipStatus.Error:   return "✕"; // ✕
                default:                      return "•"; // •
            }
        }

        private static void EnsureStyles() {
            if (chipStyle == null) {
                chipStyle = new GUIStyle(EditorStyles.miniButton) {
                    fontSize = 10,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(6, 6, 1, 1),
                    margin = new RectOffset(2, 2, 1, 1),
                    fixedHeight = 16f,
                };
            }
            if (titleStyle == null) {
                titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
            }
            if (emptyStyle == null) {
                emptyStyle = new GUIStyle(EditorStyles.miniLabel) {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                };
            }
        }

        /// <summary>Draw a single colored status pill (non-interactive).</summary>
        public static void DrawChip(PanelChip chip) {
            EnsureStyles();
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = StatusColor(chip.status);
            var content = new GUIContent(StatusGlyph(chip.status) + " " + chip.label, chip.tooltip);
            GUILayout.Label(content, chipStyle);
            GUI.backgroundColor = prev;
        }

        /// <summary>Draw a wrapping row of chips.</summary>
        public static void DrawChips(IEnumerable<PanelChip> chips) {
            if (chips == null)
                return;
            foreach (var chip in chips)
                DrawChip(chip);
        }

        /// <summary>
        /// Render the registered section bodies via <see cref="IHierarchyPanelSection.OnBodyGUI"/>.
        /// A single section draws its body directly (no redundant title); multiple sections get a
        /// collapsible title each (state persisted in
        /// <see cref="HierarchyXSettings.panelCollapsedSections"/>). Header chips are NOT drawn here —
        /// they live in the bottom status bar (<see cref="DrawStatusBar"/>). Shared by the docked
        /// footer and the fallback window.
        /// </summary>
        public static void DrawSections(ref Vector2 scroll) {
            EnsureStyles();
            var sections = HierarchyXPanelRegistry.Sections;
            if (sections.Count == 0) {
                GUILayout.FlexibleSpace();
                GUILayout.Label(
                    "No panel sections registered.\nInstall a plugin that provides an IHierarchyPanelSection.",
                    emptyStyle);
                GUILayout.FlexibleSpace();
                return;
            }

            var single = sections.Count == 1;
            var settings = HierarchyXSettings.Instance;
            scroll = EditorGUILayout.BeginScrollView(scroll);

            for (var i = 0; i < sections.Count; i++) {
                var section = sections[i];
                var expanded = single || !settings.panelCollapsedSections.Contains(section.Id);

                if (!single) {
                    var newExpanded = EditorGUILayout.Foldout(expanded, section.Title, true, titleStyle);
                    if (newExpanded != expanded) {
                        if (newExpanded)
                            settings.panelCollapsedSections.Remove(section.Id);
                        else if (!settings.panelCollapsedSections.Contains(section.Id))
                            settings.panelCollapsedSections.Add(section.Id);
                        settings.Save();
                        expanded = newExpanded;
                    }
                }

                // Honor a pending reveal request: scroll this section's header to the top once.
                if (!string.IsNullOrEmpty(PendingRevealId) && section.Id == PendingRevealId
                    && Event.current.type == EventType.Repaint) {
                    scroll.y = single ? 0f : Mathf.Max(0f, GUILayoutUtility.GetLastRect().y);
                    PendingRevealId = null;
                }

                if (expanded) {
                    try {
                        section.OnBodyGUI();
                    } catch (System.Exception e) {
                        EditorGUILayout.HelpBox("Section '" + section.Title + "' threw:\n" + e.Message, MessageType.Error);
                    }
                }

                if (!single && i < sections.Count - 1)
                    HorizontalLine();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Persistent bottom status bar: the aggregated header chips of every section, always
        /// visible (even when the panel is collapsed) so state reads at a glance.
        /// </summary>
        public static void DrawStatusBar() {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawStatusChips();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// The aggregated chips only (no toolbar wrapper), so callers can embed them next to other
        /// controls — e.g. the collapsed strip's expand arrow.
        /// </summary>
        public static void DrawStatusChips() {
            EnsureStyles();
            var sections = HierarchyXPanelRegistry.Sections;
            var any = false;
            for (var i = 0; i < sections.Count; i++) {
                IEnumerable<PanelChip> chips = null;
                try {
                    chips = sections[i].GetHeaderChips();
                } catch {
                    // A misbehaving section must not break the status bar.
                }
                if (chips == null)
                    continue;
                foreach (var chip in chips) {
                    DrawChip(chip);
                    any = true;
                }
            }

            if (!any)
                GUILayout.Label("No status", EditorStyles.miniLabel);
        }

        /// <summary>
        /// Draw the aggregated top-toolbar icon actions of every section, as compact toolbar buttons.
        /// Call inside a horizontal toolbar group (e.g. the panel header).
        /// </summary>
        public static void DrawToolbarActions() {
            var sections = HierarchyXPanelRegistry.Sections;
            for (var i = 0; i < sections.Count; i++) {
                IEnumerable<PanelAction> actions = null;
                try {
                    actions = sections[i].GetToolbarActions();
                } catch {
                    // A misbehaving section must not break the toolbar.
                }
                if (actions == null)
                    continue;
                foreach (var action in actions) {
                    GUIContent content;
                    if (!string.IsNullOrEmpty(action.iconName)) {
                        content = new GUIContent(EditorGUIUtility.IconContent(action.iconName)) { tooltip = action.tooltip };
                    } else {
                        content = new GUIContent(action.glyph, action.tooltip);
                    }
                    if (GUILayout.Button(content, EditorStyles.toolbarButton, GUILayout.Width(24f))
                        && action.onClick != null)
                        action.onClick();
                }
            }
        }

        private static void HorizontalLine() {
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.25f));
        }
    }
}
