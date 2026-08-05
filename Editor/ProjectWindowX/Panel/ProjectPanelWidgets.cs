using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectWindowX {

    public static class ProjectPanelWidgets {

        private const float SectionHeaderHeight = 18f;
        private const float HeaderButtonSize = 18f;
        private const float HeaderButtonGap = 1f;

        private static GUIStyle chipStyle;
        private static GUIStyle titleStyle;
        private static GUIStyle emptyStyle;

        public static string PendingRevealId;

        private static float lastMeasuredHeight;

        public static Color StatusColor(PanelChipStatus status) {
            switch (status) {
                case PanelChipStatus.Ok: return new Color(0.35f, 0.72f, 0.40f);
                case PanelChipStatus.Warning: return new Color(0.90f, 0.70f, 0.22f);
                case PanelChipStatus.Error: return new Color(0.85f, 0.34f, 0.34f);
                default: return new Color(0.55f, 0.55f, 0.55f);
            }
        }

        /// <summary>Shared glyph for status chips — HierarchyX forwards here to keep one palette.</summary>
        public static string StatusGlyph(PanelChipStatus status) {
            switch (status) {
                case PanelChipStatus.Ok: return "✓";
                case PanelChipStatus.Warning: return "!";
                case PanelChipStatus.Error: return "✕";
                default: return "•";
            }
        }

        private static void EnsureStyles() {
            if (chipStyle == null) {
                chipStyle = new GUIStyle(EditorStyles.miniButton) {
                    fontSize = 9,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(4, 4, 0, 0),
                    margin = new RectOffset(1, 1, 0, 0),
                    fixedHeight = 14f,
                };
            }
            if (titleStyle == null) {
                titleStyle = new GUIStyle(EditorStyles.foldout) {
                    fontStyle = FontStyle.Bold,
                    fontSize = 10,
                    fixedHeight = SectionHeaderHeight,
                };
            }
            if (emptyStyle == null) {
                emptyStyle = new GUIStyle(EditorStyles.miniLabel) {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    fontSize = 9,
                };
            }
        }

        public static void DrawChip(PanelChip chip) {
            EnsureStyles();
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = StatusColor(chip.status);
            GUILayout.Label(new GUIContent(StatusGlyph(chip.status) + " " + chip.label, chip.tooltip), chipStyle);
            GUI.backgroundColor = prev;
        }

        public static void DrawStatusBar() {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawStatusChips();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public static void DrawStatusChips() {
            EnsureStyles();
            var sections = ProjectWindowXPanelRegistry.Sections;
            var any = false;
            for (var i = 0; i < sections.Count; i++) {
                IEnumerable<PanelChip> chips = null;
                try {
                    chips = sections[i].GetHeaderChips();
                } catch {
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

        public static float DrawSections(ref Vector2 scroll, float maxHeight) {
            EnsureStyles();
            var sections = ProjectWindowXPanelRegistry.Sections;
            if (sections.Count == 0) {
                GUILayout.Label(
                    "No panel sections registered.\nInstall a plugin that provides an IProjectPanelSection.",
                    emptyStyle);
                if (Event.current.type == EventType.Repaint)
                    lastMeasuredHeight = GUILayoutUtility.GetLastRect().height;
                return lastMeasuredHeight;
            }

            var settings = ProjectWindowXSettings.instance;
            var single = sections.Count == 1;
            NormalizeExpandedId(sections, settings, single);

            var needsScroll = maxHeight > 0f && lastMeasuredHeight > maxHeight;
            if (needsScroll)
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(maxHeight));

            EditorGUILayout.BeginVertical();
            for (var i = 0; i < sections.Count; i++) {
                var section = sections[i];
                var expanded = single
                    || string.Equals(settings.panelExpandedSectionId, section.Id, System.StringComparison.Ordinal);

                var newExpanded = DrawSectionHeader(section, expanded, single, out var headerRect);
                if (!single && newExpanded != expanded) {
                    settings.panelExpandedSectionId = newExpanded ? section.Id : string.Empty;
                    settings.SaveNow();
                    expanded = newExpanded;
                }

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
        /// Accordion defaults match HierarchyX: a lone section always shows its body; with 2+ sections,
        /// empty <see cref="ProjectWindowXSettings.panelExpandedSectionId"/> expands the first by Order.
        /// Stale ids are cleared.
        /// </summary>
        private static void NormalizeExpandedId(IReadOnlyList<IProjectPanelSection> sections, ProjectWindowXSettings settings, bool single) {
            if (single) {
                var onlyId = sections[0].Id;
                if (!string.Equals(settings.panelExpandedSectionId, onlyId, System.StringComparison.Ordinal)) {
                    settings.panelExpandedSectionId = onlyId;
                    settings.SaveNow();
                }
                return;
            }

            if (string.IsNullOrEmpty(settings.panelExpandedSectionId)) {
                settings.panelExpandedSectionId = sections[0].Id;
                settings.SaveNow();
                return;
            }

            for (var i = 0; i < sections.Count; i++) {
                if (string.Equals(sections[i].Id, settings.panelExpandedSectionId, System.StringComparison.Ordinal))
                    return;
            }

            settings.panelExpandedSectionId = sections[0].Id;
            settings.SaveNow();
        }

        private static bool DrawSectionHeader(IProjectPanelSection section, bool expanded, bool hideFoldout, out Rect headerRect) {
            IEnumerable<PanelAction> actions = null;
            try {
                actions = section.GetToolbarActions();
            } catch {
            }
            var actionList = actions != null ? new List<PanelAction>(actions) : null;
            var actionCount = actionList?.Count ?? 0;

            var rect = EditorGUILayout.GetControlRect(false, SectionHeaderHeight);
            headerRect = rect;
            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);

            var buttonsWidth = actionCount > 0 ? actionCount * (HeaderButtonSize + HeaderButtonGap) : 0f;
            var labelRect = new Rect(rect.x + 2f, rect.y, Mathf.Max(0f, rect.width - buttonsWidth - 4f), rect.height);

            var newExpanded = expanded;
            if (hideFoldout) {
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
    }
}
