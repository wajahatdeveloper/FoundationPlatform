using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HierarchyX {
    /// <summary>
    /// Main draw pipeline. Hooks the hierarchy window and layers HierarchyX passes
    /// (row tint, tree lines, mini labels, separators, drag-selection) on top of each row.
    /// Uses only public editor APIs so it survives Unity version bumps.
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyX {

        private const float AlphaThreshold = 0.01f;
        private const float Indent = 14f;
        private const string MenuPath = "Edit/HierarchyX Enabled %h";

        // Per-event state
        private static EventType lastEventType;
        private static bool isFirstVisible;
        private static bool isRepaint;

        // Mini-label provider cache
        private static readonly List<MiniLabel> providers = new List<MiniLabel>();
        private static MiniLabelType[] cachedTypes = Array.Empty<MiniLabelType>();

        // Decoration for the row currently being drawn (from HierarchyXRegistry).
        private static HierarchyRowDecoration rowDeco;

        // Enhanced selection state
        private static List<Object> dragSelection;
        private static Vector2 selectionStart;
        private static Rect selectionRect;

        static HierarchyX() {
            EditorApplication.hierarchyWindowItemOnGUI -= OnItemGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnItemGUI;
            EditorApplication.RepaintHierarchyWindow();
        }

        [MenuItem(MenuPath, false, int.MinValue)]
        private static void ToggleEnabled() {
            var s = HierarchyXSettings.Instance;
            s.enabled = !s.enabled;
            s.Save();
            EditorApplication.RepaintHierarchyWindow();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleEnabledValidate() {
            Menu.SetChecked(MenuPath, HierarchyXSettings.Instance.enabled);
            return true;
        }

        private static void OnItemGUI(int id, Rect rect) {
            var s = HierarchyXSettings.Instance;
            if (!s.enabled)
                return;

            isRepaint = Event.current.type == EventType.Repaint;
            isFirstVisible = Event.current.type != lastEventType;
            lastEventType = Event.current.type;

#if UNITY_6000_2_OR_NEWER
            var go = EditorUtility.EntityIdToObject(id) as GameObject;
#else
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
#endif

            try {
                DoSelection(rect, go, s);

                if (!go)
                    return;

                RebuildProvidersIfNeeded(s);

                var hasDeco = s.rowDecorators && HierarchyXRegistry.HasAny && HierarchyXRegistry.TryGet(go, out rowDeco);
                if (!hasDeco)
                    rowDeco = default;

                ColorSort(rect, go, s);
                DrawTree(rect, go, s);
                DrawMiniLabels(rect, go, s);
                DrawRowAccent(rect, hasDeco ? rowDeco : default);
                DrawSeparator(rect, s);
            } catch (Exception e) {
                Debug.LogError("HierarchyX draw error\n" + e);
            }
        }

        #region Row Color

        private static void ColorSort(Rect rect, GameObject go, HierarchyXSettings s) {
            if (!isRepaint || !s.rowColors)
                return;

            rect.xMin = 0f;
            rect.xMax = EditorGUIUtility.currentViewWidth;

            // Decorator-supplied tint sits between per-layer custom tint and the odd/even overlay.
            if (rowDeco.HasTint) {
                switch (rowDeco.tintMode) {
                    case TintMode.Flat:
                        EditorGUI.DrawRect(rect, rowDeco.rowTint);
                        break;
                    case TintMode.GradientRightToLeft:
                        DrawGradient(rect, rowDeco.rowTint, false);
                        break;
                    case TintMode.GradientLeftToRight:
                        DrawGradient(rect, rowDeco.rowTint, true);
                        break;
                }
            }

            var custom = GetRowCustomTint(go, s);
            if (custom.color.a > AlphaThreshold) {
                switch (custom.mode) {
                    case TintMode.Flat:
                        EditorGUI.DrawRect(rect, custom.color);
                        break;
                    case TintMode.GradientRightToLeft:
                        DrawGradient(rect, custom.color, false);
                        break;
                    case TintMode.GradientLeftToRight:
                        DrawGradient(rect, custom.color, true);
                        break;
                }
            }

            var rowTint = GetRowTint(rect, s);
            if (rowTint.a > AlphaThreshold)
                EditorGUI.DrawRect(rect, rowTint);
        }

        private static Color GetRowTint(Rect rect, HierarchyXSettings s) {
            if (rect.height <= 0f)
                return s.evenRowColor;
            var index = Mathf.FloorToInt(rect.y / rect.height);
            return (index & 1) == 1 ? s.oddRowColor : s.evenRowColor;
        }

        private static LayerColor GetRowCustomTint(GameObject go, HierarchyXSettings s) {
            var list = s.perLayerColors;
            if (list == null)
                return default;

            var layer = go.layer;
            for (var i = 0; i < list.Count; i++)
                if (list[i].layer == layer)
                    return list[i];
            return default;
        }

        private static void DrawGradient(Rect rect, Color color, bool leftToRight) {
            var prev = GUI.color;
            GUI.color = color;
            if (leftToRight)
                GUI.DrawTexture(Rect.MinMaxRect(rect.xMax, rect.yMin, rect.xMin, rect.yMax), HierarchyXStyles.GradientTexture, ScaleMode.StretchToFill);
            else
                GUI.DrawTexture(rect, HierarchyXStyles.GradientTexture, ScaleMode.StretchToFill);
            GUI.color = prev;
        }

        #endregion

        #region Tree Lines

        private static void DrawTree(Rect rect, GameObject go, HierarchyXSettings s) {
            if (!s.drawTree || s.treeOpacity <= AlphaThreshold)
                return;
            if (!isRepaint && !s.selectOnTree)
                return;

            var parent = go.transform.parent;

            // Cell for the connector between this item and its parent.
            rect.x -= Indent + 2f;
            rect.xMin -= Indent;
            rect.xMax = rect.xMin + Indent;

            if (parent) {
                var last = HierarchyXUtility.TransformIsLastChild(go.transform);
                var color = HierarchyXUtility.GetHierarchyColor(parent);
                color.a = s.treeOpacity;

                if (isRepaint) {
                    DrawBranch(rect, last, color);

                    var extend = go.transform.childCount == 0 ? s.stemProportion * Indent : 0f;
                    if (extend > 0.01f) {
                        var cy = Mathf.Round(rect.y + rect.height / 2f);
                        EditorGUI.DrawRect(new Rect(rect.xMax, cy, extend, 1f), color);
                    }
                }

                if (s.selectOnTree && GUI.Button(rect, GUIContent.none, HierarchyXStyles.TransparentButton))
                    Selection.activeTransform = parent;
            }

            // Ancestor vertical lines.
            var current = parent;
            for (rect.x -= Indent; rect.xMin > 0f && current && current.parent; rect.x -= Indent) {
                if (!HierarchyXUtility.TransformIsLastChild(current)) {
                    var color = HierarchyXUtility.GetHierarchyColor(current.parent);
                    color.a = s.treeOpacity;

                    if (isRepaint)
                        DrawVerticalLine(rect, color);

                    if (s.selectOnTree && GUI.Button(rect, GUIContent.none, HierarchyXStyles.TransparentButton))
                        Selection.activeTransform = current.parent;
                }
                current = current.parent;
            }
        }

        private static void DrawBranch(Rect cell, bool last, Color color) {
            var cx = Mathf.Round(cell.x + cell.width / 2f);
            var cy = Mathf.Round(cell.y + cell.height / 2f);

            // Vertical: full height for a tee, top-half for the last child's elbow.
            var vHeight = last ? cy - cell.yMin : cell.height;
            EditorGUI.DrawRect(new Rect(cx, cell.yMin, 1f, vHeight), color);
            // Horizontal stub reaching the item.
            EditorGUI.DrawRect(new Rect(cx, cy, cell.xMax - cx, 1f), color);
        }

        private static void DrawVerticalLine(Rect cell, Color color) {
            var cx = Mathf.Round(cell.x + cell.width / 2f);
            EditorGUI.DrawRect(new Rect(cx, cell.yMin, 1f, cell.height), color);
        }

        #endregion

        #region Mini Labels

        private static void RebuildProvidersIfNeeded(HierarchyXSettings s) {
            var types = s.miniLabels;
            var changed = types.Count != cachedTypes.Length;
            if (!changed)
                for (var i = 0; i < types.Count; i++)
                    if (types[i] != cachedTypes[i]) {
                        changed = true;
                        break;
                    }

            if (!changed)
                return;

            providers.Clear();
            cachedTypes = types.ToArray();
            for (var i = 0; i < types.Count; i++) {
                var p = MiniLabel.Create(types[i]);
                if (p != null)
                    providers.Add(p);
            }
        }

        private static void DrawMiniLabels(Rect rect, GameObject go, HierarchyXSettings s) {
            if (providers.Count == 0)
                return;

            var style = HierarchyXStyles.MiniLabelStyle;
            style.fontSize = s.smallerFont ? 8 : 9;

            for (var i = 0; i < providers.Count; i++)
                providers[i].Fill(go, s);

            rect.xMax = EditorGUIUtility.currentViewWidth - s.rightMargin;

            // Two providers with values stack vertically; a single one can center.
            if (providers.Count >= 2) {
                var a = providers[0];
                var b = providers[1];
                var aHas = a.HasValue;
                var bHas = b.HasValue;

                if (aHas && bHas || !s.centralizeWhenPossible) {
                    var top = rect;
                    var bottom = rect;
                    top.yMax = rect.yMin + rect.height / 2f;
                    bottom.yMin = rect.yMin + rect.height / 2f;
                    if (aHas) DrawOne(a, top, style, go);
                    if (bHas) DrawOne(b, bottom, style, go);
                    return;
                }
                if (bHas) { DrawOne(b, rect, style, go); return; }
                if (aHas) { DrawOne(a, rect, style, go); return; }
                return;
            }

            var single = providers[0];
            if (single.HasValue)
                DrawOne(single, rect, style, go);
        }

        private static void DrawOne(MiniLabel label, Rect rect, GUIStyle style, GameObject go) {
            var width = label.Measure(style);
            rect.xMin = rect.xMax - width;

            var prev = GUI.color;
            var c = HierarchyXUtility.GetHierarchyColor(go);
            c.a = label.Faded(go) ? 0.5f : 0.85f;
            GUI.color = c;

            if (label.Draw(rect, style, go))
                EditorApplication.RepaintHierarchyWindow();

            GUI.color = prev;
        }

        #endregion

        #region Row Accent

        // Left-edge vertical spine marking decorator-owned rows (e.g. character-rig membership).
        // Drawn at x=0 so it never collides with the foldout, icon, label or mini-labels.
        private static void DrawRowAccent(Rect rect, HierarchyRowDecoration deco) {
            if (!deco.HasAccent)
                return;

            var width = deco.accentFilled ? 4f : 2f;
            var spine = new Rect(0f, rect.yMin, width, rect.height);

            if (isRepaint)
                EditorGUI.DrawRect(spine, deco.accent);

            if (!string.IsNullOrEmpty(deco.tooltip))
                GUI.Label(spine, new GUIContent(string.Empty, deco.tooltip));
        }

        #endregion

        #region Separator

        private static void DrawSeparator(Rect rect, HierarchyXSettings s) {
            if (s.lineThickness < 1 || s.lineColor.a <= AlphaThreshold || !isRepaint)
                return;

            rect.xMin = 0f;
            rect.xMax = EditorGUIUtility.currentViewWidth;
            rect.yMin -= s.lineThickness / 2f;
            rect.height = s.lineThickness;
            EditorGUI.DrawRect(rect, s.lineColor);
        }

        #endregion

        #region Enhanced Selection

        private static void DoSelection(Rect rect, GameObject go, HierarchyXSettings s) {
            if (!s.enhancedSelection || Event.current.button != 1) {
                dragSelection = null;
                return;
            }

            rect.xMin = 0f;
            rect.xMax = EditorGUIUtility.currentViewWidth;

            switch (Event.current.type) {
                case EventType.MouseDrag:
                    if (!isFirstVisible)
                        return;

                    if (dragSelection == null) {
                        dragSelection = new List<Object>();
                        selectionStart = Event.current.mousePosition;
                        selectionRect = new Rect();
                    }

                    selectionRect = Rect.MinMaxRect(
                        Mathf.Min(Event.current.mousePosition.x, selectionStart.x),
                        Mathf.Min(Event.current.mousePosition.y, selectionStart.y),
                        Mathf.Max(Event.current.mousePosition.x, selectionStart.x),
                        Mathf.Max(Event.current.mousePosition.y, selectionStart.y));

                    if (Event.current.control || Event.current.command)
                        dragSelection.AddRange(Selection.objects);

                    Selection.objects = dragSelection.ToArray();
                    Event.current.Use();
                    break;

                case EventType.MouseUp:
                    if (dragSelection != null)
                        Event.current.Use();
                    dragSelection = null;
                    break;

                case EventType.Layout:
                    if (dragSelection == null || !go)
                        break;
                    if (!selectionRect.Overlaps(rect) && dragSelection.Contains(go))
                        dragSelection.Remove(go);
                    else if (selectionRect.Overlaps(rect) && !dragSelection.Contains(go))
                        dragSelection.Add(go);
                    break;
            }
        }

        #endregion
    }
}
