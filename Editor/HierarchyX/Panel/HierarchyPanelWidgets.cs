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

        private const float SectionHeaderHeight = 22f;
        private const float HeaderButtonSize = 20f;
        private const float HeaderButtonGap = 2f;

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
                // Base on foldout (not boldLabel) so Unity draws the expand/collapse arrow.
                titleStyle = new GUIStyle(EditorStyles.foldout) {
                    fontStyle = FontStyle.Bold,
                    fontSize = 11,
                };
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
        /// Cache of the last measured, un-clipped natural height of <see cref="DrawSections"/>'s
        /// content (Repaint-event only). Hosts read this back to size their container to fit —
        /// see <see cref="DrawSections"/>. One frame of lag between a size-changing action (e.g.
        /// switching the open accordion section) and the container actually resizing is expected.
        /// </summary>
        private static float lastMeasuredHeight;

        /// <summary>
        /// Render the registered section bodies via <see cref="IHierarchyPanelSection.OnBodyGUI"/> as
        /// an accordion: at most one section is expanded at a time (state persisted in
        /// <see cref="HierarchyXSettings.panelCollapsedSections"/>, which stores the collapsed ids;
        /// opening a section collapses every other one). A single registered section draws its body
        /// directly with no foldout (nothing to accordion between). Header chips are NOT drawn here —
        /// they live in the bottom status bar (<see cref="DrawStatusBar"/>). Shared by the docked
        /// footer and the fallback window.
        /// </summary>
        /// <param name="maxHeight">
        /// The tallest the content is allowed to get before it scrolls instead of growing further
        /// (the host's own height ceiling, e.g. <see cref="HierarchyPanelHost.MaxHeight"/>).
        /// </param>
        /// <returns>
        /// The content's natural (un-clipped) height, measured this Repaint if one occurred, else the
        /// last measured value. Callers use this to size their container to fit the expanded section.
        /// </returns>
        public static float DrawSections(ref Vector2 scroll, float maxHeight) {
            EnsureStyles();
            var sections = HierarchyXPanelRegistry.Sections;
            if (sections.Count == 0) {
                GUILayout.Label(
                    "No panel sections registered.\nInstall a plugin that provides an IHierarchyPanelSection.",
                    emptyStyle);
                if (Event.current.type == EventType.Repaint)
                    lastMeasuredHeight = GUILayoutUtility.GetLastRect().height;
                return lastMeasuredHeight;
            }

            var single = sections.Count == 1;
            var settings = HierarchyXSettings.Instance;
            if (!single)
                NormalizeAccordion(sections, settings);

            var needsScroll = maxHeight > 0f && lastMeasuredHeight > maxHeight;
            if (needsScroll)
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(maxHeight));

            EditorGUILayout.BeginVertical();
            for (var i = 0; i < sections.Count; i++) {
                var section = sections[i];
                var expanded = single || !settings.panelCollapsedSections.Contains(section.Id);

                var newExpanded = DrawSectionHeader(section, expanded, single, out var headerRect);
                if (!single && newExpanded != expanded) {
                    if (newExpanded) {
                        // Accordion: opening one section collapses every other one.
                        settings.panelCollapsedSections.Clear();
                        for (var j = 0; j < sections.Count; j++)
                            if (sections[j].Id != section.Id)
                                settings.panelCollapsedSections.Add(sections[j].Id);
                    } else if (!settings.panelCollapsedSections.Contains(section.Id)) {
                        settings.panelCollapsedSections.Add(section.Id);
                    }
                    settings.Save();
                    expanded = newExpanded;
                }

                // Honor a pending reveal request: scroll this section's header to the top once.
                if (!string.IsNullOrEmpty(PendingRevealId) && section.Id == PendingRevealId
                    && Event.current.type == EventType.Repaint) {
                    scroll.y = single ? 0f : Mathf.Max(0f, headerRect.y);
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
            EditorGUILayout.EndVertical();
            var contentRect = GUILayoutUtility.GetLastRect();

            if (needsScroll)
                EditorGUILayout.EndScrollView();

            if (Event.current.type == EventType.Repaint)
                lastMeasuredHeight = contentRect.height;

            return lastMeasuredHeight;
        }

        /// <summary>
        /// Enforce the accordion invariant (at most one section expanded): if more than one section
        /// is currently uncollapsed — e.g. on first run, when <see cref="HierarchyXSettings.panelCollapsedSections"/>
        /// starts empty and every section reads as expanded — collapse every one after the first.
        /// </summary>
        private static void NormalizeAccordion(IReadOnlyList<IHierarchyPanelSection> sections, HierarchyXSettings settings) {
            string firstExpandedId = null;
            var changed = false;
            for (var i = 0; i < sections.Count; i++) {
                var id = sections[i].Id;
                if (settings.panelCollapsedSections.Contains(id))
                    continue;
                if (firstExpandedId == null) {
                    firstExpandedId = id;
                } else {
                    settings.panelCollapsedSections.Add(id);
                    changed = true;
                }
            }
            if (changed)
                settings.Save();
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
        /// Draw one section's header: a full-width toolbar-styled band tall enough to host real
        /// toolbar buttons, with the (optionally foldable) title on the left and that section's own
        /// <see cref="IHierarchyPanelSection.GetToolbarActions"/> right-aligned in the same row —
        /// e.g. its "Refresh" button lives next to the title it refreshes instead of a shared,
        /// unlabeled top toolbar. Returns the (possibly toggled) expanded state; <paramref name="single"/>
        /// suppresses the foldout affordance but still draws the header so a lone section's actions
        /// are never left with nowhere to render.
        /// </summary>
        private static bool DrawSectionHeader(IHierarchyPanelSection section, bool expanded, bool single, out Rect headerRect) {
            IEnumerable<PanelAction> actions = null;
            try {
                actions = section.GetToolbarActions();
            } catch {
                // A misbehaving section must not break the header.
            }
            var actionList = actions != null ? new List<PanelAction>(actions) : null;
            var actionCount = actionList?.Count ?? 0;

            var rect = EditorGUILayout.GetControlRect(false, SectionHeaderHeight);
            headerRect = rect;
            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);

            var buttonsWidth = actionCount > 0 ? actionCount * (HeaderButtonSize + HeaderButtonGap) : 0f;
            var labelRect = new Rect(rect.x + 2f, rect.y, Mathf.Max(0f, rect.width - buttonsWidth - 4f), rect.height);

            var newExpanded = expanded;
            if (single) {
                GUI.Label(labelRect, section.Title, titleStyle);
            } else {
                newExpanded = EditorGUI.Foldout(labelRect, expanded, section.Title, true, titleStyle);
            }

            if (actionCount > 0) {
                var bx = rect.xMax - 2f;
                for (var i = 0; i < actionCount; i++) {
                    bx -= HeaderButtonSize;
                    var btnRect = new Rect(bx, rect.y + (rect.height - HeaderButtonSize) * 0.5f, HeaderButtonSize, HeaderButtonSize);
                    var action = actionList[i];
                    GUIContent content;
                    if (!string.IsNullOrEmpty(action.iconName)) {
                        content = new GUIContent(EditorGUIUtility.IconContent(action.iconName)) { tooltip = action.tooltip };
                    } else {
                        content = new GUIContent(action.glyph, action.tooltip);
                    }
                    if (GUI.Button(btnRect, content, EditorStyles.toolbarButton) && action.onClick != null)
                        action.onClick();
                    bx -= HeaderButtonGap;
                }
            }

            return newExpanded;
        }

        private static void HorizontalLine() {
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.25f));
        }
    }
}
